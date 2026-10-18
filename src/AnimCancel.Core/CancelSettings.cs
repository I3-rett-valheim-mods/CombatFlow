namespace AnimCancel.Core
{
    public enum AxesComboMode { FinisherOnly, MiddleAndFinisher }

    public enum CancelEmote { NoNoNo, Cheer }

    /// <summary>Max combo (sword and berserker axes): Cycle cancels after the second chained swing; Loop keeps
    /// alternating the two chained swings while the weapon's button is held.</summary>
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
        MaxComboMode MaxComboMode = MaxComboMode.Cycle,
        int MaxComboLoopCap = MaxComboPolicy.DefaultLoopCap,
        bool SwapButtons = false,
        bool AutoParry = false,
        bool AxesMaxCombo = false)
    {
        public static CancelSettings Default { get; } = new CancelSettings(
            true, true, true, AxesComboMode.MiddleAndFinisher, false, true, CancelEmote.NoNoNo, 0f, false,
            MaxCombo: true, MaxComboMode: MaxComboMode.Loop, MaxComboLoopCap: 0, SwapButtons: false, AutoParry: false,
            AxesMaxCombo: true);
    }
}
