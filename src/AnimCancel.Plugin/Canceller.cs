using AnimCancel.Core;
using UnityEngine;

namespace AnimCancel;

/// <summary>
/// Fires the cancel: an emote trigger on the local Animator only. Never ZSyncAnimation,
/// never StartEmote, never the ZDO, so other players see nothing. Also owns the clean
/// recovery: speed under hit-stop, emote_stop on release, and the cancelled state whose
/// own Speed events are skipped during the blend.
/// </summary>
internal static class Canceller
{
    private static readonly int TagAttack = Animator.StringToHash("attack");
    private static readonly int TagEmote = Animator.StringToHash("emote");
    private const string EmoteStop = "emote_stop";

    /// <summary>True from a cancel until the next attack starts or the emote is stopped.</summary>
    internal static bool InFlight;

    /// <summary>Time.time of the last cancel.</summary>
    internal static float LastCancelTime;

    /// <summary>Layer-0 fullPathHash of the state that was cancelled, while it still plays.</summary>
    internal static int? CancelledState;

    /// <summary>
    /// An attack started since the cancel: <see cref="CancelledState"/> then only counts
    /// while it is current, so a relaunch into the same state keeps its own Speed events.
    /// </summary>
    internal static bool Relaunched;

    /// <summary>Time.time layer 0 was first seen fully in the emote while in flight; -1 otherwise.</summary>
    private static float _fullyInEmoteSince = -1f;

    /// <summary>
    /// The emote trigger set by the last cancel until it is known to be spent. If it lost to
    /// another trigger set the same frame it would stay latched and cut whatever comes next.
    /// </summary>
    private static string? _emoteTrigger;

    /// <summary>
    /// The relaunch was consumed for the current cancel: further StartAttack calls of the
    /// local player are duplicates until the relaunched attack begins (RelaunchBlock).
    /// </summary>
    private static bool _blockActive;

    /// <summary>StartAttack calls refused by the current block.</summary>
    private static int _blocked;

    /// <param name="stateTag">Tag of the state being cancelled (default "attack"; a dodge passes its own).</param>
    /// <param name="kindOverride">Log kind when there is no current attack (dodge).</param>
    public static void Cancel(Player p, CancelEmote e, int? stateTag = null, string? kindOverride = null)
    {
        Animator animator = p.m_animator;
        if (animator == null)
        {
            return;
        }

        float speedBefore = animator.speed;
        float pauseTimer = p.m_animEvent != null ? p.m_animEvent.m_pauseTimer : 0f;

        // The next state when blending into the cancelled state (tag match), otherwise the current one.
        bool useNext = animator.IsInTransition(0)
                       && animator.GetNextAnimatorStateInfo(0).tagHash == (stateTag ?? TagAttack);
        CancelledState = useNext
            ? animator.GetNextAnimatorStateInfo(0).fullPathHash
            : animator.GetCurrentAnimatorStateInfo(0).fullPathHash;
        Relaunched = false;
        LiftBlock("cancel");

        // A previous emote_stop can stay latched when a dodge/attack wins via Any State in the
        // same frame; it would then cut this emote on entry.
        animator.ResetTrigger(EmoteStop);
        _emoteTrigger = CancelEmotes.Trigger(e);
        animator.SetTrigger(_emoteTrigger);
        string plan = ApplySpeedPlan(p, animator, pauseTimer);

        InFlight = true;
        LastCancelTime = Time.time;
        _fullyInEmoteSince = -1f;

        if (ReviewLog.On)
        {
            Attack? atk = p.m_currentAttack;
            ReviewLog.Write("cancel",
                ("kind", kindOverride ?? AttackClassifier.Classify(atk?.m_attackAnimation).ToString()),
                ("lvl", (atk?.m_currentAttackCainLevel ?? 0).ToString()),
                ("emote", e.ToString()),
                ("pauseTimer", DebugLine.Num(pauseTimer)),
                ("animSpeedBefore", DebugLine.Num(speedBefore)),
                ("plan", plan));
        }
    }

    /// <summary>
    /// Max combo step: sets a sword chain trigger on the local Animator only (never ZSyncAnimation,
    /// no StartAttack, so the secondary Attack stays current) after resetting stale emote and chain
    /// triggers, and applies the speed plan of a cancel. The state that fired the hit becomes the
    /// cancelled state, so its trailing Speed events are skipped during the blend (SpeedSkip, R13).
    /// Returns the speed plan, or null without an Animator.
    /// </summary>
    /// <param name="useNext">The hit came from layer 0's next state (ClipPick "next").</param>
    internal static string? Chain(Player p, string trigger, bool useNext, string[] staleTriggers)
    {
        Animator animator = p.m_animator;
        if (animator == null)
        {
            return null;
        }

        float pauseTimer = p.m_animEvent != null ? p.m_animEvent.m_pauseTimer : 0f;
        CancelledState = useNext && animator.IsInTransition(0)
            ? animator.GetNextAnimatorStateInfo(0).fullPathHash
            : animator.GetCurrentAnimatorStateInfo(0).fullPathHash;
        Relaunched = false;
        LiftBlock("cancel");

        animator.ResetTrigger(EmoteStop);
        ResetEmoteTrigger(animator);
        foreach (string t in staleTriggers)
        {
            animator.ResetTrigger(t);
        }

        animator.SetTrigger(trigger);
        string plan = ApplySpeedPlan(p, animator, pauseTimer);
        // Starts the SpeedSkip window; InFlight stays as it is (no emote was set).
        LastCancelTime = Time.time;
        return plan;
    }

