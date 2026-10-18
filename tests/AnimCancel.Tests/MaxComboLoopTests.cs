using AnimCancel.Core;
using Xunit;

namespace AnimCancel.Tests
{
    /// <summary>Max combo mode Loop: swing_sword 1 / swing_sword 2 alternate while the secondary is held.</summary>
    public class MaxComboLoopTests
    {
        const string R4 = "Sword-Attack-R4";
        const string A2 = "Attack2";
        const string A3 = "Attack3";

        static readonly CancelSettings Loop = CancelSettings.Default with { MaxCombo = true, MaxComboMode = MaxComboMode.Loop, MaxComboLoopCap = 40 };
        static readonly CancelSettings Cycle = CancelSettings.Default with { MaxCombo = true, MaxComboMode = MaxComboMode.Cycle, MaxComboLoopCap = 40 };

        static MaxComboAction Hit(MaxComboState st, string? clip, bool held, CancelSettings? s = null, bool same = true,
            float since = 0.3f, bool staggering = false)
            => MaxComboPolicy.Decide(st, clip, same, s ?? Loop, since, eligible: true, staggering, held);

        static void Is(MaxComboAction a, MaxComboActionKind kind, string reason = "")
        {
            Assert.Equal(kind, a.Kind);
            Assert.Equal(reason, a.Reason);
        }

        [Fact]
        public void Mode_defaults_to_loop_without_cap()
        {
            Assert.True(CancelSettings.Default.MaxCombo);
            Assert.Equal(MaxComboMode.Loop, CancelSettings.Default.MaxComboMode);
            Assert.Equal(0, MaxComboPolicy.DefaultLoopCap);
            Assert.Equal(0, CancelSettings.Default.MaxComboLoopCap);
        }

        [Fact]
        public void Held_loop_alternates_swing1_and_swing2_on_the_same_instance()
        {
            MaxComboAction a = Hit(MaxComboState.Idle, R4, held: true, same: false, since: 99f);
            Is(a, MaxComboActionKind.TriggerSwing1);
            MaxComboState s = MaxComboPolicy.Next(MaxComboState.Idle, a, instanceId: 42, now: 1f);
            Assert.Equal(new MaxComboState(1, 42, 1f, 0), s);

            a = Hit(s, A2, held: true);
            Is(a, MaxComboActionKind.TriggerSwing2);
            s = MaxComboPolicy.Next(s, a, instanceId: 42, now: 1.4f);
            Assert.Equal(new MaxComboState(2, 42, 1.4f, 0), s);

            // Where Cycle cancels, Loop sets swing_longsword1 again: loop hit 1.
            a = Hit(s, A3, held: true);
            Is(a, MaxComboActionKind.TriggerSwing1);
            s = MaxComboPolicy.Next(s, a, instanceId: 42, now: 1.8f);
            Assert.Equal(new MaxComboState(1, 42, 1.8f, 1), s);

            a = Hit(s, A2, held: true);
            Is(a, MaxComboActionKind.TriggerSwing2);
            s = MaxComboPolicy.Next(s, a, instanceId: 42, now: 2.2f);
            Assert.Equal(new MaxComboState(2, 42, 2.2f, 2), s);

            a = Hit(s, A3, held: true);
            Is(a, MaxComboActionKind.TriggerSwing1);
            s = MaxComboPolicy.Next(s, a, instanceId: 42, now: 2.6f);
            Assert.Equal(new MaxComboState(1, 42, 2.6f, 3), s);
        }

        [Fact]
        public void Loop_trigger_keeps_the_run_instance_id()
        {
            var s = new MaxComboState(2, 42, 1f, 5);
            MaxComboState n = MaxComboPolicy.Next(s, MaxComboAction.TriggerSwing1, instanceId: 99, now: 2f);
            Assert.Equal(new MaxComboState(1, 42, 2f, 6), n);
        }

