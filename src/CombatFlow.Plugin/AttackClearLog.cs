using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Reflection;
using CombatFlow.Core;
using UnityEngine;

namespace CombatFlow;

/// <summary>
/// ev=attack_cleared (Debug log, diagnosis only): who set the local player's Humanoid.m_currentAttack to
/// null. via=unequip (Humanoid.UnequipItem of the attack's weapon), via=start_failed (Humanoid.StartAttack
/// cleared it, then Attack.Start returned false), via=unknown (a max combo run aborted with reason=instance
/// and neither line fired within <see cref="AttackClear.Window"/>). Reads only; never writes game state.
/// </summary>
internal static class AttackClearLog
{
    private const int CallerCount = 3;

    private static float _lastSeen = AttackClear.Never;
    private static readonly HashSet<string> Reported = new();

    /// <summary>Humanoid.UnequipItem cleared the current attack (postfix).</summary>
    internal static void OnUnequip(ItemDrop.ItemData item)
    {
        _lastSeen = Time.time;
        ReviewLog.Write("attack_cleared", ("via", "unequip"),
            ("item", item.m_shared != null ? item.m_shared.m_name : "-"), ("caller", Callers()));
    }

    /// <summary>Humanoid.StartAttack cleared the current attack and returned false (postfix).</summary>
    internal static void OnStartFailed(Player p, bool secondary, bool inAttackBefore)
    {
        _lastSeen = Time.time;
        ReviewLog.Write("attack_cleared", ("via", "start_failed"), ("secondary", DebugLine.Bool(secondary)),
            ("stamina", Get(p.GetStamina)), ("need", Need(p, secondary)),
            ("inAttack", DebugLine.Bool(inAttackBefore)),
            ("inDodge", Get(p.InDodge)), ("canMove", Get(p.CanMove)),
            ("staggering", Get(p.IsStaggering)), ("minorAction", Get(p.InMinorAction)),
            ("caller", Callers()));
    }

    /// <summary>MaxComboRun aborted with reason=instance: log via=unknown unless a known clear explains it.</summary>
    internal static void OnInstanceAbort(Player p)
    {
        if (!AttackClear.Unexplained(Time.time, _lastSeen))
        {
            return;
        }

        ReviewLog.Write("attack_cleared", ("via", "unknown"), ("current", ComboLab.Anim(p.m_currentAttack)));
    }

    internal static void Report(string where, Exception e)
    {
        if (Reported.Add(where))
        {
            ReviewLog.Log?.LogError($"{nameof(AttackClearLog)}.{where}: {e}");
        }
    }

    /// <summary>
    /// Attack.GetAttackStamina of the attack the failed start would have made: a throwaway clone of the
    /// weapon's attack (as StartAttack clones it), given the character and weapon Attack.Start would set.
    /// </summary>
    private static string Need(Player p, bool secondary)
    {
        try
        {
            ItemDrop.ItemData? w = p.GetCurrentWeapon();
            Attack? template = w?.m_shared == null ? null
                : secondary ? w.m_shared.m_secondaryAttack : w.m_shared.m_attack;
            if (template == null)
            {
                return "-";
            }

            Attack probe = template.Clone();
            probe.m_character = p;
            probe.m_weapon = w;
            return Dp1(probe.GetAttackStamina());
        }
        catch (Exception e)
        {
            Report(nameof(Need), e);
            return "?";
        }
    }

    private static string Get(Func<bool> getter)
    {
        try
        {
            return DebugLine.Bool(getter());
        }
        catch (Exception e)
        {
            Report(getter.Method.Name, e);
            return "?";
        }
    }

    private static string Get(Func<float> getter)
    {
        try
        {
            return Dp1(getter());
        }
        catch (Exception e)
        {
            Report(getter.Method.Name, e);
            return "?";
        }
    }

    private static string Dp1(float v) => v.ToString("0.0", CultureInfo.InvariantCulture);

    /// <summary>The nearest game methods on the stack (AttackClear.Callers skips Harmony and the mod).</summary>
    private static string Callers()
    {
        try
        {
            StackFrame[] frames = new StackTrace(1, false).GetFrames() ?? Array.Empty<StackFrame>();
            var list = new List<(string? ns, string? type, string? method)>(frames.Length);
            foreach (StackFrame f in frames)
            {
                MethodBase? m = f.GetMethod();
                Type? t = m?.DeclaringType;
                list.Add((t?.Namespace, t?.Name, m?.Name));
            }

            return AttackClear.Callers(list, CallerCount);
        }
        catch (Exception e)
        {
            Report(nameof(Callers), e);
            return "?";
        }
    }
}
