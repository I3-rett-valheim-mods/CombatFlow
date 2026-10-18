using System;

namespace AnimCancel.Core
{
    public enum ClipPickResult
    {
        Current,
        Next,
        Ambiguous,
    }

    /// <summary>
    /// At a damage event during a layer-0 blend, the event may come from either clip: the
    /// source (current) clip fires its Hit while blending into the next attack, or the next
    /// clip fires its own. The clip that fired is the one with a damage event within
    /// <see cref="Tolerance"/> seconds (clip time, wrapping) of its own clip time. Both or
    /// neither matching is ambiguous and falls back to the current clip.
    /// </summary>
    public static class ClipPick
    {
        public const float Tolerance = 0.05f;

        /// <param name="next">Timing of the next clip, null when not in transition.</param>
        public static ClipPickResult Choose(ClipTiming? current, float currentT, float currentLength,
            ClipTiming? next, float nextT, float nextLength)
        {
            if (next == null)
            {
                return ClipPickResult.Current;
            }

            if (current == null)
            {
                return ClipPickResult.Next;
            }

            bool c = HasDamageNear(current, currentT, currentLength);
            bool n = HasDamageNear(next, nextT, nextLength);
            if (c == n)
            {
                return ClipPickResult.Ambiguous;
            }

            return c ? ClipPickResult.Current : ClipPickResult.Next;
        }

        public static bool UseNext(ClipPickResult r) => r == ClipPickResult.Next;

        /// <summary>A damage event of <paramref name="t"/> within the tolerance of clip time <paramref name="clipT"/>.</summary>
        public static bool HasDamageNear(ClipTiming t, float clipT, float clipLength, float tolerance = Tolerance)
        {
            foreach (float e in t.DamageEventTimes)
            {
                float d = Math.Abs(e - clipT);
                if (clipLength > 0f)
                {
                    d %= clipLength;
                    d = Math.Min(d, clipLength - d);
                }

                // Float slack so an exact 50 ms reads as a match.
                if (d <= tolerance + 1e-4f)
                {
                    return true;
                }
            }

            return false;
        }

        public static string LogValue(ClipPickResult r) => r switch
        {
            ClipPickResult.Next => "next",
            ClipPickResult.Ambiguous => "ambiguous",
            _ => "current",
        };
    }
}
