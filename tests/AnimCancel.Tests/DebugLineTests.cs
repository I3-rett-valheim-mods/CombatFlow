using System.Globalization;
using System.Text.RegularExpressions;
using System.Threading;
using AnimCancel.Core;
using Xunit;

namespace AnimCancel.Tests
{
    public class DebugLineTests
    {
        [Fact]
        public void Format_matches_review_log_grammar()
        {
            string line = DebugLine.Format(12.34567, 4021, "damage",
                new[] { ("kind", "SwordCombo"), ("lvl", "2"), ("n", "1/1"), ("clipT", "0.278") });

            Assert.Equal("AC t=12.346 f=4021 ev=damage kind=SwordCombo lvl=2 n=1/1 clipT=0.278", line);
            Assert.Matches(new Regex(@"^AC t=\d+\.\d{3} f=\d+ ev=\w+( \w+=\S+)*$"), line);
        }

        [Fact]
        public void Format_without_keys_ends_at_event()
            => Assert.Equal("AC t=0.000 f=0 ev=toggle", DebugLine.Format(0, 0, "toggle", new (string, string)[0]));

        [Fact]
        public void Format_is_culture_invariant()
        {
            CultureInfo saved = Thread.CurrentThread.CurrentCulture;
            try
            {
                Thread.CurrentThread.CurrentCulture = new CultureInfo("fr-FR");
                Assert.Equal("AC t=1.500 f=1 ev=cancel", DebugLine.Format(1.5, 1, "cancel", new (string, string)[0]));
                Assert.Equal("0.25", DebugLine.Num(0.25));
            }
            finally
            {
                Thread.CurrentThread.CurrentCulture = saved;
            }
        }

        [Fact]
        public void Values_with_spaces_are_made_single_tokens()
            => Assert.Equal("AC t=1.000 f=1 ev=x clip=Sword_Attack_R4",
                DebugLine.Format(1, 1, "x", new[] { ("clip", "Sword Attack R4") }));

        [Fact]
        public void Empty_value_is_dash()
            => Assert.Equal("AC t=1.000 f=1 ev=x clip=-", DebugLine.Format(1, 1, "x", new[] { ("clip", "") }));

        [Theory]
        [InlineData(0.2, "0.2")]
        [InlineData(0.0001, "0")]
        [InlineData(1.23456, "1.235")]
        [InlineData(2.0, "2")]
        public void Num_rounds_to_three_decimals(double v, string expected)
            => Assert.Equal(expected, DebugLine.Num(v));

        [Fact]
        public void Bool_is_lowercase()
        {
            Assert.Equal("true", DebugLine.Bool(true));
            Assert.Equal("false", DebugLine.Bool(false));
        }
    }
}
