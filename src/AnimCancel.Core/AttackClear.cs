using System.Collections.Generic;
using System.Text;

namespace AnimCancel.Core
{
    /// <summary>
    /// ev=attack_cleared (diagnosis only): who set Humanoid.m_currentAttack to null under a max combo run.
    /// The game does it in two places, Humanoid.UnequipItem (the attack's weapon) and Humanoid.StartAttack
    /// (before Attack.Start, which may then fail).
    /// </summary>
    public static class AttackClear
    {
        /// <summary>A via=unequip or via=start_failed line within this many seconds explains an instance abort.</summary>
        public const float Window = 0.1f;

        /// <summary>lastSeen before any clear was seen.</summary>
        public const float Never = float.NegativeInfinity;

        /// <summary>The run aborted with reason=instance and neither known clear fired within <see cref="Window"/>.</summary>
        public static bool Unexplained(float now, float lastSeen) => now - lastSeen > Window + 1e-4f;

        /// <summary>
        /// Up to <paramref name="max"/> calling methods as Type.Method joined by '>', innermost first, skipping
        /// frames without a type (Harmony's patched copies), the mod's own, Harmony's and MonoMod's. "-" if none.
        /// </summary>
        public static string Callers(IEnumerable<(string? ns, string? type, string? method)> frames, int max)
        {
            var sb = new StringBuilder();
            int n = 0;
            foreach ((string? ns, string? type, string? method) in frames)
            {
                if (n >= max)
                {
                    break;
                }

                if (string.IsNullOrEmpty(type) || string.IsNullOrEmpty(method) || Skipped(ns))
                {
                    continue;
                }

                if (n > 0)
                {
                    sb.Append('>');
                }

                sb.Append(type).Append('.').Append(method);
                n++;
            }

            return n == 0 ? "-" : sb.ToString();
        }

        private static bool Skipped(string? ns) =>
            ns != null && (Starts(ns, "AnimCancel") || Starts(ns, "HarmonyLib") || Starts(ns, "MonoMod"));

        private static bool Starts(string ns, string root) =>
            ns == root || ns.StartsWith(root + ".", System.StringComparison.Ordinal);
    }
}
