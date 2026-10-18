using CombatFlow.Core;
using Xunit;

namespace CombatFlow.Tests
{
    public class AutoParryTests
    {
        [Fact]
        public void Forces_a_block_for_a_blockable_front_hit_on_the_local_player_with_a_blocker()
        {
            Assert.True(AutoParry.ShouldForce(enabled: true, autoParry: true, isLocalPlayer: true,
                blockable: true, fromFront: true, hasBlocker: true, inDodge: false, inPlaceMode: false,
                encumbered: false, inMinorAction: false, staggering: false));
            Assert.Equal(AutoParry.Force,
                AutoParry.Decide(true, true, true, true, true, true, false, false, false, false, false));
        }

        [Theory]
        [InlineData(false, true, true, true, true, true, false, false, false, false, false, "off")]           // mod disabled
        [InlineData(true, false, true, true, true, true, false, false, false, false, false, "off")]           // option off (default)
        [InlineData(true, true, false, true, true, true, false, false, false, false, false, "not_local")]     // other characters
        [InlineData(true, true, true, false, true, true, false, false, false, false, false, "not_blockable")] // hit not blockable
        [InlineData(true, true, true, true, false, true, false, false, false, false, false, "behind")]        // vanilla direction rule
        [InlineData(true, true, true, true, true, false, false, false, false, false, false, "no_blocker")]    // nothing to block with
        // Vanilla Humanoid.IsBlocking conditions kept (only m_blocking and !InAttack() are bypassed):
        [InlineData(true, true, true, true, true, true, true, false, false, false, false, "dodge")]           // !InDodge()
        [InlineData(true, true, true, true, true, true, false, true, false, false, false, "place_mode")]      // !InPlaceMode()
        [InlineData(true, true, true, true, true, true, false, false, true, false, false, "encumbered")]      // !IsEncumbered()
        [InlineData(true, true, true, true, true, true, false, false, false, true, false, "minor_action")]    // !InMinorAction()
        [InlineData(true, true, true, true, true, true, false, false, false, false, true, "staggering")]      // !IsStaggering()
        public void Never_forces_otherwise(bool enabled, bool autoParry, bool local, bool blockable, bool front,
            bool blocker, bool dodge, bool placeMode, bool encumbered, bool minorAction, bool staggering, string reason)
        {
            Assert.False(AutoParry.ShouldForce(enabled, autoParry, local, blockable, front, blocker, dodge, placeMode,
                encumbered, minorAction, staggering));
            Assert.Equal(reason, AutoParry.Decide(enabled, autoParry, local, blockable, front, blocker, dodge, placeMode,
                encumbered, minorAction, staggering));
        }

        [Fact]
        public void Off_wins_over_every_other_reason()
        {
            Assert.Equal("off", AutoParry.Decide(true, false, false, false, false, false, true, true, true, true, true));
            Assert.Equal("off", AutoParry.Decide(false, true, false, false, false, false, true, true, true, true, true));
        }

        [Fact]
        public void Forced_blocking_value_mirrors_vanilla_minus_button_and_attack()
        {
            Assert.True(AutoParry.ForcedBlocking(inDodge: false, inPlaceMode: false, encumbered: false,
                inMinorAction: false, staggering: false));
            for (int m = 1; m < 32; m++)
            {
                Assert.False(AutoParry.ForcedBlocking((m & 1) != 0, (m & 2) != 0, (m & 4) != 0, (m & 8) != 0,
                    (m & 16) != 0));
            }
        }

        [Theory]
        [InlineData(-0.5f, false)]   // facing the attacker: hit travels against forward
        [InlineData(0f, false)]      // side-on: vanilla blocks (Dot > 0 is the only reject)
        [InlineData(0.0001f, true)]  // any positive dot: from behind
        [InlineData(1f, true)]       // straight in the back
        public void From_front_mirrors_BlockAttack_direction_rule(float dotDirForward, bool behind)
        {
            Assert.Equal(!behind, AutoParry.FromFront(dotDirForward));
        }

        [Theory]
        [InlineData(1.5f, false, true, "perfect")]
        [InlineData(1.5f, false, false, "blocked")]      // no attacker: vanilla skips the perfect effects
        [InlineData(2f, true, true, "guard_broken")]
        [InlineData(1f, false, true, "blocked")]         // no timed block bonus: an ordinary block
        [InlineData(0f, true, false, "guard_broken")]
        public void Outcome_names_what_the_forced_block_did(float timedBlockBonus, bool guardBroken, bool hasAttacker,
            string result)
        {
            Assert.Equal(result, AutoParry.Outcome(timedBlockBonus, guardBroken, hasAttacker));
        }

        [Fact]
        public void Perfect_window_timer_is_inside_the_game_window()
        {
            // Humanoid.BlockAttack: perfect when m_blockTimer != -1 && m_blockTimer < 0.25.
            Assert.True(AutoParry.PerfectBlockTimer != -1f && AutoParry.PerfectBlockTimer < 0.25f);
        }

        [Fact]
        public void Option_is_off_by_default()
        {
            Assert.False(CancelSettings.Default.AutoParry);
        }
    }
}
