namespace CombatFlow.Core
{
    /// <summary>The four attack arguments of Player.SetControls.</summary>
    public readonly struct AttackInputs : System.IEquatable<AttackInputs>
    {
        public bool Attack { get; }
        public bool AttackHold { get; }
        public bool Secondary { get; }
        public bool SecondaryHold { get; }

        public AttackInputs(bool attack, bool attackHold, bool secondary, bool secondaryHold)
        {
            Attack = attack;
            AttackHold = attackHold;
            Secondary = secondary;
            SecondaryHold = secondaryHold;
        }

        /// <summary>Primary and secondary exchanged: press with press, hold with hold.</summary>
        public AttackInputs Swapped() => new(Secondary, SecondaryHold, Attack, AttackHold);

        public bool Equals(AttackInputs o) => Attack == o.Attack && AttackHold == o.AttackHold
                                              && Secondary == o.Secondary && SecondaryHold == o.SecondaryHold;

        public override bool Equals(object? obj) => obj is AttackInputs o && Equals(o);

        public override int GetHashCode() =>
            (Attack ? 1 : 0) | (AttackHold ? 2 : 0) | (Secondary ? 4 : 0) | (SecondaryHold ? 8 : 0);
    }

    /// <summary>
    /// Sword / Swap attack buttons: with a sword (the Max combo weapon) the primary and secondary
    /// attack inputs of the local player are exchanged before Player.SetControls reads them, so the
    /// left button starts the secondary (and its Max combo loop) and the right button the light combo.
    /// Block, dodge, jump and movement are not touched.
    /// </summary>
    public static class SwapButtons
    {
        /// <summary>The option is on and the mod is enabled (mod off means vanilla).</summary>
        public static bool IsOn(CancelSettings s) => s.Enabled && s.SwapButtons;

        /// <summary>The equipped weapon's secondary is sword_secondary and its primary the 3-level
        /// swing_longsword combo: the same weapons as <see cref="MaxComboPolicy.Eligible"/>.</summary>
        public static bool IsSword(string? secondaryAnimation, string? primaryAnimation, int primaryChainLevels)
            => MaxComboPolicy.Eligible(secondaryAnimation, primaryAnimation, primaryChainLevels);

        /// <summary><paramref name="enabled"/>: <see cref="IsOn"/>. Never in place (build) mode, where
        /// the buttons place and remove pieces.</summary>
        public static bool ShouldSwap(bool enabled, bool swordEquipped, bool inPlaceMode)
            => enabled && swordEquipped && !inPlaceMode;

        /// <summary>Extra keys of the combo lab input line: swapped=1 when the values logged are the
        /// swapped ones (the raw input is then the mirror image), nothing otherwise.</summary>
        public static (string k, string v)[] LabFields(bool swapped)
            => swapped ? new[] { ("swapped", "1") } : System.Array.Empty<(string, string)>();
    }
}
