using System;
using HarmonyLib;

namespace AnimCancel.Patches;

/// <summary>
/// Humanoid.StartAttack. Prefix: refuses the held button's duplicate starts during the blend
/// into the cancel emote, after the relaunch (Canceller.RefuseStartAttack, local player only).
/// Postfix: the real start of each attack, for the review log (attack_start, and relaunch
/// after a cancel).
/// </summary>
[HarmonyPatch(typeof(Humanoid), nameof(Humanoid.StartAttack), new[] { typeof(Character), typeof(bool) })]
internal static class StartAttackPatch
{
    private static bool _reported;
    private static bool _prefixReported;

    /// <summary>Callers (Player.UpdateAttack, bow draw) only use the bool: false is "could not start".</summary>
    private static bool Prefix(Humanoid __instance, ref bool __result)
    {
        try
        {
            if (__instance != Player.m_localPlayer || !Plugin.Current.Enabled
                || !Canceller.RefuseStartAttack(__instance))
            {
                return true;
            }

            __result = false;
            return false;
        }
        catch (Exception e)
        {
            if (!_prefixReported)
            {
                _prefixReported = true;
                ReviewLog.Log?.LogError($"{nameof(StartAttackPatch)}.{nameof(Prefix)}: {e}");
            }

            return true;
        }
    }

    private static void Postfix(Humanoid __instance, bool __result)
    {
        try
        {
            if (!__result || __instance != Player.m_localPlayer || !Plugin.Current.Enabled)
            {
                return;
            }

            AttackTracker.OnAttackStarted(Player.m_localPlayer);
        }
        catch (Exception e)
        {
            if (!_reported)
            {
                _reported = true;
                ReviewLog.Log?.LogError($"{nameof(StartAttackPatch)}: {e}");
            }
        }
    }
}
