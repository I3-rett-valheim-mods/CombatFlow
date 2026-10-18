using System;
using HarmonyLib;

namespace AnimCancel.Patches;

/// <summary>
/// Humanoid.StartAttack. Prefix: refuses the held button's duplicate starts during the blend
/// into the cancel emote, after the relaunch (Canceller.RefuseStartAttack, local player only).
/// Postfix: the real start of each attack, for the review log (attack_start, and relaunch
/// after a cancel), and ev=attack_cleared via=start_failed when the call cleared the current
/// attack and Attack.Start then failed (Debug log, diagnosis only).
/// </summary>
[HarmonyPatch(typeof(Humanoid), nameof(Humanoid.StartAttack), new[] { typeof(Character), typeof(bool) })]
internal static class StartAttackPatch
{
    private static bool _reported;
    private static bool _prefixReported;

    /// <summary>What the prefix saw, for ev=attack_cleared via=start_failed.</summary>
    internal struct Before
    {
        public bool HadAttack;
        public bool InAttack;
    }

    /// <summary>Callers (Player.UpdateAttack, bow draw) only use the bool: false is "could not start".</summary>
    private static bool Prefix(Humanoid __instance, ref bool __result, out Before __state)
    {
        __state = default;
        try
        {
            if (ReviewLog.On && __instance.m_currentAttack != null && __instance == Player.m_localPlayer)
            {
                Player p = Player.m_localPlayer;
                // Player.InAttack's cached answer, without writing its cache.
                bool inAttack = p.m_animator != null && ComboLab.InAttackNoCache(p, out _);
                __state = new Before { HadAttack = true, InAttack = inAttack };
            }
        }
        catch (Exception e)
        {
            AttackClearLog.Report(nameof(StartAttackPatch) + ".Prefix", e);
        }

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

    private static void Postfix(Humanoid __instance, bool secondaryAttack, bool __result, Before __state)
    {
        try
        {
            if (__state.HadAttack && !__result && __instance.m_currentAttack == null
                && __instance == Player.m_localPlayer)
            {
                AttackClearLog.OnStartFailed(Player.m_localPlayer, secondaryAttack, __state.InAttack);
            }
        }
        catch (Exception e)
        {
            AttackClearLog.Report(nameof(StartAttackPatch) + ".Postfix", e);
        }

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
