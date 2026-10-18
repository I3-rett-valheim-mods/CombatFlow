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

        // Same rule as ClipTimingCache: the next state when blending into the cancelled state.
        bool useNext = animator.IsInTransition(0)
                       && animator.GetNextAnimatorStateInfo(0).tagHash == (stateTag ?? TagAttack);
        CancelledState = useNext
            ? animator.GetNextAnimatorStateInfo(0).fullPathHash
            : animator.GetCurrentAnimatorStateInfo(0).fullPathHash;
        Relaunched = false;

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
        // Do not let an unconsumed emote_stop or emote trigger wait for the next transition.
        Player? p = Player.m_localPlayer;
        if (p != null && p.m_animator != null)
        {
            p.m_animator.ResetTrigger(EmoteStop);
            ResetEmoteTrigger(p.m_animator);
        }

        return true;
    }

    /// <summary>Drops everything (plugin disabled).</summary>
    internal static void Reset()
    {
        InFlight = false;
        CancelledState = null;
        Relaunched = false;
        _fullyInEmoteSince = -1f;
        _emoteTrigger = null;
    }

    /// <summary>
    /// Every frame: forgets the cancelled state once it left layer 0, and stops the emote
    /// when the player is fully in it with no attack held, or held for longer than
    /// RecoveryPolicy.HeldGrace with no relaunch; drops a stale in-flight state after
    /// RecoveryPolicy.Expiry. Works for a dodge cancel too (no Attack needed).
    /// </summary>
    internal static void TickRecovery()
    {
        if (!InFlight && !CancelledState.HasValue)
        {
            return;
        }

        Player? p = Player.m_localPlayer;
        if (p == null || p.m_animator == null)
        {
            Reset();
            return;
        }

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
            StopEmote(p, animator);
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
    private static void StopEmote(Player p, Animator animator)
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
