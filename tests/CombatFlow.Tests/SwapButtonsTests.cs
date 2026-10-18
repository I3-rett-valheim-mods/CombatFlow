using CombatFlow.Core;
using Xunit;

namespace CombatFlow.Tests
{
    public class SwapButtonsTests
    {
        [Fact]
        public void Swaps_only_when_on_with_a_sword_outside_place_mode()
        {
            Assert.True(SwapButtons.ShouldSwap(enabled: true, swordEquipped: true, inPlaceMode: false));
        }

        [Theory]
        [InlineData(false, true, false)]  // mod or option off: vanilla
        [InlineData(true, false, false)]  // not a sword (hammer, bow, axe, unarmed)
        [InlineData(true, true, true)]    // build mode: the buttons place and remove pieces
        [InlineData(false, false, true)]
        public void Never_swaps_otherwise(bool enabled, bool sword, bool placeMode)
        {
            Assert.False(SwapButtons.ShouldSwap(enabled, sword, placeMode));
        }

        [Fact]
        public void Swap_exchanges_press_and_hold_of_both_buttons()
        {
            var raw = new AttackInputs(attack: true, attackHold: false, secondary: false, secondaryHold: true);

            AttackInputs s = raw.Swapped();

            Assert.False(s.Attack);
            Assert.True(s.AttackHold);
            Assert.True(s.Secondary);
            Assert.False(s.SecondaryHold);
        }

        [Fact]
        public void Swap_twice_is_the_identity()
        {
            for (int m = 0; m < 16; m++)
            {
                var raw = new AttackInputs((m & 1) != 0, (m & 2) != 0, (m & 4) != 0, (m & 8) != 0);
                Assert.Equal(raw, raw.Swapped().Swapped());
            }
        }

        [Fact]
        public void Option_is_off_by_default()
        {
            Assert.False(CancelSettings.Default.SwapButtons);
        }

        [Fact]
        public void Option_counts_only_with_the_mod_enabled()
        {
            CancelSettings on = CancelSettings.Default with { SwapButtons = true };

            Assert.True(SwapButtons.IsOn(on));
            Assert.False(SwapButtons.IsOn(on with { Enabled = false }));
            Assert.False(SwapButtons.IsOn(CancelSettings.Default));
        }

        [Fact]
        public void Sword_is_the_max_combo_weapon()
        {
            Assert.True(SwapButtons.IsSword("sword_secondary", "swing_longsword", 3));
            Assert.False(SwapButtons.IsSword("sword_secondary", "swing_longsword", 2));
            Assert.False(SwapButtons.IsSword("axe_secondary", "swing_axe", 3));
            Assert.False(SwapButtons.IsSword(null, "swing_longsword", 3));
            Assert.False(SwapButtons.IsSword("sword_secondary", null, 3));
        }

        [Fact]
        public void Lab_input_line_marks_swapped_values()
        {
            Assert.Equal(new (string, string)[0], SwapButtons.LabFields(false));
            Assert.Equal(new[] { ("swapped", "1") }, SwapButtons.LabFields(true));
        }
    }
}
