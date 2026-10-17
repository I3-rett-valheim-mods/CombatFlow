using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace AnimCancel.Core
{
    /// <summary>
    /// Builds one review-log line: <c>AC t=&lt;s, 3 dp&gt; f=&lt;frame&gt; ev=&lt;event&gt; key=value ...</c>.
    /// Culture-invariant, one token per value so the log stays machine-parseable.
    /// </summary>
    public static class DebugLine
    {
        public static string Format(double t, int frame, string ev, IReadOnlyList<(string k, string v)> kv)
        {
            var sb = new StringBuilder(64);
            sb.Append("AC t=").Append(t.ToString("0.000", CultureInfo.InvariantCulture))
              .Append(" f=").Append(frame.ToString(CultureInfo.InvariantCulture))
              .Append(" ev=").Append(ev);
            for (int i = 0; i < kv.Count; i++)
            {
                sb.Append(' ').Append(kv[i].k).Append('=').Append(Token(kv[i].v));
            }
            return sb.ToString();
        }

        /// <summary>A number with at most three decimals, invariant culture.</summary>
        public static string Num(double v) => v.ToString("0.###", CultureInfo.InvariantCulture);

        public static string Bool(bool b) => b ? "true" : "false";

        private static string Token(string? v) =>
            string.IsNullOrEmpty(v) ? "-" : v!.Replace(' ', '_');
    }
}
