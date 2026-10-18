using CombatFlow.Core;
using Xunit;

namespace CombatFlow.Tests
{
    /// <summary>
    /// Max combo with the berserker axes: the finisher (dualaxes level 3 of 4, ×2) stays current while
    /// dualaxes2 / dualaxes3 are set on the local Animator. Real clip names (pass1 log): DualAxes 2 plays
    /// DualAxes_Attack_3_2 (hits 0.335, 0.553), DualAxes 3 plays DualAxes_Attack_4 (hits 0.367, 0.540).
    /// </summary>
    public class MaxComboAxesTests
    {
        const string Fin = "DualAxes_Attack_4";
        const string Ax2 = "DualAxes_Attack_3_2";
        const string Ax1 = "DualAxes_Attack_2_2";

        static readonly MaxComboProfile Axes = MaxComboProfile.Axes;
        static readonly CancelSettings Loop = CancelSettings.Default with
        {
            MaxCombo = false, AxesMaxCombo = true, MaxComboMode = MaxComboMode.Loop, MaxComboLoopCap = 0,
        };
        static readonly CancelSettings Cycle = Loop with { MaxComboMode = MaxComboMode.Cycle };
        static readonly MaxComboState Idle = MaxComboState.Idle;

        static MaxComboAction Hit(MaxComboState st, string? clip, int swingHit, CancelSettings? s = null,
            bool same = true, float since = 0.3f, bool eligible = true, bool staggering = false, bool held = true)
            => MaxComboPolicy.Decide(Axes, st, clip, swingHit, same, s ?? Loop, since, eligible, staggering, held);

        static void Is(MaxComboAction a, MaxComboActionKind kind, string reason = "")
        {
            Assert.Equal(kind, a.Kind);
            Assert.Equal(reason, a.Reason);
        }

        [Fact]
        public void Profile_names_the_real_clips_triggers_and_hit_counts()
        {
            Assert.Equal("axes", Axes.Weapon);
            Assert.Equal(AttackKind.AxesCombo, Axes.Kind);
            Assert.Equal(Fin, Axes.StartClip);
            Assert.Equal(Ax2, Axes.Swing1Clip);
            Assert.Equal(Fin, Axes.Swing2Clip);
            Assert.Equal(2, Axes.StartHits);
            Assert.Equal(2, Axes.Swing1Hits);
            Assert.Equal(2, Axes.Swing2Hits);
            Assert.Equal("dualaxes2", Axes.Swing1Trigger);
            Assert.Equal("dualaxes3", Axes.Swing2Trigger);
            Assert.True(Axes.HoldPrimary);
            Assert.Equal(new[] { "dualaxes0", "dualaxes1", "dualaxes2", "dualaxes3" }, Axes.SwingTriggers);
            Assert.Equal(new[] { "dualaxes2", "dualaxes3" }, Axes.AbortTriggers);
        }

        [Fact]
        public void Sword_profile_matches_the_sword_constants()
        {
            MaxComboProfile w = MaxComboProfile.Sword;
            Assert.Equal("sword", w.Weapon);
            Assert.Equal(AttackKind.SwordSecondary, w.Kind);
            Assert.Equal(MaxComboPolicy.SecondaryClip, w.StartClip);
            Assert.Equal(MaxComboPolicy.Swing1Clip, w.Swing1Clip);
            Assert.Equal(MaxComboPolicy.Swing2Clip, w.Swing2Clip);
            Assert.Equal(1, w.StartHits);
            Assert.Equal(1, w.Swing1Hits);
            Assert.Equal(1, w.Swing2Hits);
            Assert.Equal(MaxComboPolicy.Swing1Trigger, w.Swing1Trigger);
            Assert.Equal(MaxComboPolicy.Swing2Trigger, w.Swing2Trigger);
            Assert.False(w.HoldPrimary);
            Assert.Equal(MaxComboPolicy.SwingTriggers, w.SwingTriggers);
            Assert.Equal(MaxComboPolicy.AbortTriggers, w.AbortTriggers);
        }

        [Fact]
        public void Profile_for_kind()
        {
            Assert.Same(MaxComboProfile.Sword, MaxComboProfile.For(AttackKind.SwordSecondary));
            Assert.Same(MaxComboProfile.Axes, MaxComboProfile.For(AttackKind.AxesCombo));
            Assert.Null(MaxComboProfile.For(AttackKind.SwordCombo));
            Assert.Null(MaxComboProfile.For(AttackKind.AxesSecondary));
            Assert.Null(MaxComboProfile.For(AttackKind.None));
        }

