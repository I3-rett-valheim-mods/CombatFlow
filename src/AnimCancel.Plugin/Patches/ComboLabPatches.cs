using System;
using AnimCancel.Core;
using HarmonyLib;
using UnityEngine;

namespace AnimCancel.Patches;

// Combo lab log patches: measurement only. Every prefix returns true and none writes an
// argument, a field or __result. Each returns at once unless Combo lab log is on and the
// instance is the local player. They run whatever Enabled says. Priority.First puts the
// prefixes ahead of AnimCancel's own on the same method, so they always see the call.

/// <summary>Player.SetControls: input edges (the arguments as passed in).</summary>
[HarmonyPatch(typeof(Player), nameof(Player.SetControls))]
internal static class LabSetControlsPatch
{
    [HarmonyPriority(Priority.First)]
    private static bool Prefix(Player __instance, bool attack, bool attackHold, bool secondaryAttack,
        bool secondaryAttackHold, bool block, bool blockHold)
    {
        try
        {
            if (!ComboLab.On || !ReferenceEquals(__instance, ComboLab.Local()))
            {
                return true;
            }

            ComboLab.OnInput(attack, attackHold, secondaryAttack, secondaryAttackHold, block, blockHold);
        }
        catch (Exception e)
        {
            ComboLab.Report(nameof(LabSetControlsPatch), e);
        }

        return true;
    }
}

/// <summary>Humanoid.StartAttack: every call of the local player, refused or not.</summary>
[HarmonyPatch(typeof(Humanoid), nameof(Humanoid.StartAttack), new[] { typeof(Character), typeof(bool) })]
internal static class LabStartAttackPatch
{
    internal struct Before
    {
        public bool Watch;
        public bool InAttack;
        public bool Cached;
        public bool QueuedChain;
        public float SinceLast;
        public string PrevAnim;
    }

    [HarmonyPriority(Priority.First)]
    private static bool Prefix(Humanoid __instance, out Before __state)
    {
        __state = default;
        try
        {
            if (!ComboLab.On)
            {
                return true;
            }

            Player? p = ComboLab.Local();
            if (p == null || !ReferenceEquals(__instance, p))
            {
                return true;
            }

            bool inAttack = ComboLab.InAttackNoCache(p, out bool cached);
            __state = new Before
            {
                Watch = true,
                InAttack = inAttack,
                Cached = cached,
                QueuedChain = p.HaveQueuedChain(), // Player override: reads fields only
                SinceLast = p.m_timeSinceLastAttack, // > 0.2 s resets the chain (Attack.Start)
                PrevAnim = ComboLab.Anim(p.m_currentAttack),
            };
        }
        catch (Exception e)
        {
            ComboLab.Report(nameof(LabStartAttackPatch) + ".Prefix", e);
        }

        return true;
    }

    private static void Postfix(Humanoid __instance, bool secondaryAttack, bool __result, Before __state)
    {
        try
        {
            if (!__state.Watch)
            {
                return;
            }

            Attack? atk = __result ? __instance.m_currentAttack : null;
            ComboLab.Write("start",
                ("secondary", LabLine.Bit(secondaryAttack)), ("result", LabLine.Bit(__result)),
                ("inAttackBefore", LabLine.Bit(__state.InAttack)), ("cached", LabLine.Bit(__state.Cached)),
                ("queuedChain", LabLine.Bit(__state.QueuedChain)),
                ("sinceLast", LabLine.Dp(__state.SinceLast, 3)),
                ("anim", ComboLab.Anim(atk)),
                ("lvl", atk != null ? atk.m_currentAttackCainLevel.ToString() : "-"),
                ("levels", atk != null ? atk.m_attackChainLevels.ToString() : "-"),
                ("prevAnim", __state.PrevAnim));
        }
        catch (Exception e)
        {
            ComboLab.Report(nameof(LabStartAttackPatch) + ".Postfix", e);
        }
    }
}

/// <summary>ZSyncAnimation.SetTrigger: triggers the local player sends (attack animations, emotes).</summary>
[HarmonyPatch(typeof(ZSyncAnimation), nameof(ZSyncAnimation.SetTrigger), new[] { typeof(string) })]
internal static class LabTriggerPatch
{
    [HarmonyPriority(Priority.First)]
    private static bool Prefix(ZSyncAnimation __instance, string name)
    {
        try
        {
            if (!ComboLab.On)
            {
                return true;
            }

            Player? p = ComboLab.Local();
            if (p == null || !ReferenceEquals(__instance, p.m_zanim))
            {
                return true;
            }

            ComboLab.Write("trigger", ("name", name));
        }
        catch (Exception e)
        {
            ComboLab.Report(nameof(LabTriggerPatch), e);
        }

        return true;
    }
}

