namespace CombatFlow.Core
{
    public static class AttackClassifier
    {
        public static AttackKind Classify(string? attackAnimation)
        {
            switch (attackAnimation)
            {
                case "swing_longsword": return AttackKind.SwordCombo;
                case "sword_secondary": return AttackKind.SwordSecondary;
                case "dualaxes": return AttackKind.AxesCombo;
                case "dualaxes_secondary": return AttackKind.AxesSecondary;
                default: return AttackKind.None;
            }
        }
    }
}
