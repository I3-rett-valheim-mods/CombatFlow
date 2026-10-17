using AnimCancel.Core;
using UnityEngine;

namespace AnimCancel;

/// <summary>
/// Fires the cancel: an emote trigger on the local Animator only. Never ZSyncAnimation,
/// never StartEmote, never the ZDO, so other players see nothing.
/// </summary>
internal static class Canceller
{
    /// <summary>True from a cancel until the next attack starts (read by later tasks).</summary>
    internal static bool InFlight;

    /// <summary>Time.time of the last cancel.</summary>
    internal static float LastCancelTime;

    public static void Cancel(Player p, CancelEmote e)
    {
        Animator animator = p.m_animator;
        if (animator == null)
        {
            return;
        }

        float speedBefore = animator.speed;
        animator.SetTrigger(CancelEmotes.Trigger(e));
        ApplySpeedPlan(p, animator);

        InFlight = true;
        LastCancelTime = Time.time;

        if (ReviewLog.On)
        {
            Attack? atk = p.m_currentAttack;
            float pauseTimer = p.m_animEvent != null ? p.m_animEvent.m_pauseTimer : 0f;
            ReviewLog.Write("cancel",
                ("kind", AttackClassifier.Classify(atk?.m_attackAnimation).ToString()),
                ("lvl", (atk?.m_currentAttackCainLevel ?? 0).ToString()),
                ("emote", e.ToString()),
                ("pauseTimer", DebugLine.Num(pauseTimer)),
                ("animSpeedBefore", DebugLine.Num(speedBefore)));
        }
    }

    /// <summary>
    /// Called on the first attack start after a cancel: ends the in-flight state and
    /// gives the time since the cancel.
    /// </summary>
    internal static bool TryConsumeRelaunch(out float dtSinceCancel)
    {
        dtSinceCancel = 0f;
        if (!InFlight)
        {
            return false;
        }

        InFlight = false;
        dtSinceCancel = Time.time - LastCancelTime;
        return true;
    }

    /// <summary>
    /// Speed after the trigger so the emote blend is not stretched by a slow attack speed.
    /// Task 6 replaces this with the speed plan (FreezeFrame restore etc.).
    /// </summary>
    private static void ApplySpeedPlan(Player p, Animator animator)
    {
        animator.speed = 1f;
    }
}
