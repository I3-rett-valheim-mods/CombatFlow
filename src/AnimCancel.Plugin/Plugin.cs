using AnimCancel.Core;
using BepInEx;
using HarmonyLib;

namespace AnimCancel;

[BepInPlugin(PluginInfo.Guid, PluginInfo.Name, PluginInfo.Version)]
public sealed class Plugin : BaseUnityPlugin
{
    /// <summary>Current settings, rebuilt whenever a config entry changes.</summary>
    public static CancelSettings Current => Settings.Snapshot();

    private Harmony? _harmony;

    private void Awake()
    {
        Settings.Bind(Config);
        ReviewLog.Log = Logger;
        _harmony = new Harmony(PluginInfo.Guid);
        _harmony.PatchAll();
        Logger.LogInfo($"{PluginInfo.Name} {PluginInfo.Version} loaded");
        Settings.LogConfig();
    }

    private void OnDestroy() => _harmony?.UnpatchSelf();

    private void OnGUI() => InputGate.SampleGuiFocus();

    private void Update()
    {
        AttackTracker.Tick();
        ComboLab.Tick();

        if (!Settings.ToggleKey.Value.IsDown() || !InputGate.AcceptsHotkey())
        {
            return;
        }

        // Changing the entry persists it and rebuilds the snapshot via SettingChanged.
        Settings.Enabled.Value = !Settings.Enabled.Value;
        string state = Settings.Enabled.Value ? "ON" : "OFF";
        if (ReviewLog.On)
        {
            ReviewLog.Write("toggle", ("enabled", DebugLine.Bool(Settings.Enabled.Value)));
        }
        else
        {
            Logger.LogInfo($"AnimCancel {state}");
        }

        if (MessageHud.instance != null)
        {
            MessageHud.instance.ShowMessage(MessageHud.MessageType.TopLeft, $"AnimCancel {state}");
        }
    }
}
