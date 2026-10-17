using AnimCancel.Core;
using Xunit;

namespace AnimCancel.Tests
{
    public class CancelPolicyTests
    {
        static ClipTiming T(float? chain, params float[] dmg) => new ClipTiming(dmg, chain);
        static readonly CancelSettings D = CancelSettings.Default;

        static void Check(CancelDecision d, bool cancel, string reason)
        {
            Assert.Equal(cancel, d.Cancel);
            Assert.Equal(reason, d.Reason);
        }

        [Fact]
        public void Sword_combo_levels_all_cancel()
        {
            Check(CancelPolicy.ShouldCancel(AttackKind.SwordCombo, 0, 3, T(0.73f, 0.472f), 1, 1f, D), true, "ok");
            Check(CancelPolicy.ShouldCancel(AttackKind.SwordCombo, 1, 3, T(0.557f, 0.259f), 1, 1f, D), true, "ok");
            Check(CancelPolicy.ShouldCancel(AttackKind.SwordCombo, 2, 3, T(null, 0.278f), 1, 1f, D), true, "ok");
        }

        [Fact]
        public void Axes_level0_never_beats_chain()
            => Check(CancelPolicy.ShouldCancel(AttackKind.AxesCombo, 0, 4, T(0.257f, 0.211f), 1, 1f, D), false, "chain_faster");

        [Fact]
        public void Axes_middle_levels_cancel_in_MiddleAndFinisher()
        {
            Check(CancelPolicy.ShouldCancel(AttackKind.AxesCombo, 1, 4, T(0.533f, 0.372f), 1, 1f, D), true, "ok");
            Check(CancelPolicy.ShouldCancel(AttackKind.AxesCombo, 2, 4, T(0.706f, 0.335f, 0.553f), 2, 1f, D), true, "ok");
        }

        [Fact]
        public void Axes_middle_levels_blocked_in_FinisherOnly()
        {
            var s = D with { AxesCombo = AxesComboMode.FinisherOnly };
            Check(CancelPolicy.ShouldCancel(AttackKind.AxesCombo, 1, 4, T(0.533f, 0.372f), 1, 1f, s), false, "finisher_only");
            Check(CancelPolicy.ShouldCancel(AttackKind.AxesCombo, 2, 4, T(0.706f, 0.335f, 0.553f), 2, 1f, s), false, "finisher_only");
        }

        [Fact]
        public void Axes_finisher_cancels_in_both_modes()
        {
            var t = T(null, 0.367f, 0.540f);
            Check(CancelPolicy.ShouldCancel(AttackKind.AxesCombo, 3, 4, t, 2, 1f, D), true, "ok");
            var s = D with { AxesCombo = AxesComboMode.FinisherOnly };
            Check(CancelPolicy.ShouldCancel(AttackKind.AxesCombo, 3, 4, t, 2, 1f, s), true, "ok");
        }

        [Fact]
        public void Two_hit_clip_cancels_only_after_second_hit()
        {
            var t = T(null, 0.367f, 0.540f);
            Check(CancelPolicy.ShouldCancel(AttackKind.AxesCombo, 3, 4, t, 1, 1f, D), false, "not_last_hit");
            Check(CancelPolicy.ShouldCancel(AttackKind.AxesCombo, 3, 4, t, 2, 1f, D), true, "ok");
        }

        [Fact]
        public void No_damage_events_is_not_last_hit()
            => Check(CancelPolicy.ShouldCancel(AttackKind.SwordCombo, 0, 3, T(null), 0, 1f, D), false, "not_last_hit");

        [Fact]
        public void Cheer_blend_can_lose_to_chain()
        {
            var s = D with { Emote = CancelEmote.Cheer };
            Check(CancelPolicy.ShouldCancel(AttackKind.AxesCombo, 1, 4, T(0.533f, 0.372f), 1, 1f, s), false, "chain_faster");
        }

        [Fact]
        public void Axes_secondary_off_by_default()
        {
            var t = T(null, 0.4f);
            Check(CancelPolicy.ShouldCancel(AttackKind.AxesSecondary, 0, 1, t, 1, 1f, D), false, "toggle_off");
            Check(CancelPolicy.ShouldCancel(AttackKind.AxesSecondary, 0, 1, t, 1, 1f, D with { AxesSecondary = true }), true, "ok");
        }

        [Fact]
        public void Sword_secondary_cancels_by_default()
            => Check(CancelPolicy.ShouldCancel(AttackKind.SwordSecondary, 0, 1, T(null, 0.4f), 1, 1f, D), true, "ok");

        [Fact]
        public void Disabled_settings_never_cancel()
        {
            var s = D with { Enabled = false };
            Check(CancelPolicy.ShouldCancel(AttackKind.SwordCombo, 0, 3, T(null, 0.3f), 1, 1f, s), false, "disabled");
            Check(CancelPolicy.ShouldCancelDodge(s), false, "disabled");
        }

        [Fact]
        public void None_kind_is_not_handled()
            => Check(CancelPolicy.ShouldCancel(AttackKind.None, 0, 1, T(null, 0.3f), 1, 1f, D), false, "not_handled");

        [Fact]
        public void Sword_toggle_off()
            => Check(CancelPolicy.ShouldCancel(AttackKind.SwordCombo, 0, 3, T(null, 0.3f), 1, 1f, D with { SwordCombo = false }), false, "toggle_off");

        [Fact]
        public void State_speed_scales_blend()
        {
            // 0.372 + 0.11*2 = 0.592 > 0.533
            Check(CancelPolicy.ShouldCancel(AttackKind.AxesCombo, 1, 4, T(0.533f, 0.372f), 1, 2f, D), false, "chain_faster");
        }

        [Fact]
        public void Dodge()
        {
            Check(CancelPolicy.ShouldCancelDodge(D), true, "ok");
            Check(CancelPolicy.ShouldCancelDodge(D with { Dodge = false }), false, "toggle_off");
        }
    }
}
