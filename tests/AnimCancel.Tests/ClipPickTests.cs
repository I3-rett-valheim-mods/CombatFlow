using AnimCancel.Core;
using Xunit;

namespace AnimCancel.Tests
{
    /// <summary>Clip times from DESIGN.md; clip lengths are illustrative (only the wrap test depends on one).</summary>
    public class ClipPickTests
    {
        private static ClipTiming Hits(params float[] t) => new ClipTiming(t, null);

        // DualAxes 0 (DualAxes_Attack_1): Hit 0.211. DualAxes 1 (DualAxes_Attack_2_2): Hit 0.372.
        private static readonly ClipTiming Axes0 = Hits(0.211f);
        private static readonly ClipTiming Axes1 = Hits(0.372f);
        private static readonly ClipTiming Axes2 = Hits(0.335f, 0.553f);
        // swing_sword 1 (Attack2): Hit 0.259; emote clips carry no damage event.
        private static readonly ClipTiming Sword1 = Hits(0.259f);
        private static readonly ClipTiming Emote = Hits();

        [Fact]
        public void Source_clip_hit_during_attack_to_attack_blend_picks_current()
            // Pass 1: damage clipT=0.08 clip=DualAxes_Attack_2_2 while DualAxes_Attack_1 fired its Hit at ~0.22.
            => Assert.Equal(ClipPickResult.Current, ClipPick.Choose(Axes0, 0.22f, 1.0f, Axes1, 0.08f, 1.2f));

        [Fact]
        public void Next_clip_hit_during_blend_picks_next()
            => Assert.Equal(ClipPickResult.Next, ClipPick.Choose(Axes0, 0.70f, 1.0f, Axes1, 0.38f, 1.2f));

        [Fact]
        public void Second_hit_of_a_two_hit_clip_matches()
            => Assert.Equal(ClipPickResult.Next, ClipPick.Choose(Axes1, 0.80f, 1.2f, Axes2, 0.56f, 1.3f));

        [Fact]
        public void Both_or_neither_matching_is_ambiguous()
        {
            Assert.Equal(ClipPickResult.Ambiguous, ClipPick.Choose(Axes0, 0.22f, 1.0f, Sword1, 0.25f, 1.0f));
            Assert.Equal(ClipPickResult.Ambiguous, ClipPick.Choose(Axes0, 0.5f, 1.0f, Axes1, 0.1f, 1.2f));
            Assert.False(ClipPick.UseNext(ClipPickResult.Ambiguous));
        }

        [Fact]
        public void No_next_clip_is_current()
        {
            Assert.Equal(ClipPickResult.Current, ClipPick.Choose(Sword1, 0.27f, 1.0f, null, 0f, 0f));
            Assert.Equal(ClipPickResult.Current, ClipPick.Choose(Sword1, 0.6f, 1.0f, null, 0f, 0f));
        }

        [Fact]
        public void Attack_blending_into_the_emote_keeps_current()
            => Assert.Equal(ClipPickResult.Current, ClipPick.Choose(Sword1, 0.26f, 1.0f, Emote, 0.01f, 2.0f));

        [Fact]
        public void Only_next_clip_present_is_next()
            => Assert.Equal(ClipPickResult.Next, ClipPick.Choose(null, 0f, 0f, Axes1, 0.1f, 1.2f));

        [Fact]
        public void Tolerance_is_fifty_ms_of_clip_time()
        {
            Assert.True(ClipPick.HasDamageNear(Sword1, 0.259f + 0.049f, 1.0f));
            Assert.False(ClipPick.HasDamageNear(Sword1, 0.259f + 0.051f, 1.0f));
            Assert.True(ClipPick.HasDamageNear(Sword1, 0.259f - 0.049f, 1.0f));
            Assert.False(ClipPick.HasDamageNear(Emote, 0f, 1.0f));
        }

        [Fact]
        public void Distance_wraps_around_the_clip_length()
        {
            // An event at 0.39 of a 0.4 s clip is 0.02 from clip time 0.01 once the time has wrapped.
            Assert.True(ClipPick.HasDamageNear(Hits(0.39f), 0.01f, 0.4f));
            Assert.False(ClipPick.HasDamageNear(Hits(0.39f), 0.01f, 0f));
        }

        [Fact]
        public void Log_values()
        {
            Assert.Equal("current", ClipPick.LogValue(ClipPickResult.Current));
            Assert.Equal("next", ClipPick.LogValue(ClipPickResult.Next));
            Assert.Equal("ambiguous", ClipPick.LogValue(ClipPickResult.Ambiguous));
            Assert.True(ClipPick.UseNext(ClipPickResult.Next));
        }
    }
}
