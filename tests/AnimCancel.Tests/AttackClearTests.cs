using AnimCancel.Core;
using Xunit;

namespace AnimCancel.Tests
{
    /// <summary>ev=attack_cleared helpers: the caller list and the via=unknown window.</summary>
    public class AttackClearTests
    {
        [Fact]
        public void Callers_skip_harmony_mod_and_dynamic_frames()
        {
            var frames = new (string? ns, string? type, string? method)[]
            {
                ("AnimCancel.Patches", "UnequipPatch", "Prefix"),
                (null, null, "DMD<Humanoid::UnequipItem>"),
                (null, "DMD<Humanoid::StartAttack>", "Humanoid::StartAttack"),
                ("", "Humanoid", "DMD<Humanoid::UnequipItem>"),
                ("HarmonyLib", "Traverse", "Call"),
                ("MonoMod.Utils", "DynamicMethodDefinition", "Generate"),
                ("", "Humanoid", "UnequipAllItems"),
                ("", "Player", "OnDeath"),
                ("", "Character", "ApplyDamage"),
                ("", "Character", "Damage"),
            };
            Assert.Equal("Humanoid.UnequipAllItems>Player.OnDeath>Character.ApplyDamage", AttackClear.Callers(frames, 3));
            Assert.Equal("Humanoid.UnequipAllItems>Player.OnDeath", AttackClear.Callers(frames, 2));
        }

        [Fact]
        public void Callers_without_game_frames_are_a_dash()
        {
            Assert.Equal("-", AttackClear.Callers(new (string?, string?, string?)[] { ("HarmonyLib", "X", "Y") }, 3));
            Assert.Equal("-", AttackClear.Callers(new (string?, string?, string?)[0], 3));
        }

        [Fact]
        public void Unknown_only_when_no_clear_was_seen_within_a_tenth_of_a_second()
        {
            Assert.True(AttackClear.Unexplained(now: 10f, lastSeen: AttackClear.Never));
            Assert.False(AttackClear.Unexplained(10f, 9.95f));
            Assert.False(AttackClear.Unexplained(10f, 9.9f));
            Assert.True(AttackClear.Unexplained(10f, 9.85f));
        }
    }
}
