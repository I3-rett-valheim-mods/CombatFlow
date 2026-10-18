using AnimCancel.Core;
using Xunit;

namespace AnimCancel.Tests
{
    public class HoldDropTests
    {
        [Fact]
        public void Held_then_cleared_is_a_drop() => Assert.True(HoldDrop.WasDropped(true, false));

        [Fact]
        public void Held_and_kept_is_not_a_drop() => Assert.False(HoldDrop.WasDropped(true, true));

        [Fact]
        public void Not_held_is_never_a_drop()
        {
            Assert.False(HoldDrop.WasDropped(false, false));
            Assert.False(HoldDrop.WasDropped(false, true));
        }

        [Fact]
        public void In_flight_is_watched_regardless_of_time() => Assert.True(HoldDrop.ShouldWatch(true, 100f, 0f));

        [Fact]
        public void Within_window_after_cancel_is_watched() => Assert.True(HoldDrop.ShouldWatch(false, 10.4f, 10f));

        [Fact]
        public void Past_window_is_not_watched() => Assert.False(HoldDrop.ShouldWatch(false, 10.6f, 10f));

        [Fact]
        public void Before_any_cancel_is_not_watched() => Assert.False(HoldDrop.ShouldWatch(false, 5f, 6f));
    }
}
