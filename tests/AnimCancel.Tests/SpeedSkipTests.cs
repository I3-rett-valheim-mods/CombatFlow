using AnimCancel.Core;
using Xunit;

namespace AnimCancel.Tests
{
    public class SpeedSkipTests
    {
        private const int Attack = 111;
        private const int Emote = 222;

        [Fact]
        public void Speed_event_skipped_only_during_cancel_blend()
        {
            Assert.True(SpeedSkip.ShouldSkip(cancelledState: Attack, currentState: Attack, inTransition: true));
            // Cancel did not (yet) blend: the clip's own speed events stay vanilla.
            Assert.False(SpeedSkip.ShouldSkip(Attack, Attack, inTransition: false));
            // Emote (or a relaunch blending in) is current: not the cancelled clip.
            Assert.False(SpeedSkip.ShouldSkip(Attack, Emote, inTransition: true));
            // Nothing cancelled.
            Assert.False(SpeedSkip.ShouldSkip(null, Attack, inTransition: true));
        }

        [Fact]
        public void Cancelled_state_forgotten_once_neither_current_nor_next()
        {
            Assert.False(SpeedSkip.ShouldForget(Attack, currentState: Attack, nextState: Emote));
            Assert.False(SpeedSkip.ShouldForget(Attack, currentState: Emote, nextState: Attack));
            Assert.True(SpeedSkip.ShouldForget(Attack, currentState: Emote, nextState: 0));
            Assert.False(SpeedSkip.ShouldForget(null, Emote, 0));
        }
    }
}
