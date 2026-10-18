using AnimCancel.Core;
using BepInEx.Configuration;
using UnityEngine;

namespace AnimCancel;

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
    private static ConfigEntry<bool> _maxCombo = null!;
    private static ConfigEntry<MaxComboMode> _maxComboMode = null!;
    private static ConfigEntry<int> _maxComboLoopCap = null!;
    private static ConfigEntry<AxesComboMode> _axesCombo = null!;
    private static ConfigEntry<bool> _axesSecondary = null!;
    private static ConfigEntry<bool> _dodge = null!;
    private static ConfigEntry<CancelEmote> _emote = null!;
    private static ConfigEntry<float> _delayAfterHit = null!;
    private static ConfigEntry<bool> _debugLog = null!;
    private static ConfigEntry<bool> _comboLab = null!;

    /// <summary>Combo lab log is on. Kept out of the snapshot: it is measurement only and works with Enabled off.</summary>
    internal static bool ComboLabLog { get; private set; }

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
        _maxCombo = config.Bind("Sword", "Max combo (auto)", d.MaxCombo,
            Describe("While the secondary attack plays, chain the second and third primary swings into it, then cancel or loop (see Max combo mode). Works even with Cancel secondary off. The final cancel honours Delay after hit. Releasing the button takes effect at the next hit, so one more swing may play. Needs a sword whose primary is the three-swing combo. Off by default.", 85));
        _maxComboMode = config.Bind("Sword", "Max combo mode", d.MaxComboMode,
            Describe("Used when Max combo (auto) is on. Cycle: secondary, second swing, third swing, then cancel (every relaunched secondary costs stamina). Loop: after the third swing, keep alternating the second and third swings while the secondary button is held; only the first secondary costs stamina. Releasing the button cancels at the next hit (one more swing may play). After Max combo loop cap extra swings a still-held button starts a new secondary, which costs stamina again.", 84));
        _maxComboLoopCap = config.Bind("Sword", "Max combo loop cap", d.MaxComboLoopCap,
            new ConfigDescription(
                "Max combo mode Loop only: how many extra swings are chained past the first pass before the run ends with a cancel, from 0 to 1000. A still-held button then starts a new secondary, which costs stamina again. 0 means no cap: the loop goes on until the button is released or the run is interrupted.",
                new AcceptableValueRange<int>(MaxComboPolicy.MinLoopCap, MaxComboPolicy.MaxLoopCap),
                new ConfigurationManagerAttributes { Order = 83 }));

        _axesCombo = config.Bind("Berserker axes", "Combo blows to cancel", d.AxesCombo,
            Describe("Which blows of the Berserker axes primary combo are cancelled: only the last, or the middle ones as well.", 100));
        _axesSecondary = config.Bind("Berserker axes", "Cancel secondary (Cleave)", d.AxesSecondary,
            Describe("Cut the recovery after the Berserker axes secondary attack, Cleave.", 90));

        _dodge = config.Bind("Dodge", "Cancel after invincibility", d.Dodge,
            Describe("End the dodge roll as soon as its invincibility window is over.", 100));

        _emote = config.Bind("Advanced", "Cancel emote", d.Emote,
            Describe("The emote the game plays under the hood to cut an animation short, NoNoNo or Cheer.", 100, advanced: true));
        _delayAfterHit = config.Bind("Advanced", "Delay after hit (s)", d.DelayAfterHit,
            new ConfigDescription(
                "Seconds to wait after the hit lands before cancelling, from 0 to 0.3.",
                new AcceptableValueRange<float>(0f, 0.3f),
                new ConfigurationManagerAttributes { Order = 90, IsAdvanced = true }));
        _debugLog = config.Bind("Advanced", "Debug log", d.DebugLog,
            Describe("Log the timestamp of every damage event and cancel, for measuring DPS. Off: the mod writes nothing to the log apart from its startup line and errors. Off by default.", 80, advanced: true));
        _comboLab = config.Bind("Advanced", "Combo lab log", LogGate.ComboLabLogDefault,
            Describe("Measurement only: log inputs, attack starts, animator states, blocking and hits of the local player (CL lines), even with Enabled off. Needs Debug log on as well. Off by default.", 70, advanced: true));

        config.SettingChanged += (_, __) =>
        {
            Rebuild();
            LogConfig();
        };
        Rebuild();
    }

    /// <summary>
    /// One ev=config line (AC with Debug log, CL with Debug log and Combo lab log) so log analyses can label runs
    /// by mode. Called at startup once the logger is set, and on every SettingChanged.
    /// </summary>
    internal static void LogConfig()
    {
        try
        {
            if (ReviewLog.Log == null)
            {
                return;
            }

            (string k, string v)[] kv = MaxComboPolicy.ConfigFields(_snapshot);
            if (ReviewLog.On)
            {
                ReviewLog.Write("config", kv);
            }

            if (ComboLab.On)
            {
                ComboLab.Write("config", kv);
            }
        }
        catch (System.Exception e)
        {
            if (!_configReported)
            {
                _configReported = true;
                ReviewLog.Log?.LogError($"{nameof(Settings)}.{nameof(LogConfig)}: {e}");
            }
        }
    }

    private static bool _configReported;

    private static ConfigDescription Describe(string text, int order, bool advanced = false) =>
        new ConfigDescription(text, null, new ConfigurationManagerAttributes
        {
            Order = order,
            IsAdvanced = advanced ? true : (bool?)null,
        });

    private static void Rebuild()
    {
        ComboLabLog = _comboLab.Value;
        _snapshot = new CancelSettings(
            Enabled.Value,
            _swordCombo.Value,
            _swordSecondary.Value,
            _axesCombo.Value,
            _axesSecondary.Value,
            _dodge.Value,
            _emote.Value,
            _delayAfterHit.Value,
            _debugLog.Value,
            _maxCombo.Value,
            _maxComboMode.Value,
            _maxComboLoopCap.Value);
    }
}
