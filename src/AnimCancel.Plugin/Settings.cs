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

    private static ConfigEntry<bool> _maxCombo = null!;
    private static ConfigEntry<MaxComboMode> _maxComboMode = null!;
    private static ConfigEntry<int> _maxComboLoopCap = null!;
    private static ConfigEntry<bool> _swapButtons = null!;
    private static ConfigEntry<bool> _axesMaxCombo = null!;
    private static ConfigEntry<bool> _dodge = null!;
    private static ConfigEntry<bool> _autoParry = null!;
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

        _maxCombo = config.Bind("Sword", "Max combo (auto)", d.MaxCombo,
            Describe("While the secondary attack plays, chain the second and third primary swings into it, then cancel or loop (see Max combo mode). The final cancel honours Delay after hit. Releasing the button takes effect at the next hit, so one more swing may play. Needs a sword whose primary is the three-swing combo. On by default.", 85));
        _maxComboMode = config.Bind("Sword", "Max combo mode", d.MaxComboMode,
            Describe("Used by Max combo (auto) of the sword and of the Berserker axes. Sword, Cycle: secondary, second swing, third swing, then cancel (every relaunched secondary costs stamina). Sword, Loop: after the third swing, keep alternating the second and third swings while the secondary button is held; only the first secondary costs stamina. Berserker axes: the same with the combo finisher and the third and fourth blows, while the primary button is held. Releasing the button cancels at the end of the swing playing (one more swing may play). After Max combo loop cap extra swings a still-held button starts a new attack, which costs stamina again.", 84));
        _maxComboLoopCap = config.Bind("Sword", "Max combo loop cap", d.MaxComboLoopCap,
            new ConfigDescription(
                "Max combo mode Loop only, sword and Berserker axes: how many extra swings are chained past the first pass before the run ends with a cancel, from 0 to 1000. A still-held button then starts a new attack, which costs stamina again. 0 means no cap: the loop goes on until the button is released or the run is interrupted.",
                new AcceptableValueRange<int>(MaxComboPolicy.MinLoopCap, MaxComboPolicy.MaxLoopCap),
                new ConfigurationManagerAttributes { Order = 83 }));
        _swapButtons = config.Bind("Sword", "Swap attack buttons", d.SwapButtons,
            Describe("With a sword only, swap the primary and secondary attack buttons (press and hold), so the primary button starts the secondary and its Max combo loop and the secondary button the light combo. Block, dodge, jump and movement are unchanged; other weapons, tools and build mode are untouched. Works with a gamepad. Needs Enabled on. Off by default.", 82));

        _axesMaxCombo = config.Bind("Berserker axes", "Max combo (auto)", d.AxesMaxCombo,
            Describe("After the last hit of the primary combo finisher, chain the third and fourth blows into it, then cancel or loop, with Max combo mode and Max combo loop cap of the Sword section. The chained blows keep the finisher's bonus damage. Loop mode keeps going while the primary button is held; releasing it cancels at the end of the swing playing. On by default.", 85));

        _dodge = config.Bind("Dodge", "Cancel after invincibility", d.Dodge,
            Describe("End the dodge roll as soon as its invincibility window is over.", 100));

        _autoParry = config.Bind("Combat", "Auto perfect parry", d.AutoParry,
            Describe("A blockable hit from the front is parried as a perfect block, even mid-attack and without the block button. Blocks with the shield, weapon or bare hands, as the game does; hits from behind and during a dodge are unchanged. The game's own block rules apply (stamina, guard break, attacker stagger). Applies to every attacker, creatures and players. Needs Enabled on. Off by default.", 100));

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
            _dodge.Value,
            _emote.Value,
            _delayAfterHit.Value,
            _debugLog.Value,
            _maxCombo.Value,
            _maxComboMode.Value,
            _maxComboLoopCap.Value,
            _swapButtons.Value,
            _autoParry.Value,
            _axesMaxCombo.Value);
    }
}