    /// <summary>
    /// Called on the first attack start after a cancel: ends the in-flight state and
    /// gives the time since the cancel.
    /// </summary>
    internal static bool TryConsumeRelaunch(out float dtSinceCancel)
    {
        dtSinceCancel = 0f;
        // The relaunch can happen inside a hit-stop freeze, before the old clip's Speed event
        // fires: keep the state, but only while it is current (SpeedSkip, relaunched variant).
        if (CancelledState.HasValue)
        {
            Relaunched = true;
        }

        if (!InFlight)
        {
            return false;
        }

        InFlight = false;
        _fullyInEmoteSince = -1f;
        dtSinceCancel = Time.time - LastCancelTime;
        _blockActive = RelaunchBlock.ShouldStart(dtSinceCancel);
        _blocked = 0;
        // Do not let an unconsumed emote_stop or emote trigger wait for the next transition.
        Player? p = Player.m_localPlayer;
        if (p != null && p.m_animator != null)
        {
            p.m_animator.ResetTrigger(EmoteStop);
            ResetEmoteTrigger(p.m_animator);
        }

        return true;
    }

    /// <summary>
    /// Plugin disabled (every frame while off): stops our emote locally if a cancel is in
    /// flight and layer 0 is in or entering it, then drops everything.
    /// </summary>
    internal static void Disable()
    {
        LiftBlock("disabled");
        if (!InFlight && !CancelledState.HasValue && _emoteTrigger == null)
        {
            return;
        }

        Player? p = Player.m_localPlayer;
        Animator? animator = p != null ? p.m_animator : null;
        if (p != null && animator != null)
        {
            bool inTransition = animator.IsInTransition(0);
            bool currentIsEmote = animator.GetCurrentAnimatorStateInfo(0).tagHash == TagEmote;
            bool nextIsEmote = inTransition && animator.GetNextAnimatorStateInfo(0).tagHash == TagEmote;
            if (RecoveryPolicy.StopOnDisable(InFlight, currentIsEmote, nextIsEmote))
            {
                SendEmoteStop(p, animator);
                if (ReviewLog.On)
                {
                    ReviewLog.Write("recovery_stop",
                        ("dtSinceCancel", DebugLine.Num(Time.time - LastCancelTime)), ("reason", "disabled"));
                }
            }

            ResetEmoteTrigger(animator);
        }

        Reset();
    }

    /// <summary>Drops everything (no local player).</summary>
    internal static void Reset()
    {
        InFlight = false;
        CancelledState = null;
        Relaunched = false;
        _fullyInEmoteSince = -1f;
        _emoteTrigger = null;
        _blockActive = false;
        _blocked = 0;
    }

    /// <summary>
    /// Harmony prefix on Humanoid.StartAttack: true when this call is a duplicate of the
    /// relaunch, during the blend into the emote, and must be refused. Lifts the block once
    /// the relaunched attack has begun or the window is over. Nothing when no block is active.
    /// </summary>
    internal static bool RefuseStartAttack(Humanoid h)
    {
        if (!_blockActive)
        {
            return false;
        }

        Player? p = Player.m_localPlayer;
        if (p == null || p.m_animator == null)
        {
            LiftBlock("no_player");
            return false;
        }

        bool begun = RelaunchedAttackBegun(p.m_animator);
        float since = Time.time - LastCancelTime;
        if (RelaunchBlock.ShouldLift(_blockActive, since, begun))
        {
            LiftBlock(begun ? "attack" : "window");
            return false;
        }

        if (!RelaunchBlock.ShouldRefuse(h == p, _blockActive, since, begun))
        {
            return false;
        }

        _blocked++;
        return true;
    }

    /// <summary>Every frame: lifts the block without waiting for another StartAttack call.</summary>
    private static void TickBlock(Player p)
    {
        if (!_blockActive || p.m_animator == null)
        {
            return;
        }

        bool begun = RelaunchedAttackBegun(p.m_animator);
        if (RelaunchBlock.ShouldLift(_blockActive, Time.time - LastCancelTime, begun))
        {
            LiftBlock(begun ? "attack" : "window");
        }
    }

