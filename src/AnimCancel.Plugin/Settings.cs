using AnimCancel.Core;
using BepInEx.Configuration;
using UnityEngine;

namespace AnimCancel
{
    /// <summary>
    /// Binds every config entry and keeps a <see cref="CancelSettings"/> snapshot in
    /// step with them. Defaults come from <see cref="CancelSettings.Default"/> so the
    /// design table lives in one place. Order counts down per section so a
    /// configuration manager lists entries as written here.
    /// </summary>
    internal static class Settings
    {
        private static CancelSettings _snapshot = CancelSettings.Default;

        internal static ConfigEntry<bool> Enabled { get; private set; } = null!;
        internal static ConfigEntry<KeyboardShortcut> ToggleKey { get; private set; } = null!;

        private static ConfigEntry<bool> _swordCombo = null!;
        private static ConfigEntry<bool> _swordSecondary = null!;
        private static ConfigEntry<AxesComboMode> _axesCombo = null!;
        private static ConfigEntry<bool> _axesSecondary = null!;
        private static ConfigEntry<bool> _dodge = null!;
        private static ConfigEntry<CancelEmote> _emote = null!;
        private static ConfigEntry<float> _delayAfterHit = null!;
        private static ConfigEntry<bool> _debugLog = null!;

        internal static CancelSettings Snapshot() => _snapshot;

        internal static void Bind(ConfigFile config)
        {
            CancelSettings d = CancelSettings.Default;

            Enabled = config.Bind("General", "Enabled", d.Enabled,
                Describe("Master switch for animation cancelling.", 100));
            ToggleKey = config.Bind("General", "Toggle key", new KeyboardShortcut(KeyCode.F8),
                Describe("Turns animation cancelling on or off in game and shows a short message.", 90));

            _swordCombo = config.Bind("Sword", "Cancel primary combo", d.SwordCombo,
                Describe("Cut the recovery after each blow of the sword primary combo.", 100));
            _swordSecondary = config.Bind("Sword", "Cancel secondary", d.SwordSecondary,
                Describe("Cut the recovery after the sword secondary attack.", 90));

            _axesCombo = config.Bind("Berserker axes", "Combo blows to cancel", d.AxesCombo,
                Describe("Which blows of the Berserker axes primary combo are cancelled: only the last, or the middle ones as well.", 100));
            _axesSecondary = config.Bind("Berserker axes", "Cancel secondary (Cleave)", d.AxesSecondary,
                Describe("Cut the recovery after the Berserker axes secondary attack, Cleave.", 90));

            _dodge = config.Bind("Dodge", "Cancel after invincibility", d.Dodge,
                Describe("End the dodge roll as soon as its invincibility window is over.", 100));

            _emote = config.Bind("Advanced", "Cancel emote", d.Emote,
                Describe("The emote the game plays under the hood to cut an animation short, nonono or cheer.", 100, advanced: true));
            _delayAfterHit = config.Bind("Advanced", "Delay after hit (s)", d.DelayAfterHit,
                new ConfigDescription(
                    "Seconds to wait after the hit lands before cancelling, from 0 to 0.3.",
                    new AcceptableValueRange<float>(0f, 0.3f),
                    new ConfigurationManagerAttributes { Order = 90, IsAdvanced = true }));
            _debugLog = config.Bind("Advanced", "Debug log", d.DebugLog,
                Describe("Log the timestamp of every damage event and cancel, for measuring DPS.", 80, advanced: true));

            config.SettingChanged += (_, __) => Rebuild();
            Rebuild();
        }

        private static ConfigDescription Describe(string text, int order, bool advanced = false) =>
            new ConfigDescription(text, null, new ConfigurationManagerAttributes
            {
                Order = order,
                IsAdvanced = advanced ? true : (bool?)null,
            });

        private static void Rebuild() =>
            _snapshot = new CancelSettings(
                Enabled.Value,
                _swordCombo.Value,
                _swordSecondary.Value,
                _axesCombo.Value,
                _axesSecondary.Value,
                _dodge.Value,
                _emote.Value,
                _delayAfterHit.Value,
                _debugLog.Value);
    }
}
