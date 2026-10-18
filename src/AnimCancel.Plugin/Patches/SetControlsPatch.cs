using System;
using AnimCancel.Core;
using HarmonyLib;
using UnityEngine;

namespace AnimCancel.Patches;

/// <summary>
/// Measurement only (Task 8). Player.SetControls clears attack/attackHold/secondaryAttack/
/// secondaryAttackHold and calls StopEmote() while InEmote() and the player moves. This
/// prefix records the incoming hold arguments around a cancel and the postfix logs
/// hold_drop when a held button ended up false in m_attackHold / m_secondaryAttackHold.
/// It never changes an argument or a field.
/// </summary>
[HarmonyPatch(typeof(Player), nameof(Player.SetControls))]
internal static class SetControlsPatch
{
    private static bool _reported;

    internal struct State
    {
        public bool Watch;
        public bool Attack;
        public bool Secondary;
    }

    private static bool Prefix(Player __instance, bool attackHold, bool secondaryAttackHold, out State __state)
    {
        __state = default;
        try
        {
            if (!ReviewLog.On || __instance != Player.m_localPlayer)
            {
                return true;
            }

            if (HoldDrop.ShouldWatch(Canceller.InFlight, Time.time, Canceller.LastCancelTime))
            {
                __state = new State { Watch = true, Attack = attackHold, Secondary = secondaryAttackHold };
            }
        }
        catch (Exception e)
        {
            Report(e);
        }

        return true;
    }

    private static void Postfix(Player __instance, State __state)
    {
        try
        {
            if (!__state.Watch)
            {
                return;
            }

            if (HoldDrop.WasDropped(__state.Attack, __instance.m_attackHold))
            {
                ReviewLog.Write("hold_drop", ("which", "attack"));
            }

            if (HoldDrop.WasDropped(__state.Secondary, __instance.m_secondaryAttackHold))
            {
                ReviewLog.Write("hold_drop", ("which", "secondary"));
            }
        }
        catch (Exception e)
        {
            Report(e);
        }
    }

    private static void Report(Exception e)
    {
        if (!_reported)
        {
            _reported = true;
            ReviewLog.Log?.LogError($"{nameof(SetControlsPatch)}: {e}");
        }
    }
}
