using System;
using AnimCancel.Core;
using HarmonyLib;
using UnityEngine;

namespace AnimCancel.Patches;

/// <summary>
/// Prefix on CharacterAnimEvent.Speed (the clip's Speed animation event, which sets
/// animator.speed). During the blend out of a cancelled attack, that clip's own events
/// still fire (sword_secondary: Speed(0.2) just after its Hit) and would undo speed 1.
/// Local player only; skipped only while the cancelled state is current and blending.
/// </summary>
[HarmonyPatch(typeof(CharacterAnimEvent), nameof(CharacterAnimEvent.Speed), new[] { typeof(float) })]
internal static class SpeedEventPatch
{
    private static bool _reported;

    private static bool Prefix(CharacterAnimEvent __instance, float speedScale)
    {
        try
        {
            if (!Canceller.CancelledState.HasValue
                || Player.m_localPlayer == null
                || __instance.m_character != Player.m_localPlayer)
            {
                return true;
            }

            Animator animator = __instance.m_animator;
            if (animator == null
                || !SpeedSkip.ShouldSkip(Canceller.CancelledState,
                    animator.GetCurrentAnimatorStateInfo(0).fullPathHash, animator.IsInTransition(0)))
            {
                return true;
            }

            if (ReviewLog.On)
            {
                ReviewLog.Write("speed_skip", ("value", DebugLine.Num(speedScale)));
            }

            return false;
        }
        catch (Exception e)
        {
            if (!_reported)
            {
                _reported = true;
                ReviewLog.Log?.LogError($"{nameof(SpeedEventPatch)}: {e}");
            }

            return true;
        }
    }
}
