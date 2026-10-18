using CombatFlow.Core;
using Xunit;

namespace CombatFlow.Tests
{
    public class RelaunchBlockTests
    {
        [Fact]
        public void Duplicate_start_refused_while_blending_into_the_emote_after_the_relaunch()
            // t=58.040 in pass 1: relaunch consumed at 58.020, layer 0 cancelled attack -> emote.
            => Assert.True(RelaunchBlock.ShouldRefuse(isLocalPlayer: true, active: true, sinceCancel: 0.06f, layer0Attack: false));

        [Fact]
        public void Nothing_refused_when_no_relaunch_was_consumed_for_the_cancel()
        {
            // No cancel in progress, or the first relaunch itself: vanilla behaviour.
            Assert.False(RelaunchBlock.ShouldRefuse(true, active: false, sinceCancel: 0.06f, layer0Attack: false));
            Assert.False(RelaunchBlock.ShouldRefuse(true, active: false, sinceCancel: 0f, layer0Attack: true));
        }

        [Fact]
        public void Never_affects_another_character()
            => Assert.False(RelaunchBlock.ShouldRefuse(isLocalPlayer: false, active: true, sinceCancel: 0.06f, layer0Attack: false));

        [Fact]
        public void Lifts_once_layer_0_current_or_next_is_an_attack()
        {
            Assert.False(RelaunchBlock.ShouldRefuse(true, true, sinceCancel: 0.2f, layer0Attack: true));
            Assert.True(RelaunchBlock.ShouldLift(active: true, sinceCancel: 0.2f, layer0Attack: true));
        }

        [Fact]
        public void Never_outlives_half_a_second_after_the_cancel()
        {
            Assert.True(RelaunchBlock.ShouldRefuse(true, true, sinceCancel: 0.49f, layer0Attack: false));
            Assert.False(RelaunchBlock.ShouldRefuse(true, true, sinceCancel: RelaunchBlock.Window, layer0Attack: false));
            Assert.False(RelaunchBlock.ShouldRefuse(true, true, sinceCancel: 0.8f, layer0Attack: false));
            Assert.True(RelaunchBlock.ShouldLift(true, sinceCancel: RelaunchBlock.Window, layer0Attack: false));
            Assert.Equal(0.5f, RelaunchBlock.Window);
        }

        [Fact]
        public void Inactive_block_has_nothing_to_lift()
        {
            Assert.False(RelaunchBlock.ShouldLift(active: false, sinceCancel: 0.8f, layer0Attack: true));
            Assert.False(RelaunchBlock.ShouldLift(active: true, sinceCancel: 0.1f, layer0Attack: false));
        }

        [Fact]
        public void A_clock_going_backwards_lifts_the_block()
        {
            Assert.True(RelaunchBlock.ShouldLift(true, sinceCancel: -1f, layer0Attack: false));
            Assert.False(RelaunchBlock.ShouldRefuse(true, true, sinceCancel: -1f, layer0Attack: false));
        }

        [Fact]
        public void Blend_out_of_the_cancelled_attack_into_the_emote_is_not_the_relaunch()
            // Pass 1 at t=58.040: current = cancelled attack (tag attack), next = emote. Player.InAttack() reads
            // layer 0 as next-or-current, which is the emote: StartAttack would succeed, so the block must hold.
            => Assert.False(RelaunchBlock.AttackBegun(inTransition: true, currentIsAttack: true, nextIsAttack: false, currentIsCancelled: true));

        [Fact]
        public void Attack_entering_after_the_emote_has_begun()
        {
            // emote -> relaunched attack.
            Assert.True(RelaunchBlock.AttackBegun(inTransition: true, currentIsAttack: false, nextIsAttack: true, currentIsCancelled: false));
            // Relaunch trigger won over the emote: cancelled attack -> new attack (or itself, sword secondary).
            Assert.True(RelaunchBlock.AttackBegun(inTransition: true, currentIsAttack: true, nextIsAttack: true, currentIsCancelled: true));
            // Relaunched attack fully current.
            Assert.True(RelaunchBlock.AttackBegun(inTransition: false, currentIsAttack: true, nextIsAttack: false, currentIsCancelled: false));
        }

        [Fact]
        public void Cancelled_attack_still_current_before_the_blend_has_not_begun()
        {
            Assert.False(RelaunchBlock.AttackBegun(inTransition: false, currentIsAttack: true, nextIsAttack: false, currentIsCancelled: true));
            // Fully in the emote.
            Assert.False(RelaunchBlock.AttackBegun(inTransition: false, currentIsAttack: false, nextIsAttack: false, currentIsCancelled: false));
        }

        [Fact]
        public void Block_is_set_only_for_a_relaunch_inside_the_window()
        {
            Assert.True(RelaunchBlock.ShouldStart(0f));
            Assert.True(RelaunchBlock.ShouldStart(RelaunchBlock.Window - 0.01f));
            Assert.False(RelaunchBlock.ShouldStart(RelaunchBlock.Window));
            Assert.False(RelaunchBlock.ShouldStart(2f));
            Assert.False(RelaunchBlock.ShouldStart(-0.1f));
        }
    }
}
