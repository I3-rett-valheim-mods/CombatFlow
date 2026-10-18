using System;
using CombatFlow.Core;
using HarmonyLib;
using UnityEngine;

namespace CombatFlow.Patches;

/// <summary>
/// Combat / Auto perfect parry. Around Character.RPC_Damage on the local player (the owner, so our client):
/// for a blockable hit from the front with a blocker, the call runs with IsBlocking() answering vanilla's rules
/// minus the block button and !InAttack() for this player and m_blockTimer at 0 (BlockAttack's perfect window), so the game takes its own block
/// branch as a perfect block. m_blockTimer is restored exactly in the finalizer, which runs even when the
/// original throws. Within that call the block check is the only IsBlocking caller (verified in the IL).
/// Option off or mod disabled: nothing is read or written.
/// </summary>
[HarmonyPatch(typeof(Character), nameof(Character.RPC_Damage))]
internal static class AutoParryPatch
{
    /// <summary>The player whose IsBlocking is forced; non-null only for the duration of one RPC_Damage call.</summary>
    internal static Humanoid? Forced;

    /// <summary>Nesting of RPC_Damage calls on the local player; a block is forced only at depth 1.</summary>
    internal static int Depth;

    /// <summary>The forced IsBlocking answered true during the current call (vanilla's other rules allowed it).</summary>
    internal static bool ForcedTrue;

    private static bool _reported;

    internal struct State
    {
        internal bool Counted;
        internal Player? Player;
        internal bool Active;
        internal float Timer;
        internal float TimedBlockBonus;
        internal float StaggerBefore;
        internal bool HasAttacker;
        internal bool Log;
        internal string? Result;
        internal string? Attacker;
        internal bool Blockable;
        internal bool Front;
        internal float DmgBefore;
    }

    private static void Prefix(Character __instance, HitData hit, out State __state)
    {
        __state = default;
        try
        {
            CancelSettings s = Plugin.Current;
            if (!s.Enabled || hit == null || !(__instance is Player p) || !ReferenceEquals(p, Player.m_localPlayer))
            {
                return;
            }

            Depth++;
            __state.Counted = true;
            bool log = ReviewLog.On;
            if (Depth != 1 || (!s.AutoParry && !log))
            {
                return;
            }

            bool front = AutoParry.FromFront(Vector3.Dot(hit.m_dir, p.transform.forward));
            Character? attacker = hit.GetAttacker();
            string reason = "off";
            ItemDrop.ItemData? blocker = null;
            if (s.AutoParry)
            {
                blocker = p.GetCurrentBlocker();
                reason = AutoParry.Decide(s.Enabled, s.AutoParry, true, hit.m_blockable, front, blocker != null,
                    p.InDodge(), p.InPlaceMode(), p.IsEncumbered(), p.InMinorAction(), p.IsStaggering());
            }

            __state.Player = p;
            __state.Result = reason;
            __state.HasAttacker = attacker != null;
            if (log)
            {
                __state.Log = true;
                __state.Attacker = AttackerName(attacker);
                __state.Blockable = hit.m_blockable;
                __state.Front = front;
                __state.DmgBefore = hit.GetTotalDamage();
            }

            if (reason != AutoParry.Force || blocker == null)
            {
                return;
            }

            __state.Timer = p.m_blockTimer;
            __state.TimedBlockBonus = blocker.m_shared.m_timedBlockBonus;
            __state.StaggerBefore = p.GetStaggerPercentage();
            ForcedTrue = false;
            __state.Active = true;
            p.m_blockTimer = AutoParry.PerfectBlockTimer;
            Forced = p;
        }
        catch (Exception e)
        {
            // Undo anything set so the original runs unchanged; the depth count is undone by the finalizer.
            if (__state.Active && __state.Player != null)
            {
                __state.Player.m_blockTimer = __state.Timer;
                Forced = null;
            }

            bool counted = __state.Counted;
            __state = default;
            __state.Counted = counted;
            Report(e);
        }
    }

    // Void finalizer: runs after the original whatever happens and leaves any exception as it was.
    private static void Finalizer(HitData hit, State __state)
    {
        if (__state.Counted)
        {
            Depth--;
        }

        try
        {
            Player? p = __state.Player;
            if (p == null)
            {
                return;
            }

            string? result = __state.Result;
            if (__state.Active)
            {
                Forced = null;
                p.m_blockTimer = __state.Timer;
                bool guardBroken = !p.HaveStamina()
                                   || (__state.StaggerBefore < 1f && p.GetStaggerPercentage() >= 1f);
                result = ForcedTrue
                    ? AutoParry.Outcome(__state.TimedBlockBonus, guardBroken, __state.HasAttacker)
                    : "not_forced";
            }

            if (__state.Log && ReviewLog.On)
            {
                ReviewLog.Write("parry",
                    ("attacker", __state.Attacker ?? "-"),
                    ("blockable", DebugLine.Bool(__state.Blockable)),
                    ("front", DebugLine.Bool(__state.Front)),
                    ("dmgBefore", DebugLine.Num(__state.DmgBefore)),
                    ("dmgAfter", hit != null ? DebugLine.Num(hit.GetTotalDamage()) : "-"),
                    ("result", result ?? "-"));
            }
        }
        catch (Exception e)
        {
            Forced = null;
            Report(e);
        }
    }

    private static string AttackerName(Character? attacker)
    {
        if (attacker == null)
        {
            return "-";
        }

        if (attacker is Player pl)
        {
            return pl.GetPlayerName();
        }

        return string.IsNullOrEmpty(attacker.m_name) ? attacker.gameObject.name : attacker.m_name;
    }

    internal static void Report(Exception e)
    {
        if (!_reported)
        {
            _reported = true;
            ReviewLog.Log?.LogError($"{nameof(AutoParryPatch)}: {e}");
        }
    }
}

/// <summary>
/// Humanoid.IsBlocking for <see cref="AutoParryPatch.Forced"/> only (inside that one RPC_Damage call, depth 1):
/// vanilla's own rules except the block button (m_blocking) and !InAttack(); a single null check otherwise. Runs before the combo lab's measurement postfix
/// (Priority.Last), so the lab logs the value the game's block check actually saw.
/// </summary>
[HarmonyPatch(typeof(Humanoid), nameof(Humanoid.IsBlocking))]
internal static class AutoParryBlockingPatch
{
    [HarmonyPriority(Priority.High)]
    private static void Postfix(Humanoid __instance, ref bool __result)
    {
        try
        {
            if (AutoParryPatch.Forced == null || !ReferenceEquals(__instance, AutoParryPatch.Forced)
                || AutoParryPatch.Depth != 1)
            {
                return;
            }

            // Vanilla's non-owner branch (the ZDO value) is left alone; the local player is always the owner.
            if (__instance.m_nview.IsValid() && !__instance.m_nview.IsOwner())
            {
                return;
            }

            // Vanilla minus m_blocking and !InAttack(): same calls, same order.
            __result = AutoParry.ForcedBlocking(__instance.InDodge(), __instance.InPlaceMode(),
                __instance.IsEncumbered(), __instance.InMinorAction(), __instance.IsStaggering());
            AutoParryPatch.ForcedTrue |= __result;
        }
        catch (Exception e)
        {
            AutoParryPatch.Report(e);
        }
    }
}
