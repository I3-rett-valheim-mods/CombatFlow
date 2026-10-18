using System.Globalization;

namespace AnimCancel.Core
{
    public enum MaxComboActionKind { None, TriggerSwing1, TriggerSwing2, CancelNow, Abort }

    public readonly struct MaxComboAction
    {
        public MaxComboActionKind Kind { get; }

        /// <summary>Abort reason (disabled|instance|stagger|dodge|timeout), Loop end reason on a CancelNow
        /// (released|cap), empty otherwise.</summary>
        public string Reason { get; }

        private MaxComboAction(MaxComboActionKind kind, string reason)
        {
            Kind = kind;
            Reason = reason;
        }

        public static MaxComboAction None => new(MaxComboActionKind.None, "");
        public static MaxComboAction TriggerSwing1 => new(MaxComboActionKind.TriggerSwing1, "");
        public static MaxComboAction TriggerSwing2 => new(MaxComboActionKind.TriggerSwing2, "");
        public static MaxComboAction CancelNow => new(MaxComboActionKind.CancelNow, "");
        public static MaxComboAction Abort(string reason) => new(MaxComboActionKind.Abort, reason);

        /// <summary>Loop mode: end the run with a normal cancel at this hit (released|cap).</summary>
        public static MaxComboAction End(string reason) => new(MaxComboActionKind.CancelNow, reason);
    }

    /// <summary>
    /// A max combo run: Step 0 idle, 1 once swing_longsword1 was set, 2 once swing_longsword2 was set.
    /// InstanceId identifies the secondary Attack the run belongs to (log only); StepTime is the
    /// Time.time of the last step. Loop counts the swings Loop mode chained past the first pass
    /// (0 during the first pass and in Cycle mode).
    /// </summary>
    public sealed record MaxComboState(int Step, int InstanceId, float StepTime, int Loop = 0)
    {
        public static MaxComboState Idle { get; } = new(0, 0, 0f);

        public bool Running => Step > 0;
    }

    /// <summary>
    /// Max combo (auto), from lab run 2: while the sword secondary Attack is current, the Hit events
    /// of the swing_sword 1 and swing_sword 2 clips go to it (Humanoid.OnAttackTrigger hands every hit
    /// to m_currentAttack), so each hits at the secondary's multiplier. At the secondary's Hit the
    /// plugin sets swing_longsword1 on the local Animator, at swing_sword 1's Hit swing_longsword2,
    /// and at swing_sword 2's Hit it cancels as usual (Cycle). In Loop mode, while the secondary
    /// button is held, swing_sword 2's Hit sets swing_longsword1 again and so on, until the button
    /// is released or <see cref="CancelSettings.MaxComboLoopCap"/> loop swings were chained (0: no cap), then it cancels.
    /// </summary>
    public static class MaxComboPolicy
    {
        /// <summary>Longest wait for the next step's hit, in seconds.</summary>
        public const float Timeout = 1.0f;

        /// <summary>Loop mode: default of Max combo loop cap, the swings chained past the first pass before
        /// the run ends with a cancel.</summary>
        public const int DefaultLoopCap = 40;

        /// <summary>Range of Max combo loop cap; 0 means no cap.</summary>
        public const int MinLoopCap = 0;
        public const int MaxLoopCap = 1000;

        public const string SecondaryAnimation = "sword_secondary";
        public const string PrimaryAnimation = "swing_longsword";
        public const int MinPrimaryLevels = 3;

        // Clips of the sword_secondary, swing_sword 1 and swing_sword 2 states.
        public const string SecondaryClip = "Sword-Attack-R4";
        public const string Swing1Clip = "Attack2";
        public const string Swing2Clip = "Attack3";

        public const string Swing1Trigger = "swing_longsword1";
        public const string Swing2Trigger = "swing_longsword2";

        /// <summary>Reset before a step's trigger is set, so no stale chain trigger cuts in.</summary>
        public static readonly string[] SwingTriggers = { "swing_longsword0", Swing1Trigger, Swing2Trigger };

        /// <summary>Reset on abort.</summary>
        public static readonly string[] AbortTriggers = { Swing1Trigger, Swing2Trigger };

        /// <summary>The current attack is the sword secondary and the weapon's primary is the 3-level longsword combo.</summary>
        public static bool Eligible(string? attackAnimation, string? primaryAnimation, int primaryChainLevels)
            => attackAnimation == SecondaryAnimation
               && primaryAnimation == PrimaryAnimation
               && primaryChainLevels >= MinPrimaryLevels;

        /// <summary>The hit is handled by max combo instead of the plain cancel policy.</summary>
        public static bool Routes(AttackKind kind, CancelSettings s, bool eligible)
            => s.Enabled && s.MaxCombo && kind == AttackKind.SwordSecondary && eligible;

        /// <summary>
        /// OnHit owns this hit: it routes, or a run is still running and the hit is a sword secondary
        /// (a primary or any other attack never belongs to the run).
        /// </summary>
        public static bool Handles(AttackKind kind, bool routes, bool wasRunning)
            => routes || (wasRunning && kind == AttackKind.SwordSecondary);

