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
            AttackKind kind, int chainLevel, int chainLevels, ClipTiming timing,
            int damageEventsSeen, float stateSpeed, CancelSettings s)
        {
            if (!s.Enabled) return new CancelDecision(false, "disabled");
            if (kind == AttackKind.None) return new CancelDecision(false, "not_handled");

            bool on;
            switch (kind)
            {
                case AttackKind.SwordCombo: on = s.SwordCombo; break;
                case AttackKind.SwordSecondary: on = s.SwordSecondary; break;
                case AttackKind.AxesSecondary: on = s.AxesSecondary; break;
                default: on = true; break;
            }
            if (!on) return new CancelDecision(false, "toggle_off");

            if (kind == AttackKind.AxesCombo
                && s.AxesCombo == AxesComboMode.FinisherOnly
                && chainLevel != chainLevels - 1)
                return new CancelDecision(false, "finisher_only");

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

        public static CancelDecision ShouldCancelDodge(CancelSettings s)
        {
            if (!s.Enabled) return new CancelDecision(false, "disabled");
            if (!s.Dodge) return new CancelDecision(false, "toggle_off");
            return new CancelDecision(true, "ok");
        }
    }
}
