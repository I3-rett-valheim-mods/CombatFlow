using System.Collections.Generic;
using System.Linq;

namespace CombatFlow.Core
{
    public sealed class ClipTiming
    {
        public IReadOnlyList<float> DamageEventTimes { get; }
        public float? ChainTime { get; }

        public ClipTiming(IReadOnlyList<float> damageEventTimes, float? chainTime)
        {
            DamageEventTimes = damageEventTimes;
            ChainTime = chainTime;
        }

        /// <summary>Damage = Hit or OnAttackTrigger; chain = first Chain. Other events are ignored.</summary>
        public static ClipTiming FromEvents(IEnumerable<(float time, string functionName)> events)
        {
            var damage = new List<float>();
            float? chain = null;
            foreach (var (time, fn) in events)
            {
                if (fn == "Hit" || fn == "OnAttackTrigger") damage.Add(time);
                else if (fn == "Chain" && (chain == null || time < chain)) chain = time;
            }
            damage.Sort();
            return new ClipTiming(damage, chain);
        }
    }
}