        /// <summary>At a damage event. <paramref name="firedClip"/> is the clip that fired it (ClipPick).
        /// <paramref name="held"/>: the secondary button is held (Loop mode only).</summary>
        public static MaxComboAction Decide(MaxComboState state, string? firedClip, bool sameInstance,
            CancelSettings s, float sinceLastStep, bool eligible, bool staggering, bool held = false)
        {
            bool on = s.Enabled && s.MaxCombo;
            if (state.Running)
            {
                MaxComboAction abort = AbortReason(on, sameInstance, staggering, false, sinceLastStep);
                if (abort.Kind == MaxComboActionKind.Abort)
                {
                    return abort;
                }

                bool loop = s.MaxComboMode == MaxComboMode.Loop;
                if (state.Step == 1 && firedClip == Swing1Clip)
                {
                    // The first pass (Loop 0) runs as in Cycle; past it, a loop hit.
                    return loop && state.Loop > 0 ? LoopHit(state, s, held, MaxComboAction.TriggerSwing2)
                        : MaxComboAction.TriggerSwing2;
                }

                if (state.Step == 2 && firedClip == Swing2Clip)
                {
                    return loop ? LoopHit(state, s, held, MaxComboAction.TriggerSwing1) : MaxComboAction.CancelNow;
                }

                return MaxComboAction.None;
            }

            if (!on || !eligible || staggering || firedClip != SecondaryClip)
            {
                return MaxComboAction.None;
            }

            return MaxComboAction.TriggerSwing1;
        }

        /// <summary>Every frame while a run is going: abort when it can no longer complete.</summary>
        public static MaxComboAction Watch(MaxComboState state, bool sameInstance, CancelSettings s,
            float sinceLastStep, bool staggering, bool dodging)
            => state.Running
                ? AbortReason(s.Enabled && s.MaxCombo, sameInstance, staggering, dodging, sinceLastStep)
                : MaxComboAction.None;

        public static MaxComboState Next(MaxComboState state, MaxComboAction a, int instanceId, float now)
        {
            switch (a.Kind)
            {
                case MaxComboActionKind.TriggerSwing1:
                    // From idle a new run; from swing_sword 2 (Loop) the same run, one more loop swing.
                    return state.Running
                        ? new MaxComboState(1, state.InstanceId, now, state.Loop + 1)
                        : new MaxComboState(1, instanceId, now);
                case MaxComboActionKind.TriggerSwing2:
                    return new MaxComboState(2, state.InstanceId, now, state.Loop > 0 ? state.Loop + 1 : 0);
                case MaxComboActionKind.CancelNow:
                case MaxComboActionKind.Abort: return MaxComboState.Idle;
                default: return state;
            }
        }

        public static int StepNumber(MaxComboActionKind k) => k switch
        {
            MaxComboActionKind.TriggerSwing1 => 1,
            MaxComboActionKind.TriggerSwing2 => 2,
            MaxComboActionKind.CancelNow => 3,
            _ => 0,
        };

        public static string? TriggerFor(MaxComboActionKind k) => k switch
        {
            MaxComboActionKind.TriggerSwing1 => Swing1Trigger,
            MaxComboActionKind.TriggerSwing2 => Swing2Trigger,
            _ => null,
        };

        /// <summary>ev=maxcombo keys.</summary>
        public static (string k, string v)[] StepFields(int step, string? clip, int instanceId) => new[]
        {
            ("step", step.ToString(CultureInfo.InvariantCulture)),
            ("clip", string.IsNullOrEmpty(clip) ? "-" : clip!),
            ("attack", ShortId(instanceId)),
        };

        /// <summary>Keys added to every ev=maxcombo / maxcombo_end / maxcombo_abort line.</summary>
        public static (string k, string v)[] RunFields(int loop, string? plan, float stamina) => new[]
        {
            ("loop", loop.ToString(CultureInfo.InvariantCulture)),
            ("plan", string.IsNullOrEmpty(plan) ? "-" : plan!),
            ("stamina", stamina.ToString("0.0", CultureInfo.InvariantCulture)),
        };

        /// <summary>ev=maxcombo_end keys (Loop mode ended with a cancel).</summary>
        public static (string k, string v)[] EndFields(string reason) => new[] { ("reason", reason) };

        /// <summary>ev=maxcombo_abort keys.</summary>
        public static (string k, string v)[] AbortFields(string reason) => new[] { ("reason", reason) };

        /// <summary>ev=config keys: what labels a run (vanilla / cancel / maxcombo) in the log analysis.</summary>
        public static (string k, string v)[] ConfigFields(CancelSettings s) => new[]
        {
            ("enabled", DebugLine.Bool(s.Enabled)),
            ("maxCombo", DebugLine.Bool(s.MaxCombo)),
            ("maxComboMode", s.MaxComboMode.ToString()),
            ("swordCombo", DebugLine.Bool(s.SwordCombo)),
            ("swordSecondary", DebugLine.Bool(s.SwordSecondary)),
            ("maxComboLoopCap", s.MaxComboLoopCap.ToString(CultureInfo.InvariantCulture)),
        };

        /// <summary>Low 16 bits of an instance id, 4 hex digits.</summary>
        public static string ShortId(int id) => (id & 0xFFFF).ToString("x4", CultureInfo.InvariantCulture);

        /// <summary>Loop mode, at a loop hit: the next swing while held and under the cap (0: none), else a cancel.</summary>
        private static MaxComboAction LoopHit(MaxComboState state, CancelSettings s, bool held, MaxComboAction next)
        {
            if (!held) return MaxComboAction.End("released");
            if (s.MaxComboLoopCap > 0 && state.Loop >= s.MaxComboLoopCap) return MaxComboAction.End("cap");
            return next;
        }

        private static MaxComboAction AbortReason(bool on, bool sameInstance, bool staggering, bool dodging, float since)
        {
            if (!on) return MaxComboAction.Abort("disabled");
            if (!sameInstance) return MaxComboAction.Abort("instance");
            if (staggering) return MaxComboAction.Abort("stagger");
            if (dodging) return MaxComboAction.Abort("dodge");
            if (since > Timeout) return MaxComboAction.Abort("timeout");
            return MaxComboAction.None;
        }
    }
}
