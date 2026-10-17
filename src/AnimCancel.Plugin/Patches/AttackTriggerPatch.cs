using System;
using HarmonyLib;

namespace AnimCancel.Patches;

/// <summary>
/// Postfix on Humanoid.OnAttackTrigger (animation events Hit / OnAttackTrigger). The game
/// deals the damage inside the original (Attack.OnAttackTrigger → DoMeleeAttack), so by
/// the time this runs the blow has landed.
/// </summary>
[HarmonyPatch(typeof(Humanoid), nameof(Humanoid.OnAttackTrigger))]
internal static class AttackTriggerPatch
{
    private static bool _reported;

    private static void Postfix(Humanoid __instance)
    {
        try
        {
            if (__instance != Player.m_localPlayer || __instance.m_currentAttack == null)
            {
                return;
            }

            Core.CancelSettings s = Plugin.Current;
            if (!s.Enabled)
            {
                return;
            }

            AttackTracker.OnDamageEvent(Player.m_localPlayer, s);
        }
        catch (Exception e)
        {
            if (!_reported)
            {
                _reported = true;
                ReviewLog.Log?.LogError($"{nameof(AttackTriggerPatch)}: {e}");
            }
        }
    }
}