        [Fact]
        public void First_pass_runs_as_in_cycle_even_when_released()
        {
            Is(Hit(MaxComboState.Idle, R4, held: false, same: false), MaxComboActionKind.TriggerSwing1);
            Is(Hit(new MaxComboState(1, 7, 0f), A2, held: false), MaxComboActionKind.TriggerSwing2);
        }

        [Fact]
        public void Released_cancels_at_the_current_swing_hit()
        {
            Is(Hit(new MaxComboState(2, 7, 0f), A3, held: false), MaxComboActionKind.CancelNow, "released");
            Is(Hit(new MaxComboState(1, 7, 0f, 3), A2, held: false), MaxComboActionKind.CancelNow, "released");
            Is(Hit(new MaxComboState(2, 7, 0f, 4), A3, held: false), MaxComboActionKind.CancelNow, "released");
        }

        [Fact]
        public void Cap_cancels_after_forty_loop_hits()
        {
            Is(Hit(new MaxComboState(1, 7, 0f, 39), A2, held: true), MaxComboActionKind.TriggerSwing2);
            Is(Hit(new MaxComboState(2, 7, 0f, 40), A3, held: true), MaxComboActionKind.CancelNow, "cap");
            Is(Hit(new MaxComboState(1, 7, 0f, 40), A2, held: true), MaxComboActionKind.CancelNow, "cap");
            Is(Hit(new MaxComboState(1, 7, 0f, 41), A2, held: true), MaxComboActionKind.CancelNow, "cap");
        }

        [Fact]
        public void Cap_follows_the_setting()
        {
            CancelSettings five = Loop with { MaxComboLoopCap = 5 };
            Is(Hit(new MaxComboState(1, 7, 0f, 4), A2, held: true, five), MaxComboActionKind.TriggerSwing2);
            Is(Hit(new MaxComboState(2, 7, 0f, 5), A3, held: true, five), MaxComboActionKind.CancelNow, "cap");
            Is(Hit(new MaxComboState(2, 7, 0f, 40), A3, held: true, Loop with { MaxComboLoopCap = 1000 }),
                MaxComboActionKind.TriggerSwing1);
        }

        [Fact]
        public void Cap_zero_means_no_cap()
        {
            CancelSettings unlimited = Loop with { MaxComboLoopCap = 0 };
            Is(Hit(new MaxComboState(2, 7, 0f, 40), A3, held: true, unlimited), MaxComboActionKind.TriggerSwing1);
            Is(Hit(new MaxComboState(1, 7, 0f, 5000), A2, held: true, unlimited), MaxComboActionKind.TriggerSwing2);
            Is(Hit(new MaxComboState(2, 7, 0f, 5000), A3, held: false, unlimited), MaxComboActionKind.CancelNow, "released");
            Is(Hit(new MaxComboState(2, 7, 0f, 5000), A3, true, unlimited, since: 1.01f), MaxComboActionKind.Abort, "timeout");
        }

        [Fact]
        public void Cap_range_is_zero_to_a_thousand()
        {
            Assert.Equal(0, MaxComboPolicy.MinLoopCap);
            Assert.Equal(1000, MaxComboPolicy.MaxLoopCap);
        }

        [Fact]
        public void End_returns_to_idle()
        {
            var s = new MaxComboState(2, 7, 0f, 12);
            Assert.Same(MaxComboState.Idle, MaxComboPolicy.Next(s, MaxComboAction.End("released"), 7, 1f));
            Assert.Same(MaxComboState.Idle, MaxComboPolicy.Next(s, MaxComboAction.End("cap"), 7, 1f));
            Is(MaxComboAction.End("cap"), MaxComboActionKind.CancelNow, "cap");
        }

        [Fact]
        public void Loop_waits_on_an_unexpected_clip()
        {
            Is(Hit(new MaxComboState(1, 7, 0f, 2), A3, held: true), MaxComboActionKind.None);
            Is(Hit(new MaxComboState(2, 7, 0f, 2), A2, held: true), MaxComboActionKind.None);
            Is(Hit(new MaxComboState(2, 7, 0f, 2), R4, held: false), MaxComboActionKind.None);
        }