/// <summary>
/// Humanoid.OnAttackTrigger: one line per swing of the local player (the hit frame of the animation), whatever
/// it hits. mult is what Attack would apply to a hit of this swing. Postfix only, reads fields and the Animator.
/// </summary>
[HarmonyPatch(typeof(Humanoid), nameof(Humanoid.OnAttackTrigger))]
internal static class LabSwingPatch
{
    [HarmonyPriority(Priority.Last)]
    private static void Postfix(Humanoid __instance)
    {
        try
        {
            if (!ComboLab.On || !ReferenceEquals(__instance, ComboLab.Local()))
            {
                return;
            }

            Attack? atk = __instance.m_currentAttack;
            if (atk == null)
            {
                return;
            }

            int lvl = atk.m_currentAttackCainLevel;
            int levels = atk.m_attackChainLevels;
            float mult = LabLine.SwingMultiplier(atk.m_damageMultiplier, atk.m_lastChainDamageMultiplier, lvl, levels);
            int cur = 0;
            int next = 0;
            Animator? a = __instance.m_animator;
            if (a != null)
            {
                cur = a.GetCurrentAnimatorStateInfo(0).fullPathHash;
                next = a.IsInTransition(0) ? a.GetNextAnimatorStateInfo(0).fullPathHash : 0;
            }

            ComboLab.Write("swing",
                ("anim", ComboLab.Anim(atk)),
                ("secondary", LabLine.Bit(__instance.m_currentAttackIsSecondary)),
                ("lvl", lvl.ToString()),
                ("levels", levels.ToString()),
                ("mult", LabLine.Dp(mult, 2)),
                ("state", cur.ToString()),
                ("next", next.ToString()));
        }
        catch (Exception e)
        {
            ComboLab.Report(nameof(LabSwingPatch), e);
        }
    }
}

/// <summary>Humanoid.IsBlocking: the value of the game's own calls (UpdateBlock every tick).</summary>
[HarmonyPatch(typeof(Humanoid), nameof(Humanoid.IsBlocking))]
internal static class LabBlockingPatch
{
    private static void Postfix(Humanoid __instance, bool __result)
    {
        try
        {
            if (!ComboLab.On || !ReferenceEquals(__instance, ComboLab.Local()))
            {
                return;
            }

            ComboLab.OnBlocking(__result);
        }
        catch (Exception e)
        {
            ComboLab.Report(nameof(LabBlockingPatch), e);
        }
    }
}

/// <summary>Character.Damage: hits dealt by the local player (raw total, before resistances and block).</summary>
[HarmonyPatch(typeof(Character), nameof(Character.Damage), new[] { typeof(HitData) })]
internal static class LabHitPatch
{
    [HarmonyPriority(Priority.First)]
    private static bool Prefix(Character __instance, HitData hit)
    {
        try
        {
            if (!ComboLab.On || hit == null)
            {
                return true;
            }

            Player? p = ComboLab.Local();
            if (p == null || !ReferenceEquals(hit.GetAttacker(), p))
            {
                return true;
            }

            Attack? atk = p.m_currentAttack;
            int lvl = atk != null ? atk.m_currentAttackCainLevel : -1;
            int levels = atk != null ? atk.m_attackChainLevels : 0;
            string target = string.IsNullOrEmpty(__instance.m_name) ? __instance.gameObject.name : __instance.m_name;
            ComboLab.Write("hit",
                ("dmg", LabLine.Dp(hit.GetTotalDamage(), 1)),
                ("target", target),
                ("anim", ComboLab.Anim(atk)),
                ("lvl", atk != null ? lvl.ToString() : "-"),
                ("levels", atk != null ? levels.ToString() : "-"),
                ("secondary", LabLine.Bit(p.m_currentAttackIsSecondary)),
                ("lastChain", LabLine.Bit(LabLine.IsLastChain(lvl, levels))));
        }
        catch (Exception e)
        {
            ComboLab.Report(nameof(LabHitPatch), e);
        }

        return true;
    }
}
