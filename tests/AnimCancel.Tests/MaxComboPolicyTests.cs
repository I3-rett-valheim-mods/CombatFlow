using AnimCancel.Core;
using Xunit;

namespace AnimCancel.Tests
{
    public class MaxComboPolicyTests
    {
        // Real clip names (docs/DESIGN.md clip data): sword_secondary plays Sword-Attack-R4,
        // swing_sword 1 plays Attack2, swing_sword 2 plays Attack3.
        const string R4 = "Sword-Attack-R4";
        const string A1 = "Attack1";
        const string A2 = "Attack2";
        const string A3 = "Attack3";

        static readonly CancelSettings Base = CancelSettings.Default with { MaxCombo = false, MaxComboMode = MaxComboMode.Cycle, MaxComboLoopCap = 40 };
        static readonly CancelSettings On = Base with { MaxCombo = true };
        static readonly MaxComboState Idle = MaxComboState.Idle;

        static MaxComboAction Hit(MaxComboState st, string? clip, CancelSettings? s = null, bool same = true,
            float since = 0.3f, bool eligible = true, bool staggering = false)
            => MaxComboPolicy.Decide(st, clip, same, s ?? On, since, eligible, staggering);

        static void Is(MaxComboAction a, MaxComboActionKind kind, string reason = "")
        {
            Assert.Equal(kind, a.Kind);
            Assert.Equal(reason, a.Reason);
        }

        [Fact]
        public void Happy_path_triggers_swing1_then_swing2_then_cancels()
        {
            MaxComboAction a1 = Hit(Idle, R4, same: false, since: 99f);
            Is(a1, MaxComboActionKind.TriggerSwing1);
            MaxComboState s1 = MaxComboPolicy.Next(Idle, a1, instanceId: 42, now: 10f);
            Assert.Equal(1, s1.Step);
            Assert.Equal(42, s1.InstanceId);
            Assert.Equal(10f, s1.StepTime);

            MaxComboAction a2 = Hit(s1, A2);
            Is(a2, MaxComboActionKind.TriggerSwing2);
            MaxComboState s2 = MaxComboPolicy.Next(s1, a2, instanceId: 42, now: 10.4f);
            Assert.Equal(2, s2.Step);
            Assert.Equal(42, s2.InstanceId);
            Assert.Equal(10.4f, s2.StepTime);

            MaxComboAction a3 = Hit(s2, A3);
            Is(a3, MaxComboActionKind.CancelNow);
            Assert.Same(MaxComboState.Idle, MaxComboPolicy.Next(s2, a3, 42, 10.7f));
        }

        [Fact]
        public void Step_numbers_for_the_log()
        {
            Assert.Equal(1, MaxComboPolicy.StepNumber(MaxComboActionKind.TriggerSwing1));
            Assert.Equal(2, MaxComboPolicy.StepNumber(MaxComboActionKind.TriggerSwing2));
            Assert.Equal(3, MaxComboPolicy.StepNumber(MaxComboActionKind.CancelNow));
            Assert.Equal(0, MaxComboPolicy.StepNumber(MaxComboActionKind.None));
        }

        [Fact]
        public void Triggers_are_the_sword_chain_triggers()
        {
            Assert.Equal("swing_longsword1", MaxComboPolicy.TriggerFor(MaxComboActionKind.TriggerSwing1));
            Assert.Equal("swing_longsword2", MaxComboPolicy.TriggerFor(MaxComboActionKind.TriggerSwing2));
            Assert.Null(MaxComboPolicy.TriggerFor(MaxComboActionKind.CancelNow));
            Assert.Equal(new[] { "swing_longsword0", "swing_longsword1", "swing_longsword2" }, MaxComboPolicy.SwingTriggers);
            Assert.Equal(new[] { "swing_longsword1", "swing_longsword2" }, MaxComboPolicy.AbortTriggers);
        }

        [Fact]
        public void Option_off_does_nothing()
        {
            Is(Hit(Idle, R4, Base), MaxComboActionKind.None);
            Is(Hit(Idle, R4, On with { Enabled = false }), MaxComboActionKind.None);
        }

        [Fact]
        public void Wrong_weapon_does_nothing()
            => Is(Hit(Idle, R4, eligible: false), MaxComboActionKind.None);

        [Fact]
        public void Idle_ignores_other_clips_and_stagger()
        {
            Is(Hit(Idle, A2), MaxComboActionKind.None);
            Is(Hit(Idle, A3), MaxComboActionKind.None);
            Is(Hit(Idle, null), MaxComboActionKind.None);
            Is(Hit(Idle, R4, staggering: true), MaxComboActionKind.None);
        }

        [Fact]
        public void Running_waits_on_an_unexpected_clip()
        {
            var s1 = new MaxComboState(1, 7, 0f);
            var s2 = new MaxComboState(2, 7, 0f);
            Is(Hit(s1, A3), MaxComboActionKind.None);
            Is(Hit(s1, R4), MaxComboActionKind.None);
            Is(Hit(s1, A1), MaxComboActionKind.None);
            Is(Hit(s2, A2), MaxComboActionKind.None);
        }

        [Fact]
        public void Running_aborts_at_a_hit()
        {
            var s1 = new MaxComboState(1, 7, 0f);
            Is(Hit(s1, A2, Base), MaxComboActionKind.Abort, "disabled");
            Is(Hit(s1, A2, On with { Enabled = false }), MaxComboActionKind.Abort, "disabled");
            Is(Hit(s1, A2, same: false), MaxComboActionKind.Abort, "instance");
            Is(Hit(s1, A2, staggering: true), MaxComboActionKind.Abort, "stagger");
            Is(Hit(s1, A2, since: 1.01f), MaxComboActionKind.Abort, "timeout");
            Is(Hit(s1, A2, since: 1.0f), MaxComboActionKind.TriggerSwing2);
        }

