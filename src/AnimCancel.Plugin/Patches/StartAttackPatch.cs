using System;
using HarmonyLib;

namespace AnimCancel.Patches;

/// <summary>
/// Postfix on Humanoid.StartAttack: the real start of each attack, for the review log
/// (attack_start, and relaunch after a cancel).
/// </summary>
[HarmonyPatch(typeof(Humanoid), nameof(Humanoid.StartAttack), new[] { typeof(Character), typeof(bool) })]
internal static class StartAttackPatch
{
    private static bool _reported;

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
