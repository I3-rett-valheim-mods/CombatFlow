using AnimCancel.Core;
using Xunit;

namespace AnimCancel.Tests
{
    public class CancelSettingsTests
    {
        // Mirrors docs/DESIGN.md "Configuration". The plugin binds its config defaults
        // from CancelSettings.Default, so this table is what a fresh install gets.
        [Fact]
        public void Default_matches_design_table()
        {
            CancelSettings d = CancelSettings.Default;

            Assert.True(d.Enabled, nameof(d.Enabled));
            Assert.True(d.SwordCombo, nameof(d.SwordCombo));
            Assert.True(d.SwordSecondary, nameof(d.SwordSecondary));
            Assert.Equal(AxesComboMode.MiddleAndFinisher, d.AxesCombo);
            Assert.False(d.AxesSecondary, nameof(d.AxesSecondary));
            Assert.True(d.Dodge, nameof(d.Dodge));
            Assert.Equal(CancelEmote.NoNoNo, d.Emote);
            Assert.Equal(0f, d.DelayAfterHit);
            Assert.False(d.DebugLog, nameof(d.DebugLog));
            Assert.False(d.MaxCombo, nameof(d.MaxCombo));
        }
    }
}