        [Fact]
        public void Watch_aborts_every_reason()
        {
            var s = new MaxComboState(2, 7, 0f);
            MaxComboAction W(CancelSettings? set = null, bool same = true, float since = 0.2f, bool stag = false, bool dodge = false)
                => MaxComboPolicy.Watch(s, same, set ?? On, since, stag, dodge);

            Is(W(), MaxComboActionKind.None);
            Is(W(Base), MaxComboActionKind.Abort, "disabled");
            Is(W(On with { Enabled = false }), MaxComboActionKind.Abort, "disabled");
            Is(W(same: false), MaxComboActionKind.Abort, "instance");
            Is(W(stag: true), MaxComboActionKind.Abort, "stagger");
            Is(W(dodge: true), MaxComboActionKind.Abort, "dodge");
            Is(W(since: 1.5f), MaxComboActionKind.Abort, "timeout");
        }

        [Fact]
        public void Watch_is_quiet_when_idle()
            => Is(MaxComboPolicy.Watch(Idle, false, Base, 99f, true, true), MaxComboActionKind.None);

        [Fact]
        public void Abort_and_none_transitions()
        {
            var s = new MaxComboState(2, 7, 3f);
            Assert.Same(MaxComboState.Idle, MaxComboPolicy.Next(s, MaxComboAction.Abort("timeout"), 7, 5f));
            Assert.Same(s, MaxComboPolicy.Next(s, MaxComboAction.None, 7, 5f));
        }

        [Fact]
        public void Eligible_only_for_sword_secondary_with_a_three_level_longsword_primary()
        {
            Assert.True(MaxComboPolicy.Eligible("sword_secondary", "swing_longsword", 3));
            Assert.False(MaxComboPolicy.Eligible("swing_longsword", "swing_longsword", 3));
            Assert.False(MaxComboPolicy.Eligible("sword_secondary", "swing_axe", 3));
            Assert.False(MaxComboPolicy.Eligible("sword_secondary", "swing_longsword", 2));
            Assert.False(MaxComboPolicy.Eligible(null, null, 0));
        }

        [Fact]
        public void Routes_replaces_only_the_sword_secondary_with_the_option_on()
        {
            Assert.True(MaxComboPolicy.Routes(AttackKind.SwordSecondary, On, eligible: true));
            Assert.False(MaxComboPolicy.Routes(AttackKind.SwordSecondary, On, eligible: false));
            Assert.False(MaxComboPolicy.Routes(AttackKind.SwordSecondary, On with { Enabled = false }, true));
            Assert.False(MaxComboPolicy.Routes(AttackKind.SwordCombo, On, true));
            Assert.False(MaxComboPolicy.Routes(AttackKind.AxesSecondary, On, true));
        }

        [Fact]
        public void Max_combo_off_keeps_the_plain_sword_secondary_cancel()
        {
            CancelSettings off = Base;
            Assert.False(off.MaxCombo);
            Assert.False(MaxComboPolicy.Routes(AttackKind.SwordSecondary, off, eligible: true));

            // sword_secondary (Sword-Attack-R4): single Hit @0.244, no Chain.
            var timing = new ClipTiming(new[] { 0.244f }, null);
            CancelDecision d = CancelPolicy.ShouldCancel(AttackKind.SwordSecondary, timing, 1, 1f, off);
            Assert.True(d.Cancel);
            Assert.Equal("ok", d.Reason);
        }

        [Fact]
        public void Log_fields()
        {
            Assert.Equal(new[] { ("step", "1"), ("clip", R4), ("attack", "002a") },
                MaxComboPolicy.StepFields(1, R4, 42));
            Assert.Equal(new[] { ("step", "3"), ("clip", "-"), ("attack", "beef") },
                MaxComboPolicy.StepFields(3, null, unchecked((int)0xdeadbeef)));
            Assert.Equal(new[] { ("reason", "dodge") }, MaxComboPolicy.AbortFields("dodge"));
        }

        [Fact]
        public void Config_fields_label_the_mode()
        {
            Assert.Equal(
                new[] { ("enabled", "true"), ("maxCombo", "false"), ("maxComboMode", "Cycle"), ("maxComboLoopCap", "40"), ("axesMaxCombo", "true") },
                MaxComboPolicy.ConfigFields(Base));
            Assert.Equal(
                new[] { ("enabled", "false"), ("maxCombo", "true"), ("maxComboMode", "Cycle"), ("maxComboLoopCap", "40"), ("axesMaxCombo", "true") },
                MaxComboPolicy.ConfigFields(On with { Enabled = false }));
        }

        [Fact]
        public void Config_line_formats_as_ac_and_cl()
        {
            var kv = MaxComboPolicy.ConfigFields(On);
            Assert.Equal("AC t=1.000 f=2 ev=config enabled=true maxCombo=true maxComboMode=Cycle maxComboLoopCap=40 axesMaxCombo=true",
                DebugLine.Format(1, 2, "config", kv));
            Assert.Equal("CL t=1.000 f=2 ff=3 ev=config enabled=true maxCombo=true maxComboMode=Cycle maxComboLoopCap=40 axesMaxCombo=true",
                LabLine.Format(1, 2, 3, "config", kv));
        }
    }
}
