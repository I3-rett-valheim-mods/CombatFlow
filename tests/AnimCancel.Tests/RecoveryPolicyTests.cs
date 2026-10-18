using AnimCancel.Core;
using Xunit;

namespace AnimCancel.Tests
{
    public class RecoveryPolicyTests
    {
        [Fact]
        public void Recovery_requests_emote_stop_when_hold_released()
            => Assert.True(RecoveryPolicy.ShouldStop(inFlight: true, currentTagIsEmote: true, inTransition: false, attackHeld: false));

        [Fact]
        public void Not_in_flight_never_stops()
            => Assert.False(RecoveryPolicy.ShouldStop(false, true, false, false));

        [Fact]
        public void Still_blending_into_emote_waits()
            => Assert.False(RecoveryPolicy.ShouldStop(true, true, true, false));

        [Fact]
        public void Not_yet_in_emote_waits()
            => Assert.False(RecoveryPolicy.ShouldStop(true, false, false, false));

        [Fact]
        public void Hold_still_down_lets_the_relaunch_happen()
            => Assert.False(RecoveryPolicy.ShouldStop(true, true, false, true));
    }
}
