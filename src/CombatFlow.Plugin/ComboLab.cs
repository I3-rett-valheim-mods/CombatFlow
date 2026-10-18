using System;
using System.Collections.Generic;
using System.Linq;
using CombatFlow.Core;
using UnityEngine;

namespace CombatFlow;

/// <summary>
/// Combo lab log (measurement only): writes "CL ..." lines (see LabLine) for the local
/// player's inputs, attack starts, ZSyncAnimation triggers, layer-0 state changes,
/// blocking, swings and hits. Works with Enabled off; needs Debug log as well as Combo lab log (LogGate). Never writes a game field, never calls a
/// game method that caches state (Player.InAttack, IsBlocking): everything is read from
/// fields or the Animator, or observed in a postfix of the game's own call.
/// ff is MonoUpdaters.UpdateCount, the key Player.InAttack caches on (bumped once per
/// FixedUpdate and once per Update).
/// </summary>
internal static class ComboLab
{
    private static readonly int TagAttack = Animator.StringToHash("attack");
    private static readonly int TagEmote = Animator.StringToHash("emote");
    private static readonly int TagDodge = Animator.StringToHash("dodge");
    private static readonly int TagIdle = Animator.StringToHash("idle");

    private static Player? _player;
    private static int _inputMask = -1;
    private static int _block = -1;
    private static bool _stateKnown;
    private static int _cur;
    private static int _next;
    private static readonly HashSet<string> Reported = new();

    /// <summary>Cheap check first in every lab hook.</summary>
    internal static bool On =>
        ReviewLog.Log != null && LogGate.Allows(LogKind.Lab, Plugin.Current.DebugLog, Settings.ComboLabLog);

    internal static void Write(string ev, params (string k, string v)[] kv)
    {
        if (On)
        {
            ReviewLog.Log!.LogInfo(LabLine.Format(Time.time, Time.frameCount, MonoUpdaters.UpdateCount, ev, kv));
        }
    }

    /// <summary>The local player, or null; forgets the remembered values when it changes.</summary>
    internal static Player? Local()
    {
        Player p = Player.m_localPlayer;
        if (!ReferenceEquals(p, _player))
        {
            _player = p;
            _inputMask = -1;
            _block = -1;
            _stateKnown = false;
        }

        return p == null ? null : p;
    }

    /// <summary>Plugin.Update: layer-0 state changes.</summary>
    internal static void Tick()
    {
        try
        {
            if (!On)
            {
                _player = null; // turning the lab back on logs the first values again
                return;
            }

            Player? p = Local();
            Animator? a = p != null ? p.m_animator : null;
            if (a == null)
            {
                return;
            }

            AnimatorStateInfo cur = a.GetCurrentAnimatorStateInfo(0);
            int next = a.IsInTransition(0) ? a.GetNextAnimatorStateInfo(0).fullPathHash : 0;
            if (_stateKnown && cur.fullPathHash == _cur && next == _next)
            {
                return;
            }

            _stateKnown = true;
            _cur = cur.fullPathHash;
            _next = next;
            Write("state", ("cur", _cur.ToString()), ("next", _next.ToString()),
                ("curTag", LabLine.TagName(cur.tagHash, TagAttack, TagEmote, TagDodge, TagIdle)),
                ("t01", LabLine.Dp(cur.normalizedTime, 2)));
        }
        catch (Exception e)
        {
            Report(nameof(Tick), e);
        }
    }

    /// <summary>Player.SetControls arguments as the game receives them (after Swap attack buttons, before the
    /// game clears them for emotes). <paramref name="swapped"/> adds swapped=1.</summary>
    internal static void OnInput(bool attack, bool attackHold, bool secondary, bool secondaryHold, bool block, bool blockHold,
        bool swapped)
    {
        // Bit 64: a swap toggle alone changes the raw input, so it is a new line too.
        int mask = LabLine.InputMask(attack, attackHold, secondary, secondaryHold, block, blockHold) | (swapped ? 64 : 0);
        if (mask == _inputMask)
        {
            return;
        }

        _inputMask = mask;
        (string k, string v)[] kv =
        {
            ("attack", LabLine.Bit(attack)), ("attackHold", LabLine.Bit(attackHold)),
            ("secondary", LabLine.Bit(secondary)), ("secondaryHold", LabLine.Bit(secondaryHold)),
            ("block", LabLine.Bit(block)), ("blockHold", LabLine.Bit(blockHold)),
        };
        Write("input", swapped ? kv.Concat(SwapButtons.LabFields(true)).ToArray() : kv);
    }

    /// <summary>The value the game's own Humanoid.IsBlocking call returned.</summary>
    internal static void OnBlocking(bool on)
    {
        int v = on ? 1 : 0;
        if (v == _block)
        {
            return;
        }

        _block = v;
        Write("block", ("on", LabLine.Bit(on)));
    }

    /// <summary>
    /// What Player.InAttack would answer now, without touching its cache: the cached value
    /// when it is for this update count, otherwise the same Animator test it would run.
    /// </summary>
    internal static bool InAttackNoCache(Player p, out bool cached)
    {
        cached = p.m_cachedFrame == MonoUpdaters.UpdateCount;
        if (cached)
        {
            return p.m_cachedAttack;
        }

        // Player.InAttack's rule, done here without writing its cache. This mirror reads layer 0 live,
        // while the game reads its per-update cached next-or-current tag, so during a transition the two
        // can differ for one frame. Diagnostic only; layers 1.. use the same rule.
        Animator a = p.m_animator;
        for (int i = 0; i < a.layerCount; i++)
        {
            int tag = a.IsInTransition(i) ? a.GetNextAnimatorStateInfo(i).tagHash : a.GetCurrentAnimatorStateInfo(i).tagHash;
            if (tag == TagAttack)
            {
                return true;
            }
        }

        return false;
    }

    internal static string Anim(Attack? atk) => atk != null ? atk.m_attackAnimation : "-";

    internal static void Report(string where, Exception e)
    {
        if (Reported.Add(where))
        {
            ReviewLog.Log?.LogError($"{nameof(ComboLab)}.{where}: {e}");
        }
    }
}
