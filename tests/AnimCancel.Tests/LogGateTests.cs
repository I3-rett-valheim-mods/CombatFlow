using AnimCancel.Core;
using Xunit;

namespace AnimCancel.Tests
{
    /// <summary>Nothing but the load line and errors reaches the log unless Debug log is on.</summary>
    public class LogGateTests
    {
        [Theory]
        [InlineData(LogKind.Review)]
        [InlineData(LogKind.Lab)]
        [InlineData(LogKind.Info)]
        public void Debug_log_off_writes_nothing(LogKind kind)
        {
            Assert.False(LogGate.Allows(kind, debugLog: false, comboLabLog: false));
            Assert.False(LogGate.Allows(kind, debugLog: false, comboLabLog: true));
        }

        [Fact]
        public void Debug_log_on_writes_review_and_info_lines()
        {
            Assert.True(LogGate.Allows(LogKind.Review, true, false));
            Assert.True(LogGate.Allows(LogKind.Info, true, false));
        }

        [Fact]
        public void Lab_lines_need_both_settings()
        {
            Assert.False(LogGate.Allows(LogKind.Lab, true, false));
            Assert.True(LogGate.Allows(LogKind.Lab, true, true));
        }

        [Fact]
        public void Shipped_defaults_are_off()
        {
            Assert.False(CancelSettings.Default.DebugLog);
            Assert.False(LogGate.ComboLabLogDefault);
        }
    }
}
