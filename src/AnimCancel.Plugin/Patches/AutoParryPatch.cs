using System;
using AnimCancel.Core;
using HarmonyLib;
using UnityEngine;

namespace AnimCancel.Patches;

/// <summary>
/// Combat / Auto perfect parry. Around Character.RPC_Damage on the local player (the owner, so our client):
/// for a blockable hit from the front with a blocker, outside a dodge, the call runs with IsBlocking() forced
/// true for this player and m_blockTimer at 0 (BlockAttack's perfect window), so the game takes its own block
/// branch as a perfect block. m_blockTimer is restored exactly in the finalizer, which runs even when the
/// original throws. Within that call the block check is the only IsBlocking caller (verified in the IL).
/// Option off or mod disabled: nothing is read or written.
/// </summary>
[HarmonyPatch(typeof(Character), nameof(Character.RPC_Damage))]
internal static class AutoParryPatch
{
    /// <summary>The player whose IsBlocking is forced; non-null only for the duration of one RPC_Damage call.</summary>
    internal static Humanoid? Forced;

    private static bool _reported;

    internal struct State
    {
        internal Player? Player;
        internal bool Active;
        internal float Timer;
        internal float TimedBlockBonus;
        internal float StaggerBefore;
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
            if (!s.Enabled || Forced != null || hit == null
                || !(__instance is Player p) || !ReferenceEquals(p, Player.m_localPlayer))
            {
                return;
            }

            bool log = ReviewLog.On;
            if (!s.AutoParry && !log)
            {
                return;
            }

            bool front = AutoParry.FromFront(Vector3.Dot(hit.m_dir, p.transform.forward));
            ItemDrop.ItemData? blocker = s.AutoParry ? p.GetCurrentBlocker() : null;
            bool inDodge = s.AutoParry && p.InDodge();
            string reason = AutoParry.Decide(s.Enabled, s.AutoParry, true, hit.m_blockable, front,
                blocker != null, inDodge);

            __state.Player = p;
            __state.Result = reason;
            if (log)
            {
                __state.Log = true;
                __state.Attacker = AttackerName(hit.GetAttacker());
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
            __state.Active = true;
            p.m_blockTimer = AutoParry.PerfectBlockTimer;
            Forced = p;
        }
        catch (Exception e)
        {
            // Undo anything set so the original runs unchanged.
            if (__state.Active && __state.Player != null)
            {
                __state.Player.m_blockTimer = __state.Timer;
            }

            Forced = null;
            __state = default;
            Report(e);
        }
    }

    // Void finalizer: runs after the original whatever happens and leaves any exception as it was.
    private static void Finalizer(HitData hit, State __state)
    {
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
                result = AutoParry.Outcome(__state.TimedBlockBonus, guardBroken);
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
/// Humanoid.IsBlocking answers true for <see cref="AutoParryPatch.Forced"/> only, i.e. only inside that one
/// RPC_Damage call; a single null check otherwise. Runs before the combo lab's measurement postfix
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
            if (AutoParryPatch.Forced != null && ReferenceEquals(__instance, AutoParryPatch.Forced))
            {
                __result = true;
            }
        }
        catch (Exception e)
        {
            AutoParryPatch.Report(e);
        }
    }
}
