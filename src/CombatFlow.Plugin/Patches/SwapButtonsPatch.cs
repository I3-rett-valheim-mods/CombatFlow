using System;
using CombatFlow.Core;
using HarmonyLib;

namespace CombatFlow.Patches;

/// <summary>
/// Sword / Swap attack buttons: for the local player with a sword (the Max combo weapon) and
/// outside place mode, exchanges attack with secondaryAttack and attackHold with
/// secondaryAttackHold before Player.SetControls reads them. The other arguments are untouched.
/// Keyboard, mouse and gamepad all reach the game through this method.
/// </summary>
[HarmonyPatch(typeof(Player), nameof(Player.SetControls))]
internal static class SwapButtonsPatch
{
    private static bool _reported;

    /// <summary>The current SetControls call had its attack arguments swapped (read by the combo lab input line).</summary>
    internal static bool SwappedThisCall { get; private set; }

    // Ordering on Player.SetControls (Harmony runs prefixes from the highest priority down):
    //   First + 100 (900) this swap: every later prefix, ours or another mod's, sees what the game reads.
    //   First (800)       LabSetControlsPatch: logs the swapped values plus swapped=1.
    //   Normal (400)      SetControlsPatch (hold-drop): compares the swapped holds with m_attackHold /
    //                     m_secondaryAttackHold, which the game sets from those same values.
    internal const int Order = Priority.First + 100;

    [HarmonyPriority(Order)]
    private static bool Prefix(Player __instance, ref bool attack, ref bool attackHold, ref bool secondaryAttack,
        ref bool secondaryAttackHold)
    {
        SwappedThisCall = false;
        try
        {
            if (!SwapButtons.IsOn(Settings.Snapshot()) || !ReferenceEquals(__instance, Player.m_localPlayer))
            {
                return true;
            }

            Attack? primary = __instance.GetCurrentWeapon()?.m_shared?.m_attack;
            Attack? secondary = __instance.GetCurrentWeapon()?.m_shared?.m_secondaryAttack;
            bool sword = primary != null && secondary != null
                         && SwapButtons.IsSword(secondary.m_attackAnimation, primary.m_attackAnimation,
                             primary.m_attackChainLevels);
            if (!SwapButtons.ShouldSwap(true, sword, __instance.InPlaceMode()))
            {
                return true;
            }

            AttackInputs s = new AttackInputs(attack, attackHold, secondaryAttack, secondaryAttackHold).Swapped();
            attack = s.Attack;
            attackHold = s.AttackHold;
            secondaryAttack = s.Secondary;
            secondaryAttackHold = s.SecondaryHold;
            SwappedThisCall = true;
        }
        catch (Exception e)
        {
            if (!_reported)
            {
                _reported = true;
                ReviewLog.Log?.LogError($"{nameof(SwapButtonsPatch)}: {e}");
            }
        }

        return true;
    }
}