        [Fact]
        public void Loop_aborts_for_every_existing_reason()
        {
            var s = new MaxComboState(1, 7, 0f, 6);
            Is(Hit(s, A2, true, Loop with { Enabled = false }), MaxComboActionKind.Abort, "disabled");
            Is(Hit(s, A2, true, Loop with { MaxCombo = false }), MaxComboActionKind.Abort, "disabled");
            Is(Hit(s, A2, true, same: false), MaxComboActionKind.Abort, "instance");
            Is(Hit(s, A2, true, staggering: true), MaxComboActionKind.Abort, "stagger");
            Is(Hit(s, A2, true, since: 1.01f), MaxComboActionKind.Abort, "timeout");
            // Abort wins over a release.
            Is(Hit(s, A2, false, since: 1.01f), MaxComboActionKind.Abort, "timeout");

            MaxComboAction W(bool dodge = false, float since = 0.2f) => MaxComboPolicy.Watch(s, true, Loop, since, false, dodge);
            Is(W(), MaxComboActionKind.None);
            Is(W(dodge: true), MaxComboActionKind.Abort, "dodge");
            Is(W(since: 1.5f), MaxComboActionKind.Abort, "timeout");
        }

        [Fact]
        public void Cycle_ignores_the_button()
        {
            Is(Hit(new MaxComboState(2, 7, 0f), A3, held: true, Cycle), MaxComboActionKind.CancelNow);
            Is(Hit(new MaxComboState(2, 7, 0f), A3, held: false, Cycle), MaxComboActionKind.CancelNow);
            Is(Hit(new MaxComboState(1, 7, 0f), A2, held: false, Cycle), MaxComboActionKind.TriggerSwing2);
        }

        [Fact]
        public void Switching_to_cycle_mid_loop_finishes_like_cycle()
        {
            Is(Hit(new MaxComboState(2, 7, 0f, 5), A3, held: true, Cycle), MaxComboActionKind.CancelNow);
            Is(Hit(new MaxComboState(1, 7, 0f, 5), A2, held: true, Cycle), MaxComboActionKind.TriggerSwing2);
        }

        [Fact]
        public void Run_fields_for_the_log()
        {
            Assert.Equal(new[] { ("loop", "3"), ("plan", "restore"), ("stamina", "87.5") },
                MaxComboPolicy.RunFields(3, "restore", 87.46f));
            Assert.Equal(new[] { ("loop", "0"), ("plan", "-"), ("stamina", "0.0") },
                MaxComboPolicy.RunFields(0, null, 0f));
            Assert.Equal(new[] { ("reason", "released") }, MaxComboPolicy.EndFields("released"));
        }

        [Fact]
        public void Config_fields_carry_the_mode()
        {
            Assert.Contains(("maxComboMode", "Cycle"), MaxComboPolicy.ConfigFields(Cycle));
            Assert.Contains(("maxComboMode", "Loop"), MaxComboPolicy.ConfigFields(Loop));
            Assert.Contains(("maxComboMode", "Loop"), MaxComboPolicy.ConfigFields(CancelSettings.Default));
        }

        [Fact]
        public void A_running_run_only_owns_secondary_hits()
        {
            Assert.True(MaxComboPolicy.Handles(AttackKind.SwordSecondary, routes: false, wasRunning: true));
            Assert.False(MaxComboPolicy.Handles(AttackKind.SwordCombo, routes: false, wasRunning: true));
            Assert.False(MaxComboPolicy.Handles(AttackKind.SwordSecondary, routes: false, wasRunning: false));
            Assert.True(MaxComboPolicy.Handles(AttackKind.SwordSecondary, routes: true, wasRunning: false));
        }
    }
}