        [Fact]
        public void Each_weapon_has_its_own_switch()
        {
            CancelSettings swordOnly = Loop with { MaxCombo = true, AxesMaxCombo = false };
            Assert.True(MaxComboProfile.Sword.IsOn(swordOnly));
            Assert.False(MaxComboProfile.Axes.IsOn(swordOnly));
            Assert.True(MaxComboProfile.Axes.IsOn(Loop));
            Assert.False(MaxComboProfile.Sword.IsOn(Loop));
        }

        [Fact]
        public void Axes_option_defaults_on()
        {
            Assert.True(CancelSettings.Default.AxesMaxCombo);
        }

        [Fact]
        public void Eligible_only_for_the_finisher_of_the_four_level_combo()
        {
            Assert.True(MaxComboPolicy.Eligible(Axes, "dualaxes", 3, 4, "dualaxes", 4, "dualaxes_secondary"));
            Assert.False(MaxComboPolicy.Eligible(Axes, "dualaxes", 2, 4, "dualaxes", 4, "dualaxes_secondary"));
            Assert.False(MaxComboPolicy.Eligible(Axes, "dualaxes", 0, 4, "dualaxes", 4, "dualaxes_secondary"));
            Assert.False(MaxComboPolicy.Eligible(Axes, "dualaxes", 2, 3, "dualaxes", 3, "dualaxes_secondary"));
            Assert.False(MaxComboPolicy.Eligible(Axes, "dualaxes_secondary", 0, 1, "dualaxes", 4, "dualaxes_secondary"));
            Assert.False(MaxComboPolicy.Eligible(Axes, "dualaxes", 3, 4, "dualaxes", 4, "axe_secondary"));
            Assert.False(MaxComboPolicy.Eligible(Axes, "dualaxes", 3, 4, "swing_axe", 4, "dualaxes_secondary"));
            Assert.False(MaxComboPolicy.Eligible(Axes, null, 0, 0, null, 0, null));
        }

        [Fact]
        public void Sword_profile_eligibility_is_the_sword_rule()
        {
            MaxComboProfile w = MaxComboProfile.Sword;
            Assert.True(MaxComboPolicy.Eligible(w, "sword_secondary", 0, 1, "swing_longsword", 3, "sword_secondary"));
            Assert.False(MaxComboPolicy.Eligible(w, "sword_secondary", 0, 1, "swing_longsword", 2, "sword_secondary"));
            Assert.False(MaxComboPolicy.Eligible(w, "swing_longsword", 2, 3, "swing_longsword", 3, "sword_secondary"));
        }

        [Fact]
        public void Routes_only_the_axes_combo_with_the_axes_option()
        {
            Assert.True(MaxComboPolicy.Routes(Axes, AttackKind.AxesCombo, Loop, eligible: true));
            Assert.False(MaxComboPolicy.Routes(Axes, AttackKind.AxesCombo, Loop, eligible: false));
            Assert.False(MaxComboPolicy.Routes(Axes, AttackKind.AxesCombo, Loop with { AxesMaxCombo = false }, true));
            Assert.False(MaxComboPolicy.Routes(Axes, AttackKind.AxesCombo, Loop with { Enabled = false }, true));
            Assert.False(MaxComboPolicy.Routes(Axes, AttackKind.AxesSecondary, Loop, true));
            Assert.False(MaxComboPolicy.Routes(Axes, AttackKind.SwordSecondary, Loop, true));
        }

        [Fact]
        public void Finisher_first_hit_waits_second_hit_starts_the_run()
        {
            Is(Hit(Idle, Fin, swingHit: 1, same: false, since: 99f), MaxComboActionKind.None);
            Is(Hit(Idle, Fin, swingHit: 2, same: false, since: 99f), MaxComboActionKind.TriggerSwing1);
        }

