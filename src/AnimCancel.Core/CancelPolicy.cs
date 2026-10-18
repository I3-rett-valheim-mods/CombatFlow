namespace AnimCancel.Core
{
    public readonly struct CancelDecision
    {
        public bool Cancel { get; }
        public string Reason { get; }

        public CancelDecision(bool cancel, string reason)
        {
            Cancel = cancel;
            Reason = reason;
        }
    }

    public static class CancelPolicy
    {
        public static CancelDecision ShouldCancel(
            AttackKind kind, ClipTiming timing,
            int damageEventsSeen, float stateSpeed, CancelSettings s)
        {
            if (!s.Enabled) return new CancelDecision(false, "disabled");
            if (kind == AttackKind.None) return new CancelDecision(false, "not_handled");

            var times = timing.DamageEventTimes;
            if (times.Count == 0 || damageEventsSeen != times.Count)
                return new CancelDecision(false, "not_last_hit");

            if (timing.ChainTime is float chain)
            {
                float last = times[times.Count - 1];
                if (!(last + CancelEmotes.BlendSeconds(s.Emote) * stateSpeed < chain))
                    return new CancelDecision(false, "chain_faster");
            }
            return new CancelDecision(true, "ok");
        }

        /// <summary>
        /// At the due time of a delayed cancel: fire only if the same attack is still
        /// current, the player is not staggering and is still in the attack state.
        /// </summary>
        public static CancelDecision ShouldFireDelayed(bool enabled, bool sameAttack, bool staggering, bool inAttack)
        {
            if (!enabled) return new CancelDecision(false, "disabled");
            if (!sameAttack || staggering || !inAttack) return new CancelDecision(false, "superseded");
            return new CancelDecision(true, "ok");
        }

        /// <summary>
        /// Immediate cancel (no delay): a stagger set in the same frame would win over the
        /// emote trigger and leave it latched, to fire after the stagger. Refused as superseded.
        /// </summary>
        public static CancelDecision ShouldFireNow(CancelDecision d, bool staggering)
            => d.Cancel && staggering ? new CancelDecision(false, "superseded") : d;

        public static CancelDecision ShouldCancelDodge(CancelSettings s)
        {
            if (!s.Enabled) return new CancelDecision(false, "disabled");
            if (!s.Dodge) return new CancelDecision(false, "toggle_off");
            return new CancelDecision(true, "ok");
        }
    }
}
