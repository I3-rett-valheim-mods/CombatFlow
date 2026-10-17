using AnimCancel.Core;
using Xunit;

namespace AnimCancel.Tests
{
    public class DamageCounterTests
    {
        [Fact]
        public void Counter_counts_events_of_one_attack_instance()
        {
            var c = new DamageCounter();
            var a = new object();
            Assert.Equal(1, c.Register(a));
            Assert.Equal(2, c.Register(a));
            Assert.Equal(3, c.Register(a));
        }

        [Fact]
        public void Counter_resets_on_new_attack_instance()
        {
            var c = new DamageCounter();
            var a = new object();
            var b = new object();
            c.Register(a);
            c.Register(a);
            Assert.Equal(1, c.Register(b));
            Assert.Equal(2, c.Register(b));
            // Going back to an old instance is a new sighting too, not a resume.
            Assert.Equal(1, c.Register(a));
        }

        [Fact]
        public void Counter_compares_by_reference_not_equality()
        {
            var c = new DamageCounter();
            Assert.Equal(1, c.Register(new string('x', 3)));
            Assert.Equal(1, c.Register(new string('x', 3)));
        }
    }
}
