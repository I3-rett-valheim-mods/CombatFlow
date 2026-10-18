using System;
using HarmonyLib;

namespace CombatFlow.Patches;

/// <summary>
/// Humanoid.UnequipItem (diagnosis only, Debug log): when it clears the local player's current attack
/// (the item is that attack's weapon), log ev=attack_cleared via=unequip. Never skips or changes the call.
/// </summary>
[HarmonyPatch(typeof(Humanoid), nameof(Humanoid.UnequipItem), new[] { typeof(ItemDrop.ItemData), typeof(bool) })]
internal static class UnequipItemPatch
{
    private static void Prefix(Humanoid __instance, ItemDrop.ItemData item, out bool __state)
    {
        __state = false;
        try
        {
            if (!ReviewLog.On || item == null || __instance != Player.m_localPlayer)
            {
                return;
            }

            Attack? atk = __instance.m_currentAttack;
            __state = atk != null && atk.GetWeapon() == item;
        }
        catch (Exception e)
        {
            AttackClearLog.Report(nameof(UnequipItemPatch) + ".Prefix", e);
        }
    }

    private static void Postfix(Humanoid __instance, ItemDrop.ItemData item, bool __state)
    {
        try
        {
            if (__state && __instance.m_currentAttack == null)
            {
                AttackClearLog.OnUnequip(item);
            }
        }
        catch (Exception e)
        {
            AttackClearLog.Report(nameof(UnequipItemPatch) + ".Postfix", e);
        }
    }
}
