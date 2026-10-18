using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace CombatFlow.Core
{
    /// <summary>
    /// Builds one combo lab line: <c>CL t=&lt;s, 3 dp&gt; f=&lt;frame&gt; ff=&lt;update count&gt; ev=&lt;event&gt; key=value ...</c>.
    /// Same token rules as <see cref="DebugLine"/> (culture-invariant, spaces to '_', empty to '-'),
    /// different prefix so the two logs can be told apart in one LogOutput.log.
    /// </summary>
    public static class LabLine
    {
        public static string Format(double t, int frame, int ff, string ev, IReadOnlyList<(string k, string v)> kv)
        {
            var sb = new StringBuilder(80);
            sb.Append("CL t=").Append(t.ToString("0.000", CultureInfo.InvariantCulture))
              .Append(" f=").Append(frame.ToString(CultureInfo.InvariantCulture))
              .Append(" ff=").Append(ff.ToString(CultureInfo.InvariantCulture))
              .Append(" ev=").Append(ev);
            for (int i = 0; i < kv.Count; i++)
            {
                sb.Append(' ').Append(kv[i].k).Append('=').Append(Token(kv[i].v));
            }
            return sb.ToString();
        }

        /// <summary>A number with exactly <paramref name="decimals"/> decimals, invariant culture.</summary>
        public static string Dp(double v, int decimals) =>
            v.ToString("F" + decimals.ToString(CultureInfo.InvariantCulture), CultureInfo.InvariantCulture);

        public static string Bit(bool b) => b ? "1" : "0";

        /// <summary>The blow is the last of a chain of at least two (lvl is 0-based).</summary>
        public static bool IsLastChain(int lvl, int levels) => levels > 1 && lvl == levels - 1;

        /// <summary>
        /// The damage multiplier a blow gets from Attack.ModifyDamage (m_damageMultiplier) and, on the last blow of
        /// a chain, the extra m_lastChainDamageMultiplier (Attack.DoMeleeAttack: levels > 1, lvl == levels - 1,
        /// multiplier > 1).
        /// </summary>
        public static float SwingMultiplier(float damageMultiplier, float lastChainMultiplier, int lvl, int levels) =>
            IsLastChain(lvl, levels) && lastChainMultiplier > 1f ? damageMultiplier * lastChainMultiplier : damageMultiplier;

        /// <summary>The six SetControls buttons packed into one int, for change detection.</summary>
        public static int InputMask(bool attack, bool attackHold, bool secondary, bool secondaryHold, bool block, bool blockHold) =>
            (attack ? 1 : 0) | (attackHold ? 2 : 0) | (secondary ? 4 : 0)
            | (secondaryHold ? 8 : 0) | (block ? 16 : 0) | (blockHold ? 32 : 0);

        /// <summary>Names an Animator state tag hash: attack, emote, dodge, idle or other.</summary>
        public static string TagName(int tag, int attack, int emote, int dodge, int idle)
        {
            if (tag == attack) return "attack";
            if (tag == emote) return "emote";
            if (tag == dodge) return "dodge";
            if (tag == idle) return "idle";
            return "other";
        }

        private static string Token(string? v) =>
            string.IsNullOrEmpty(v) ? "-" : v!.Replace(' ', '_');
    }
}
