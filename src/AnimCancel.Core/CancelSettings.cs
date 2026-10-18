namespace AnimCancel.Core
{
    public enum AxesComboMode { FinisherOnly, MiddleAndFinisher }

    public enum CancelEmote { NoNoNo, Cheer }

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
        bool MaxCombo = false)
    {
        public static CancelSettings Default { get; } = new CancelSettings(
            true, true, true, AxesComboMode.MiddleAndFinisher, false, true, CancelEmote.NoNoNo, 0f, false);
    }
}
