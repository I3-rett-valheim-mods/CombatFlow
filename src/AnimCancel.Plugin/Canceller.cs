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

        // A previous emote_stop can stay latched when a dodge/attack wins via Any State in the
        // same frame; it would then cut this emote on entry.
        animator.ResetTrigger(EmoteStop);
        animator.SetTrigger(CancelEmotes.Trigger(e));
        string plan = ApplySpeedPlan(p, animator, pauseTimer);

        InFlight = true;
        LastCancelTime = Time.time;

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
        // A relaunch of the same state shares its hash; its own Speed events must stay vanilla.
        CancelledState = null;
        if (!InFlight)
        {
            return false;
        }

        InFlight = false;
        dtSinceCancel = Time.time - LastCancelTime;
        // Do not let an unconsumed emote_stop wait for the next emote.
        Player? p = Player.m_localPlayer;
        if (p != null && p.m_animator != null)
        {
            p.m_animator.ResetTrigger(EmoteStop);
        }

        return true;
    }

    /// <summary>Drops everything (plugin disabled).</summary>
    internal static void Reset()
    {
        InFlight = false;
        CancelledState = null;
    }

    /// <summary>
    /// Every frame: forgets the cancelled state once it left layer 0, and stops the emote
    /// when the player is fully in it with no attack held. Works for a dodge cancel too
    /// (no Attack needed).
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
            if (SpeedSkip.ShouldForget(CancelledState, current.fullPathHash, next))
            {
                CancelledState = null;
            }
        }

        bool attackHeld = p.m_attackHold || p.m_secondaryAttackHold;
        if (!RecoveryPolicy.ShouldStop(InFlight, current.tagHash == TagEmote, inTransition, attackHeld))
        {
            return;
        }

        animator.SetTrigger(EmoteStop);
        float pauseTimer = p.m_animEvent != null ? p.m_animEvent.m_pauseTimer : 0f;
        ApplySpeedPlan(p, animator, pauseTimer);
        InFlight = false;

        if (ReviewLog.On)
        {
            ReviewLog.Write("recovery_stop", ("dtSinceCancel", DebugLine.Num(Time.time - LastCancelTime)));
        }
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
