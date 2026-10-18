namespace AnimCancel.Core
{
    public enum AxesComboMode { FinisherOnly, MiddleAndFinisher }

    public enum CancelEmote { NoNoNo, Cheer }

    /// <summary>Max combo: Cycle cancels after swing_sword 2; Loop alternates swing_sword 1 and 2 while the secondary is held.</summary>
    public enum MaxComboMode { Cycle, Loop }

    public sealed record CancelSettings(
        bool Enabled,
        bool SwordCombo,
        bool SwordSecondary,
        AxesComboMode AxesCombo,
        bool AxesSecondary,
        bool Dodge,
        CancelEmote Emote,
        float DelayAfterHit,
        bool DebugLog,
        bool MaxCombo = false,
        MaxComboMode MaxComboMode = MaxComboMode.Cycle)
    {
        public static CancelSettings Default { get; } = new CancelSettings(
            true, true, true, AxesComboMode.MiddleAndFinisher, false, true, CancelEmote.NoNoNo, 0f, false);
    }
}
