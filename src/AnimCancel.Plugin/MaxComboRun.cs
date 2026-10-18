using System;
using System.Runtime.CompilerServices;
using AnimCancel.Core;
using UnityEngine;

namespace AnimCancel;

/// <summary>
/// Max combo (auto): drives <see cref="MaxComboPolicy"/> for the local player. At the sword
/// secondary's hit it sets swing_longsword1, at swing_sword 1's hit swing_longsword2 (local
/// Animator only, through <see cref="Canceller.Chain"/>), and at swing_sword 2's hit hands back a
/// normal cancel (Cycle). In Loop mode it keeps alternating swing_longsword1/2 while the secondary
/// button is held and hands back the cancel on release or at the cap. Every frame it aborts a run
/// that can no longer complete.
/// </summary>
internal static class MaxComboRun
{
    private static readonly int TagDodge = Animator.StringToHash("dodge");

    private static MaxComboState _state = MaxComboState.Idle;
    private static Attack? _attack;
    private static bool _hitReported;
    private static bool _tickReported;

    /// <summary>
    /// At a damage event of the local player. True when max combo handles the hit, replacing the
    /// plain cancel policy; <paramref name="cancelNow"/> then says whether to cancel (step 3).
    /// False on any failure, so the plain policy runs.
    /// </summary>
    internal static bool OnHit(Player p, Attack atk, AttackKind kind, CancelSettings s, string clip, bool clipIsNext,
        out bool cancelNow)
    {
        cancelNow = false;
        if (!s.MaxCombo && !_state.Running)
        {
            return false; // option off: the plain policy, untouched
        }

        bool wasRunning = _state.Running;
        try
        {
            bool eligible = kind == AttackKind.SwordSecondary && Eligible(p, atk);
            bool routes = MaxComboPolicy.Routes(kind, s, eligible);
            if (!routes && !_state.Running)
            {
                return false;
            }

            float now = Time.time;
            bool staggering = p.IsStaggering();
            bool held = p.m_secondaryAttackHold;
            MaxComboAction a = MaxComboPolicy.Decide(_state, clip, ReferenceEquals(atk, _attack), s,
                now - _state.StepTime, eligible, staggering, held);
            if (a.Kind == MaxComboActionKind.Abort)
            {
                Abort(p, a.Reason);
                // This hit may start a new run (a new secondary instance).
                a = MaxComboPolicy.Decide(_state, clip, false, s, 0f, eligible, staggering, held);
            }

            int id = RuntimeHelpers.GetHashCode(atk);
            switch (a.Kind)
            {
                case MaxComboActionKind.TriggerSwing1:
                case MaxComboActionKind.TriggerSwing2:
                    string trigger = MaxComboPolicy.TriggerFor(a.Kind)!;
                    string? plan = Canceller.Chain(p, trigger, clipIsNext, MaxComboPolicy.SwingTriggers);
                    if (plan == null)
                    {
                        return MaxComboPolicy.Handles(kind, routes, wasRunning);
                    }

                    _attack = atk;
                    _state = MaxComboPolicy.Next(_state, a, id, now);
                    LogStep(p, a.Kind, clip, _state.InstanceId, _state.Loop, plan);
                    break;
                case MaxComboActionKind.CancelNow:
                    int loop = _state.Loop;
                    int runId = _state.InstanceId;
                    _state = MaxComboPolicy.Next(_state, a, id, now);
                    _attack = null;
                    // The cancel itself follows (AttackTracker), with its own plan on the cancel line.
                    if (a.Reason.Length == 0)
                    {
                        LogStep(p, a.Kind, clip, runId, loop, null);
                    }
                    else
                    {
                        LogEnd(p, a.Reason, clip, runId, loop);
                    }

                    cancelNow = true;
                    break;
            }

            // A hit of a run that just aborted (option turned off) is still ours: no plain cancel.
            return MaxComboPolicy.Handles(kind, routes, wasRunning);
        }
        catch (Exception e)
        {
            if (!_hitReported)
            {
                _hitReported = true;
                ReviewLog.Log?.LogError($"{nameof(MaxComboRun)}.{nameof(OnHit)}: {e}");
            }

            // Do not leave a run (and its chain triggers) behind a failure.
            SafeAbort(p, "error");
            cancelNow = false;
            return false;
        }
    }