    private static bool RelaunchedAttackBegun(Animator animator)
    {
        bool inTransition = animator.IsInTransition(0);
        AnimatorStateInfo current = animator.GetCurrentAnimatorStateInfo(0);
        bool nextIsAttack = inTransition && animator.GetNextAnimatorStateInfo(0).tagHash == TagAttack;
        bool currentIsCancelled = CancelledState.HasValue && current.fullPathHash == CancelledState.Value;
        return RelaunchBlock.AttackBegun(inTransition, current.tagHash == TagAttack, nextIsAttack, currentIsCancelled);
    }

    /// <summary>Ends the block, with one relaunch_block line per cancel that relaunched.</summary>
    private static void LiftBlock(string reason)
    {
        if (!_blockActive)
        {
            return;
        }

        _blockActive = false;
        if (ReviewLog.On)
        {
            ReviewLog.Write("relaunch_block",
                ("blocked", _blocked.ToString()), ("reason", reason),
                ("dtSinceCancel", DebugLine.Num(Time.time - LastCancelTime)));
        }

        _blocked = 0;
    }

    /// <summary>
    /// Every frame: forgets the cancelled state once it left layer 0, and stops the emote
    /// when the player is fully in it with no attack held, or held for longer than
    /// RecoveryPolicy.HeldGrace with no relaunch; drops a stale in-flight state after
    /// RecoveryPolicy.Expiry. Works for a dodge cancel too (no Attack needed).
    /// </summary>
    internal static void TickRecovery()
    {
        if (!InFlight && !CancelledState.HasValue && !_blockActive)
        {
            return;
        }

        Player? p = Player.m_localPlayer;
        if (p == null || p.m_animator == null)
        {
            Reset();
            return;
        }

        TickBlock(p);

        Animator animator = p.m_animator;
        bool inTransition = animator.IsInTransition(0);
        AnimatorStateInfo current = animator.GetCurrentAnimatorStateInfo(0);

        if (CancelledState.HasValue)
        {
            int next = inTransition ? animator.GetNextAnimatorStateInfo(0).fullPathHash : 0;
            if (SpeedSkip.ShouldForget(CancelledState, Relaunched, current.fullPathHash, next))
            {
                CancelledState = null;
                Relaunched = false;
            }
        }

        if (!InFlight)
        {
            return;
        }

        bool currentIsEmote = current.tagHash == TagEmote;
        bool nextIsEmote = inTransition && animator.GetNextAnimatorStateInfo(0).tagHash == TagEmote;
        if (_emoteTrigger != null && RecoveryPolicy.EmoteTriggerSpent(currentIsEmote, nextIsEmote))
        {
            ResetEmoteTrigger(animator);
        }

        float now = Time.time;
        if (currentIsEmote && !inTransition)
        {
            if (_fullyInEmoteSince < 0f)
            {
                _fullyInEmoteSince = now;
            }
        }
        else
        {
            _fullyInEmoteSince = -1f;
        }

        bool attackHeld = p.m_attackHold || p.m_secondaryAttackHold;
        float inEmoteFor = _fullyInEmoteSince < 0f ? 0f : now - _fullyInEmoteSince;
        RecoveryDecision d = RecoveryPolicy.Decide(InFlight, currentIsEmote, inTransition, attackHeld,
            inEmoteFor, now - LastCancelTime);
        if (d.Action == RecoveryAction.None)
        {
            return;
        }

        if (d.Action == RecoveryAction.Stop)
        {
            SendEmoteStop(p, animator);
        }

        ResetEmoteTrigger(animator);

        InFlight = false;
        _fullyInEmoteSince = -1f;

        if (ReviewLog.On)
        {
            ReviewLog.Write(d.Action == RecoveryAction.Stop ? "recovery_stop" : "recovery_expire",
                ("dtSinceCancel", DebugLine.Num(now - LastCancelTime)), ("reason", d.Reason));
        }
    }

    private static void ResetEmoteTrigger(Animator animator)
    {
        if (_emoteTrigger != null)
        {
            animator.ResetTrigger(_emoteTrigger);
            _emoteTrigger = null;
        }
    }

    /// <summary>Local emote_stop, then the speed plan again (restore if hit-stop is still running).</summary>
    private static void SendEmoteStop(Player p, Animator animator)
    {
        animator.SetTrigger(EmoteStop);
        float pauseTimer = p.m_animEvent != null ? p.m_animEvent.m_pauseTimer : 0f;
        ApplySpeedPlan(p, animator, pauseTimer);
    }

    /// <summary>
    /// Speed 1 so the emote blend is not stretched by a slow attack speed. Under hit-stop
    /// the freeze is left visible and only its restore value is set (it would overwrite us).
    /// </summary>
    private static string ApplySpeedPlan(Player p, Animator animator, float pauseTimer)
    {
        // pauseTimer > 0 only comes from a live m_animEvent.
        (bool setSpeed, _) = SpeedPlan.For(pauseTimer);
        if (setSpeed || p.m_animEvent == null)
        {
            animator.speed = 1f;
            return "speed";
        }

        p.m_animEvent.m_pauseSpeed = 1f;
        return "restore";
    }
}
