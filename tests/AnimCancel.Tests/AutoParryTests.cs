using AnimCancel.Core;
using Xunit;

namespace AnimCancel.Tests
{
    public class AutoParryTests
    {
        [Fact]
        public void Forces_a_block_for_a_blockable_front_hit_on_the_local_player_with_a_blocker()
        {
            Assert.True(AutoParry.ShouldForce(enabled: true, autoParry: true, isLocalPlayer: true,
                blockable: true, fromFront: true, hasBlocker: true, inDodge: false));
            Assert.Equal(AutoParry.Force, AutoParry.Decide(true, true, true, true, true, true, false));
        }

        [Theory]
        [InlineData(false, true, true, true, true, true, false, "off")]           // mod disabled
        [InlineData(true, false, true, true, true, true, false, "off")]           // option off (default)
        [InlineData(true, true, false, true, true, true, false, "not_local")]     // other characters: vanilla
        [InlineData(true, true, true, false, true, true, false, "not_blockable")] // hit not blockable
        [InlineData(true, true, true, true, false, true, false, "behind")]        // vanilla direction rule
        [InlineData(true, true, true, true, true, false, false, "no_blocker")]    // no shield or weapon
        [InlineData(true, true, true, true, true, true, true, "dodge")]           // dodging: vanilla
        public void Never_forces_otherwise(bool enabled, bool autoParry, bool local, bool blockable, bool front,
            bool blocker, bool dodge, string reason)
        {
            Assert.False(AutoParry.ShouldForce(enabled, autoParry, local, blockable, front, blocker, dodge));
            Assert.Equal(reason, AutoParry.Decide(enabled, autoParry, local, blockable, front, blocker, dodge));
        }

        [Fact]
        public void Off_wins_over_every_other_reason()
        {
            Assert.Equal("off", AutoParry.Decide(true, false, false, false, false, false, true));
            Assert.Equal("off", AutoParry.Decide(false, true, false, false, false, false, true));
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
        [InlineData(1.5f, false, "perfect")]
        [InlineData(2f, true, "guard_broken")]
        [InlineData(1f, false, "blocked")]       // no timed block bonus: an ordinary block
        [InlineData(0f, true, "guard_broken")]
        public void Outcome_names_what_the_forced_block_did(float timedBlockBonus, bool guardBroken, string result)
        {
            Assert.Equal(result, AutoParry.Outcome(timedBlockBonus, guardBroken));
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