    /// <summary>Every frame (AttackTracker.Tick): abort on instance change, stagger, dodge, option off or timeout.</summary>
    internal static void Tick(CancelSettings s)
    {
        if (!_state.Running)
        {
            return;
        }

        try
        {
            Player? p = Player.m_localPlayer;
            if (p == null)
            {
                _state = MaxComboState.Idle;
                _attack = null;
                return;
            }

            Animator? a = p.m_animator;
            bool dodging = a != null
                           && (a.GetCurrentAnimatorStateInfo(0).tagHash == TagDodge
                               || (a.IsInTransition(0) && a.GetNextAnimatorStateInfo(0).tagHash == TagDodge));
            MaxComboAction d = MaxComboPolicy.Watch(_state, ReferenceEquals(p.m_currentAttack, _attack), s,
                Time.time - _state.StepTime, p.IsStaggering(), dodging);
            if (d.Kind == MaxComboActionKind.Abort)
            {
                Abort(p, d.Reason);
            }
        }
        catch (Exception e)
        {
            if (!_tickReported)
            {
                _tickReported = true;
                ReviewLog.Log?.LogError($"{nameof(MaxComboRun)}.{nameof(Tick)}: {e}");
            }

            SafeAbort(Player.m_localPlayer, "error");
        }
    }

    /// <summary>The current attack is the sword secondary of a weapon whose primary is the 3-level longsword combo.</summary>
    private static bool Eligible(Player p, Attack atk)
    {
        Attack? primary = p.GetCurrentWeapon()?.m_shared?.m_attack;
        return primary != null
               && MaxComboPolicy.Eligible(atk.m_attackAnimation, primary.m_attackAnimation, primary.m_attackChainLevels);
    }

    /// <summary>Abort from a catch block: never throws.</summary>
    private static void SafeAbort(Player? p, string reason)
    {
        try
        {
            if (p != null)
            {
                Abort(p, reason);
            }
        }
        catch (Exception)
        {
            // Already reported once by the caller.
        }
        finally
        {
            _state = MaxComboState.Idle;
            _attack = null;
        }
    }

    /// <summary>Ends the run: resets the chain triggers it may have left on the local Animator.</summary>
    private static void Abort(Player p, string reason)
    {
        int loop = _state.Loop;
        _state = MaxComboState.Idle;
        _attack = null;
        Animator? a = p.m_animator;
        if (a != null)
        {
            foreach (string t in MaxComboPolicy.AbortTriggers)
            {
                a.ResetTrigger(t);
            }
        }

        if (ReviewLog.On)
        {
            ReviewLog.Write("maxcombo_abort", Concat(MaxComboPolicy.AbortFields(reason),
                MaxComboPolicy.RunFields(loop, null, p.GetStamina())));
        }

        if (reason == "instance" && ReviewLog.On)
        {
            AttackClearLog.OnInstanceAbort(p);
        }
    }

    private static void LogStep(Player p, MaxComboActionKind k, string clip, int id, int loop, string? plan)
    {
        if (ReviewLog.On)
        {
            ReviewLog.Write("maxcombo", Concat(MaxComboPolicy.StepFields(MaxComboPolicy.StepNumber(k), clip, id),
                MaxComboPolicy.RunFields(loop, plan, p.GetStamina())));
        }
    }

    /// <summary>Loop mode ended with a cancel at this hit (released|cap).</summary>
    private static void LogEnd(Player p, string reason, string clip, int id, int loop)
    {
        if (ReviewLog.On)
        {
            ReviewLog.Write("maxcombo_end", Concat(MaxComboPolicy.EndFields(reason),
                new[] { ("clip", clip), ("attack", MaxComboPolicy.ShortId(id)) },
                MaxComboPolicy.RunFields(loop, null, p.GetStamina())));
        }
    }

    private static (string k, string v)[] Concat(params (string k, string v)[][] parts)
    {
        int n = 0;
        foreach ((string k, string v)[] part in parts)
        {
            n += part.Length;
        }

        var all = new (string k, string v)[n];
        int i = 0;
        foreach ((string k, string v)[] part in parts)
        {
            part.CopyTo(all, i);
            i += part.Length;
        }

        return all;
    }
}
