using System.Globalization;
using System.Text.RegularExpressions;
using System.Threading;
using AnimCancel.Core;
using Xunit;

namespace AnimCancel.Tests
{
    public class LabLineTests
    {
        [Fact]
        public void Format_matches_combo_lab_grammar()
        {
            string line = LabLine.Format(12.34567, 4021, 9876, "start",
                new[] { ("secondary", "1"), ("result", "1"), ("anim", "sword_secondary") });

            Assert.Equal("CL t=12.346 f=4021 ff=9876 ev=start secondary=1 result=1 anim=sword_secondary", line);
            Assert.Matches(new Regex(@"^CL t=\d+\.\d{3} f=\d+ ff=\d+ ev=\w+( \w+=\S+)*$"), line);
        }

        [Fact]
        public void Format_without_keys_ends_at_event()
            => Assert.Equal("CL t=0.000 f=0 ff=0 ev=block", LabLine.Format(0, 0, 0, "block", new (string, string)[0]));

        [Fact]
        public void Format_keeps_negative_hashes_as_one_token()
            => Assert.Equal("CL t=1.000 f=2 ff=3 ev=state cur=-123456 next=0",
                LabLine.Format(1, 2, 3, "state", new[] { ("cur", "-123456"), ("next", "0") }));

        [Fact]
        public void Values_with_spaces_become_single_tokens_and_empty_is_dash()
            => Assert.Equal("CL t=1.000 f=1 ff=1 ev=hit target=Training_dummy anim=-",
                LabLine.Format(1, 1, 1, "hit", new[] { ("target", "Training dummy"), ("anim", "") }));

        [Fact]
        public void Format_and_Dp_are_culture_invariant()
        {
            CultureInfo saved = Thread.CurrentThread.CurrentCulture;
            try
            {
                Thread.CurrentThread.CurrentCulture = new CultureInfo("fr-FR");
                Assert.Equal("CL t=1.500 f=1 ff=2 ev=x", LabLine.Format(1.5, 1, 2, "x", new (string, string)[0]));
                Assert.Equal("12.5", LabLine.Dp(12.46, 1));
                Assert.Equal("0.25", LabLine.Dp(0.249, 2));
            }
            finally
            {
                Thread.CurrentThread.CurrentCulture = saved;
            }
        }

        [Theory]
        [InlineData(36.0, 1, "36.0")]
        [InlineData(0.004, 2, "0.00")]
        [InlineData(1.999, 2, "2.00")]
        [InlineData(-0.5, 1, "-0.5")]
        public void Dp_has_fixed_decimals(double v, int decimals, string expected)
            => Assert.Equal(expected, LabLine.Dp(v, decimals));

        [Fact]
        public void Bit_is_one_or_zero()
        {
            Assert.Equal("1", LabLine.Bit(true));
            Assert.Equal("0", LabLine.Bit(false));
        }

        [Theory]
        [InlineData(2, 3, true)]
        [InlineData(1, 3, false)]
        [InlineData(0, 1, false)]  // a single-blow attack is not a chain
        [InlineData(0, 0, false)]
        [InlineData(3, 4, true)]
        public void IsLastChain_needs_a_chain_and_its_last_level(int lvl, int levels, bool expected)
            => Assert.Equal(expected, LabLine.IsLastChain(lvl, levels));

        [Fact]
        public void InputMask_gives_each_button_its_own_bit()
        {
            Assert.Equal(0, LabLine.InputMask(false, false, false, false, false, false));
            Assert.Equal(1, LabLine.InputMask(true, false, false, false, false, false));
            Assert.Equal(2, LabLine.InputMask(false, true, false, false, false, false));
            Assert.Equal(4, LabLine.InputMask(false, false, true, false, false, false));
            Assert.Equal(8, LabLine.InputMask(false, false, false, true, false, false));
            Assert.Equal(16, LabLine.InputMask(false, false, false, false, true, false));
            Assert.Equal(32, LabLine.InputMask(false, false, false, false, false, true));
            Assert.Equal(63, LabLine.InputMask(true, true, true, true, true, true));
        }

        [Theory]
        [InlineData(10, "attack")]
        [InlineData(20, "emote")]
        [InlineData(30, "dodge")]
        [InlineData(40, "idle")]
        [InlineData(0, "other")]
        [InlineData(99, "other")]
        public void TagName_maps_known_tag_hashes(int tag, string expected)
            => Assert.Equal(expected, LabLine.TagName(tag, attack: 10, emote: 20, dodge: 30, idle: 40));
    }
}
