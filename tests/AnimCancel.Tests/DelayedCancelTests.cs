using AnimCancel.Core;
using Xunit;

namespace AnimCancel.Tests
{
    public class DelayedCancelTests
    {
        static void Check(CancelDecision d, bool cancel, string reason)
        {
            Assert.Equal(cancel, d.Cancel);
            Assert.Equal(reason, d.Reason);
        }

        [Fact]
        public void Same_attack_still_attacking_fires()
            => Check(CancelPolicy.ShouldFireDelayed(enabled: true, sameAttack: true, staggering: false, inAttack: true), true, "ok");

        [Fact]
        public void Disabled_drops()
            => Check(CancelPolicy.ShouldFireDelayed(false, true, false, true), false, "disabled");

        [Fact]
        public void New_attack_instance_is_superseded()
            => Check(CancelPolicy.ShouldFireDelayed(true, false, false, true), false, "superseded");

        [Fact]
        public void Stagger_during_delay_is_superseded()
            => Check(CancelPolicy.ShouldFireDelayed(true, true, true, true), false, "superseded");

        [Fact]
        public void Left_attack_state_during_delay_is_superseded()
            => Check(CancelPolicy.ShouldFireDelayed(true, true, false, false), false, "superseded");
    }
}
