using AnimCancel.Core;
using Xunit;

namespace AnimCancel.Tests
{
    public class SpeedPlanTests
    {
        [Fact]
        public void Speed_plan_keeps_hitstop_and_overrides_restore()
        {
            // Hit-stop running: leave the 0.0001 freeze alone, make its restore value 1.
            Assert.Equal((false, true), SpeedPlan.For(0.05f));
            // No hit-stop: set the speed straight away.
            Assert.Equal((true, false), SpeedPlan.For(0f));
            Assert.Equal((true, false), SpeedPlan.For(-0.01f));
        }
    }
}
