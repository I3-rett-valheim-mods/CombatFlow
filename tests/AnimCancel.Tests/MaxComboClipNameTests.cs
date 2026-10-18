using AnimCancel.Core;
using Xunit;

namespace AnimCancel.Tests
{
    /// <summary>
    /// In-game pass axes2 (2026-10-18): every finisher hit was absorbed (decision reason=maxcombo) and the run never
    /// started. The log writes clip names through DebugLine.Token, which turns spaces into underscores, so the
    /// logged `clip=DualAxes_Attack_4` can be a raw Unity clip name with spaces that never equals the profile's
    /// constant. The sword clips (Sword-Attack-R4, Attack2, Attack3) have no spaces, which is why the sword worked.
    /// </summary>
    public class MaxComboClipNameTests
    {
        static readonly MaxComboProfile Axes = MaxComboProfile.Axes;
        static readonly CancelSettings Loop = CancelSettings.Default with
        {
            Enabled = true, MaxCombo = true, AxesMaxCombo = true, MaxComboMode = MaxComboMode.Loop, MaxComboLoopCap = 0,
        };

        // The game's clip name, as logged "DualAxes_Attack_4" (spaces shown as underscores by the log).
        const string RawFinisher = "DualAxes Attack 4";
        const string RawSwing1 = "DualAxes Attack 3 2";

        [Fact]
        public void Finisher_hit_1_then_hit_2_same_instance_starts_the_run_with_the_raw_clip_name()
        {
            var counter = new SwingHitCounter();
            bool eligible = MaxComboPolicy.Eligible(Axes, "dualaxes", 3, 4, "dualaxes", 4, "dualaxes_secondary");
            Assert.True(MaxComboPolicy.Routes(Axes, AttackKind.AxesCombo, Loop, eligible));

            int h1 = counter.Register(0x1234, RawFinisher);
            MaxComboAction a1 = MaxComboPolicy.Decide(Axes, MaxComboState.Idle, RawFinisher, h1, false, Loop, 155.32f,
                eligible, staggering: false, held: true);
            Assert.Equal(MaxComboActionKind.None, a1.Kind);

            int h2 = counter.Register(0x1234, RawFinisher);
            MaxComboAction a2 = MaxComboPolicy.Decide(Axes, MaxComboState.Idle, RawFinisher, h2, false, Loop, 155.48f,
                eligible, staggering: false, held: true);
            Assert.Equal(MaxComboActionKind.TriggerSwing1, a2.Kind);
        }

        [Fact]
        public void Run_steps_match_raw_clip_names_too()
        {
            var s1 = new MaxComboState(1, 7, 1f);
            Assert.Equal(MaxComboActionKind.TriggerSwing2,
                MaxComboPolicy.Decide(Axes, s1, RawSwing1, 2, true, Loop, 0.3f, true, false, true).Kind);
            var s2 = new MaxComboState(2, 7, 1f);
            Assert.Equal(MaxComboActionKind.TriggerSwing1,
                MaxComboPolicy.Decide(Axes, s2, RawFinisher, 2, true, Loop, 0.3f, true, false, true).Kind);
        }

        [Fact]
        public void Clip_match_ignores_space_versus_underscore_only()
        {
            Assert.True(MaxComboProfile.SameClip("DualAxes Attack 4", "DualAxes_Attack_4"));
            Assert.True(MaxComboProfile.SameClip("DualAxes_Attack_4", "DualAxes_Attack_4"));
            Assert.True(MaxComboProfile.SameClip("Attack2", "Attack2"));
            Assert.False(MaxComboProfile.SameClip("DualAxes Attack 3_2", "DualAxes_Attack_4"));
            Assert.False(MaxComboProfile.SameClip("dualaxes attack 4", "DualAxes_Attack_4"));
            Assert.False(MaxComboProfile.SameClip(null, "Attack2"));
            Assert.False(MaxComboProfile.SameClip("", "Attack2"));
        }

        [Fact]
        public void Only_an_action_owns_the_hit_otherwise_the_plain_policy_runs()
        {
            Assert.True(MaxComboPolicy.Acts(MaxComboActionKind.TriggerSwing1));
            Assert.True(MaxComboPolicy.Acts(MaxComboActionKind.TriggerSwing2));
            Assert.True(MaxComboPolicy.Acts(MaxComboActionKind.CancelNow));
            Assert.True(MaxComboPolicy.Acts(MaxComboActionKind.Progress));
            Assert.False(MaxComboPolicy.Acts(MaxComboActionKind.None));
            Assert.False(MaxComboPolicy.Acts(MaxComboActionKind.Abort));
        }

        [Fact]
        public void Skip_reason_names_why_an_absorbed_hit_did_nothing()
        {
            var idle = MaxComboState.Idle;
            string Skip(MaxComboState st, string? clip, int hit, CancelSettings? s = null, bool eligible = true,
                bool staggering = false)
                => MaxComboPolicy.SkipReason(Axes, st, clip, hit, s ?? Loop, eligible, staggering);

            Assert.Equal("not_last_hit", Skip(idle, RawFinisher, 1));
            Assert.Equal("clip", Skip(idle, "DualAxes Attack 2 2", 1));
            Assert.Equal("not_eligible", Skip(idle, RawFinisher, 2, eligible: false));
            Assert.Equal("staggering", Skip(idle, RawFinisher, 2, staggering: true));
            Assert.Equal("off", Skip(idle, RawFinisher, 2, Loop with { AxesMaxCombo = false }));
            Assert.Equal("off", Skip(idle, RawFinisher, 2, Loop with { Enabled = false }));
            Assert.Equal("clip", Skip(new MaxComboState(1, 7, 1f), RawFinisher, 2));
            Assert.Equal("clip", Skip(new MaxComboState(2, 7, 1f), RawSwing1, 2));
            Assert.Equal("-", Skip(idle, RawFinisher, 2));
        }

        [Fact]
        public void Skip_fields_for_the_log()
        {
            Assert.Equal(
                new[] { ("reason", "clip"), ("clip", "DualAxes Attack 4"), ("clipSpaces", "2"), ("hit", "1"), ("step", "0") },
                MaxComboPolicy.SkipFields("clip", "DualAxes Attack 4", 1, 0));
            Assert.Equal(
                new[] { ("reason", "x"), ("clip", "-"), ("clipSpaces", "0"), ("hit", "2"), ("step", "1") },
                MaxComboPolicy.SkipFields("x", null, 2, 1));
        }
    }
}
