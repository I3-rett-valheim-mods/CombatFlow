using CombatFlow.Core;
using Xunit;

namespace CombatFlow.Tests
{
    public class SpeedSkipTests
    {
        private const int Attack = 111;
        private const int Emote = 222;

        [Fact]
        public void Speed_event_skipped_while_cancelled_state_plays_shortly_after_cancel()
        {
            // Blending out of the cancelled attack.
            Assert.True(SpeedSkip.ShouldSkip(cancelledState: Attack, currentState: Attack, nextState: Emote, sinceCancel: 0.1f));
            // Nothing cancelled.
            Assert.False(SpeedSkip.ShouldSkip(null, Attack, Emote, 0.1f));
            // Neither current nor next is the cancelled state.
            Assert.False(SpeedSkip.ShouldSkip(Attack, Emote, 0, 0.1f));
        }

        [Fact]
        public void Same_evaluation_before_the_blend_is_skipped()
            // Cancel ran inside the Hit event; Speed fires ~17 ms later in the same evaluation, no transition yet.
            => Assert.True(SpeedSkip.ShouldSkip(Attack, currentState: Attack, nextState: 0, sinceCancel: 0f));

        [Fact]
        public void Next_state_match_is_skipped()
            // Cancelled while blending into the attack.
            => Assert.True(SpeedSkip.ShouldSkip(Attack, currentState: Emote, nextState: Attack, sinceCancel: 0.05f));

        [Fact]
        public void No_skip_after_half_a_second()
        {
            Assert.True(SpeedSkip.ShouldSkip(Attack, Attack, 0, 0.5f));
            Assert.False(SpeedSkip.ShouldSkip(Attack, Attack, 0, 0.51f));
        }

        [Fact]
        public void Cancelled_state_forgotten_once_neither_current_nor_next()
        {
            Assert.False(SpeedSkip.ShouldForget(Attack, currentState: Attack, nextState: Emote));
            Assert.False(SpeedSkip.ShouldForget(Attack, currentState: Emote, nextState: Attack));
            Assert.True(SpeedSkip.ShouldForget(Attack, currentState: Emote, nextState: 0));
            Assert.False(SpeedSkip.ShouldForget(null, Emote, 0));
        }

        [Fact]
        public void Relaunched_skips_only_while_cancelled_state_is_current()
        {
            // Relaunch inside a hit-stop freeze: the old clip is still current, its Speed(0.2) must stay skipped.
            Assert.True(SpeedSkip.ShouldSkip(Attack, relaunched: true, currentState: Attack, nextState: Emote, sinceCancel: 0.2f));
            Assert.True(SpeedSkip.ShouldSkip(Attack, relaunched: true, currentState: Attack, nextState: 0, sinceCancel: 0.2f));
            // The relaunched attack (same hash) blending in from the emote keeps its own events.
            Assert.False(SpeedSkip.ShouldSkip(Attack, relaunched: true, currentState: Emote, nextState: Attack, sinceCancel: 0.3f));
            // Still bounded by the window.
            Assert.False(SpeedSkip.ShouldSkip(Attack, relaunched: true, currentState: Attack, nextState: Emote, sinceCancel: 0.51f));
            // Not relaunched: unchanged behaviour.
            Assert.True(SpeedSkip.ShouldSkip(Attack, relaunched: false, currentState: Emote, nextState: Attack, sinceCancel: 0.05f));
        }

        [Fact]
        public void Relaunched_forgets_as_soon_as_cancelled_state_is_not_current()
        {
            Assert.False(SpeedSkip.ShouldForget(Attack, relaunched: true, currentState: Attack, nextState: Emote));
            // Emote current, same-state relaunch already next: forget before it becomes current again.
            Assert.True(SpeedSkip.ShouldForget(Attack, relaunched: true, currentState: Emote, nextState: Attack));
            Assert.True(SpeedSkip.ShouldForget(Attack, relaunched: true, currentState: Emote, nextState: 0));
            Assert.False(SpeedSkip.ShouldForget(null, relaunched: true, currentState: Emote, nextState: 0));
            // Not relaunched: next still counts.
            Assert.False(SpeedSkip.ShouldForget(Attack, relaunched: false, currentState: Emote, nextState: Attack));
        }
    }
}
