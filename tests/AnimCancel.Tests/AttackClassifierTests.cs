using System.Collections.Generic;
using AnimCancel.Core;
using Xunit;

namespace AnimCancel.Tests
{
    public class AttackClassifierTests
    {
        [Theory]
        [InlineData("swing_longsword", AttackKind.SwordCombo)]
        [InlineData("sword_secondary", AttackKind.SwordSecondary)]
        [InlineData("dualaxes", AttackKind.AxesCombo)]
        [InlineData("dualaxes_secondary", AttackKind.AxesSecondary)]
        public void Known_animations_classify(string anim, AttackKind expected)
            => Assert.Equal(expected, AttackClassifier.Classify(anim));

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("Dualaxes")]
        [InlineData("swing_axe")]
        public void Unknown_animation_is_None(string? anim)
            => Assert.Equal(AttackKind.None, AttackClassifier.Classify(anim));

        [Fact]
        public void FromEvents_ignores_trail_and_speed_events()
        {
            var t = ClipTiming.FromEvents(new List<(float, string)>
            {
                (0.5f, "Hit"), (0.1f, "TrailOn"), (0.2f, "OnAttackTrigger"),
                (0.3f, "Speed"), (0.6f, "Chain"), (0.7f, "Chain"), (0.8f, "TrailOff"),
            });
            Assert.Equal(new[] { 0.2f, 0.5f }, t.DamageEventTimes);
            Assert.Equal(0.6f, t.ChainTime);
        }

        [Fact]
        public void FromEvents_without_chain_has_null_chain()
        {
            var t = ClipTiming.FromEvents(new List<(float, string)> { (0.278f, "Hit") });
            Assert.Null(t.ChainTime);
        }

        [Fact]
        public void Emote_triggers_and_blends()
        {
            Assert.Equal("emote_nonono", CancelEmotes.Trigger(CancelEmote.NoNoNo));
            Assert.Equal("emote_cheer", CancelEmotes.Trigger(CancelEmote.Cheer));
            Assert.Equal(0.11f, CancelEmotes.BlendSeconds(CancelEmote.NoNoNo));
            Assert.Equal(0.19f, CancelEmotes.BlendSeconds(CancelEmote.Cheer));
        }

        [Fact]
        public void Default_settings()
        {
            var d = CancelSettings.Default;
            Assert.Equal(new CancelSettings(true, true, true, AxesComboMode.MiddleAndFinisher, false, true, CancelEmote.NoNoNo, 0f, false, true, MaxComboMode.Loop, 0, AxesMaxCombo: true), d);
        }
    }
}