        [Fact]
        public void Idle_ignores_other_clips_option_off_and_ineligible()
        {
            Is(Hit(Idle, Ax2, 2, same: false), MaxComboActionKind.None);
            Is(Hit(Idle, Ax1, 1, same: false), MaxComboActionKind.None);
            Is(Hit(Idle, Fin, 2, Loop with { AxesMaxCombo = false }, same: false), MaxComboActionKind.None);
            Is(Hit(Idle, Fin, 2, Loop with { MaxCombo = true, AxesMaxCombo = false }, same: false), MaxComboActionKind.None);
            Is(Hit(Idle, Fin, 2, same: false, eligible: false), MaxComboActionKind.None);
            Is(Hit(Idle, Fin, 2, same: false, staggering: true), MaxComboActionKind.None);
        }

        [Fact]
        public void Cycle_finisher_then_dualaxes2_then_dualaxes3_then_cancel_at_last_hits_only()
        {
            MaxComboAction a = Hit(Idle, Fin, 2, Cycle, same: false, since: 99f);
            Is(a, MaxComboActionKind.TriggerSwing1);
            MaxComboState s = MaxComboPolicy.Next(Idle, a, instanceId: 7, now: 1f);
            Assert.Equal(new MaxComboState(1, 7, 1f, 0), s);

            // DualAxes 2: first hit only refreshes the step time.
            a = Hit(s, Ax2, 1, Cycle);
            Is(a, MaxComboActionKind.Progress);
            s = MaxComboPolicy.Next(s, a, 7, 1.5f);
            Assert.Equal(new MaxComboState(1, 7, 1.5f, 0), s);
            a = Hit(s, Ax2, 2, Cycle);
            Is(a, MaxComboActionKind.TriggerSwing2);
            s = MaxComboPolicy.Next(s, a, 7, 1.8f);
            Assert.Equal(new MaxComboState(2, 7, 1.8f, 0), s);

            // DualAxes 3 (the finisher clip again).
            a = Hit(s, Fin, 1, Cycle);
            Is(a, MaxComboActionKind.Progress);
            s = MaxComboPolicy.Next(s, a, 7, 2.2f);
            a = Hit(s, Fin, 2, Cycle);
            Is(a, MaxComboActionKind.CancelNow);
            Assert.Same(Idle, MaxComboPolicy.Next(s, a, 7, 2.4f));
        }

        [Fact]
        public void Loop_alternates_dualaxes2_and_dualaxes3_while_primary_held()
        {
            var s = new MaxComboState(2, 7, 1f, 0);
            MaxComboAction a = Hit(s, Fin, 2, held: true);
            Is(a, MaxComboActionKind.TriggerSwing1);
            s = MaxComboPolicy.Next(s, a, 7, 1.5f);
            Assert.Equal(new MaxComboState(1, 7, 1.5f, 1), s);

            Is(Hit(s, Ax2, 1, held: true), MaxComboActionKind.Progress);
            a = Hit(s, Ax2, 2, held: true);
            Is(a, MaxComboActionKind.TriggerSwing2);
            s = MaxComboPolicy.Next(s, a, 7, 2f);
            Assert.Equal(new MaxComboState(2, 7, 2f, 2), s);

            a = Hit(s, Fin, 2, held: true);
            Is(a, MaxComboActionKind.TriggerSwing1);
            Assert.Equal(new MaxComboState(1, 7, 2.5f, 3), MaxComboPolicy.Next(s, a, 7, 2.5f));
        }

        [Fact]
        public void Loop_release_and_cap_end_at_the_last_hit_of_the_swing()
        {
            var loopSwing2 = new MaxComboState(2, 7, 1f, 2);
            Is(Hit(loopSwing2, Fin, 1, held: false), MaxComboActionKind.Progress);
            Is(Hit(loopSwing2, Fin, 2, held: false), MaxComboActionKind.CancelNow, "released");
            var loopSwing1 = new MaxComboState(1, 7, 1f, 3);
            Is(Hit(loopSwing1, Ax2, 2, held: false), MaxComboActionKind.CancelNow, "released");
            Is(Hit(loopSwing1, Ax2, 2, Loop with { MaxComboLoopCap = 3 }, held: true), MaxComboActionKind.CancelNow, "cap");
            // First pass runs as in Cycle even when released.
            Is(Hit(new MaxComboState(1, 7, 1f, 0), Ax2, 2, held: false), MaxComboActionKind.TriggerSwing2);
        }

        [Fact]
        public void Wrong_clip_for_the_step_is_ignored()
        {
            Is(Hit(new MaxComboState(1, 7, 1f), Fin, 2), MaxComboActionKind.None);
            Is(Hit(new MaxComboState(2, 7, 1f), Ax2, 2), MaxComboActionKind.None);
        }

