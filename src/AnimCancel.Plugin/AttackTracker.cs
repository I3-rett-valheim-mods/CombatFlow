using System;
using AnimCancel.Core;
using UnityEngine;

namespace AnimCancel;

/// <summary>
/// Follows the local player's attacks: logs starts and relaunches, counts damage events
/// per Attack instance, asks <see cref="CancelPolicy"/> after each one and fires (or
/// schedules, with Delay after hit) the cancel.
/// </summary>
internal static class AttackTracker
{
    private static readonly ClipTiming NoTiming = new(Array.Empty<float>(), null);
    private static readonly DamageCounter Counter = new();

    private static Player? _pendingPlayer;
    private static Attack? _pendingAttack;
    private static CancelEmote _pendingEmote;
    private static float _pendingDue;

    /// <summary>Humanoid.StartAttack returned true for the local player.</summary>
    internal static void OnAttackStarted(Player player)
    {
        Attack? atk = player.m_currentAttack;
        bool relaunch = Canceller.TryConsumeRelaunch(out float dt);
        if (atk == null || !ReviewLog.On)
        {
            return;
        }

        AttackKind kind = AttackClassifier.Classify(atk.m_attackAnimation);
        if (kind == AttackKind.None)
        {
            return;
        }

        string lvl = atk.m_currentAttackCainLevel.ToString();
        // The Animator has only been given the trigger; the clip is known from the first damage line.
        ReviewLog.Write("attack_start",
            ("kind", kind.ToString()), ("lvl", lvl), ("levels", atk.m_attackChainLevels.ToString()),
            ("clip", "-"), ("stateSpeed", "-"), ("trigger", TriggerName(atk)));

        if (relaunch)
        {
            ReviewLog.Write("relaunch", ("kind", kind.ToString()), ("lvl", lvl), ("dtSinceCancel", DebugLine.Num(dt)));
        }
    }

    /// <summary>Humanoid.OnAttackTrigger ran (damage already dealt) for the local player.</summary>
    internal static void OnDamageEvent(Player player, CancelSettings s)
    {
        Attack? atk = player.m_currentAttack;
        if (atk == null || player.GetCurrentWeapon() == null || player.m_animator == null)
        {
            return;
        }

        AttackKind kind = AttackClassifier.Classify(atk.m_attackAnimation);
        if (kind == AttackKind.None)
        {
            return;
        }

        int seen = Counter.Register(atk);
        ClipTiming timing = ClipTimingCache.ForCurrent(player.m_animator, out float stateSpeed, out string clip, out float clipT,
                                out string clipPick)
                            ?? NoTiming;
        int lvl = atk.m_currentAttackCainLevel;
        // Max combo (option on, sword secondary or berserker axes finisher) replaces the plain policy:
        // steps 1-2 never cancel, step 3 does.
        bool clipIsNext = clipPick == ClipPick.LogValue(ClipPickResult.Next);
        // Any hit max combo does not act on gets the plain policy, counted per swing when the run counted it.
        CancelDecision d = MaxComboRun.OnHit(player, atk, kind, s, clip, clipIsNext, out bool maxComboCancel,
            out int swingHit)
            ? new CancelDecision(maxComboCancel, maxComboCancel ? "ok" : "maxcombo")
            : CancelPolicy.ShouldCancel(kind, timing, swingHit > 0 ? swingHit : seen,
                stateSpeed, s);
        if (s.DelayAfterHit <= 0f)
        {
            // The delayed path checks the stagger at its due time instead.
            d = CancelPolicy.ShouldFireNow(d, player.IsStaggering());
        }

        if (ReviewLog.On)
        {
            ReviewLog.Write("damage",
                ("kind", kind.ToString()), ("lvl", lvl.ToString()),
                ("n", $"{seen}/{timing.DamageEventTimes.Count}"), ("clipT", DebugLine.Num(clipT)),
                ("clip", clip), ("stateSpeed", DebugLine.Num(stateSpeed)), ("clipPick", clipPick));
            LogDecision(kind, lvl, d.Cancel, d.Reason);
        }

        if (!d.Cancel)
        {
            return;
        }

        if (s.DelayAfterHit > 0f)
        {
            _pendingPlayer = player;
            _pendingAttack = atk;
            _pendingEmote = s.Emote;
            _pendingDue = Time.time + s.DelayAfterHit;
        }
        else
        {
            Canceller.Cancel(player, s.Emote);
        }
    }

    /// <summary>Called every frame from Plugin.Update: recovery after a cancel, and a delayed cancel when due.</summary>
    internal static void Tick()
    {
        try
        {
            TickCore();
        }
        catch (Exception e)
        {
            if (!_tickReported)
            {
                _tickReported = true;
                ReviewLog.Log?.LogError($"{nameof(AttackTracker)}.{nameof(Tick)}: {e}");
            }
        }
    }

    private static bool _tickReported;

    private static void TickCore()
    {
        // First, so a run is aborted (reason disabled) when the mod is turned off.
        MaxComboRun.Tick(Plugin.Current);

        if (!Plugin.Current.Enabled)
        {
            // Disabled: nothing may stay pending, or the first relaunch after re-enabling
            // would report a stale dtSinceCancel and a stale delayed cancel could fire.
            _pendingPlayer = null;
            _pendingAttack = null;
            Canceller.Disable();
            return;
        }

        Canceller.TickRecovery();

        if (_pendingAttack == null || Time.time < _pendingDue)
        {
            return;
        }

        Player? player = _pendingPlayer;
        Attack atk = _pendingAttack;
        CancelEmote emote = _pendingEmote;
        _pendingPlayer = null;
        _pendingAttack = null;

        bool samePlayer = player != null && player == Player.m_localPlayer;
        CancelDecision d = CancelPolicy.ShouldFireDelayed(
            enabled: true,
            sameAttack: samePlayer && player!.m_currentAttack == atk,
            staggering: samePlayer && player!.IsStaggering(),
            inAttack: samePlayer && player!.InAttack());

        if (!d.Cancel)
        {
            if (ReviewLog.On)
            {
                LogDecision(AttackClassifier.Classify(atk.m_attackAnimation), atk.m_currentAttackCainLevel, false, d.Reason);
            }
            return;
        }

        Canceller.Cancel(player!, emote);
    }

    private static void LogDecision(AttackKind kind, int lvl, bool cancel, string reason) =>
        ReviewLog.Write("decision",
            ("kind", kind.ToString()), ("lvl", lvl.ToString()),
            ("cancel", DebugLine.Bool(cancel)), ("reason", reason));

    /// <summary>Mirrors the trigger Attack.Start sets (random-animation variants unknown here).</summary>
    private static string TriggerName(Attack atk) =>
        atk.m_attackChainLevels > 1 ? atk.m_attackAnimation + atk.m_currentAttackCainLevel : atk.m_attackAnimation;
}