        [Fact]
        public void Aborts_as_for_the_sword()
        {
            var s = new MaxComboState(1, 7, 1f);
            Is(Hit(s, Ax2, 1, same: false), MaxComboActionKind.Abort, "instance");
            Is(Hit(s, Ax2, 1, staggering: true), MaxComboActionKind.Abort, "stagger");
            Is(Hit(s, Ax2, 1, since: 1.2f), MaxComboActionKind.Abort, "timeout");
            Is(Hit(s, Ax2, 2, Loop with { AxesMaxCombo = false }), MaxComboActionKind.Abort, "disabled");
            Is(MaxComboPolicy.Watch(Axes, s, true, Loop, 0.2f, false, dodging: true), MaxComboActionKind.Abort, "dodge");
            Is(MaxComboPolicy.Watch(Axes, s, true, Loop with { AxesMaxCombo = false }, 0.2f, false, false),
                MaxComboActionKind.Abort, "disabled");
            Is(MaxComboPolicy.Watch(Axes, s, true, Loop, 0.2f, false, false), MaxComboActionKind.None);
            // The sword switch does not keep an axes run alive.
            Is(MaxComboPolicy.Watch(Axes, s, true, Loop with { MaxCombo = true, AxesMaxCombo = false }, 0.2f, false, false),
                MaxComboActionKind.Abort, "disabled");
        }

        [Fact]
        public void Progress_keeps_step_instance_and_loop()
        {
            var s = new MaxComboState(2, 7, 1f, 4);
            Assert.Equal(new MaxComboState(2, 7, 1.3f, 4), MaxComboPolicy.Next(s, MaxComboAction.Progress, 99, 1.3f));
            Assert.Equal(0, MaxComboPolicy.StepNumber(MaxComboActionKind.Progress));
            Assert.Null(MaxComboPolicy.TriggerFor(MaxComboActionKind.Progress));
        }

        [Fact]
        public void Profile_trigger_for_each_step()
        {
            Assert.Equal("dualaxes2", Axes.TriggerFor(MaxComboActionKind.TriggerSwing1));
            Assert.Equal("dualaxes3", Axes.TriggerFor(MaxComboActionKind.TriggerSwing2));
            Assert.Null(Axes.TriggerFor(MaxComboActionKind.CancelNow));
            Assert.Equal("swing_longsword1", MaxComboProfile.Sword.TriggerFor(MaxComboActionKind.TriggerSwing1));
        }

        [Fact]
        public void Weapon_key_for_every_run_line()
        {
            Assert.Equal(new[] { ("weapon", "axes") }, MaxComboPolicy.WeaponFields(Axes));
            Assert.Equal(new[] { ("weapon", "sword") }, MaxComboPolicy.WeaponFields(MaxComboProfile.Sword));
        }

        [Fact]
        public void Config_line_carries_the_axes_switch()
        {
            Assert.Contains(("axesMaxCombo", "true"), MaxComboPolicy.ConfigFields(Loop));
            Assert.Contains(("axesMaxCombo", "false"), MaxComboPolicy.ConfigFields(Loop with { AxesMaxCombo = false }));
        }
    }

    public class SwingHitCounterTests
    {
        [Fact]
        public void Counts_hits_per_instance_and_clip()
        {
            var c = new SwingHitCounter();
            Assert.Equal(1, c.Register(7, "DualAxes_Attack_4"));
            Assert.Equal(2, c.Register(7, "DualAxes_Attack_4"));
            // dualaxes2 played: a new swing.
            Assert.Equal(1, c.Register(7, "DualAxes_Attack_3_2"));
            Assert.Equal(2, c.Register(7, "DualAxes_Attack_3_2"));
            Assert.Equal(1, c.Register(7, "DualAxes_Attack_4"));
            // A new Attack instance (relaunch) restarts the count, same clip or not.
            Assert.Equal(1, c.Register(8, "DualAxes_Attack_4"));
            Assert.Equal(2, c.Register(8, "DualAxes_Attack_4"));
        }

        [Fact]
        public void Reset_forgets_the_swing()
        {
            var c = new SwingHitCounter();
            c.Register(7, "A");
            c.Reset();
            Assert.Equal(1, c.Register(7, "A"));
        }
    }
}
