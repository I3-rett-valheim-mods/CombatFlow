using AnimCancel.Core;
using BepInEx;

namespace AnimCancel;

[BepInPlugin(PluginInfo.Guid, PluginInfo.Name, PluginInfo.Version)]
public sealed class Plugin : BaseUnityPlugin
{
    /// <summary>Current settings, rebuilt whenever a config entry changes.</summary>
    public static CancelSettings Current => Settings.Snapshot();

    private void Awake()
    {
        Settings.Bind(Config);
        Logger.LogInfo($"{PluginInfo.Name} {PluginInfo.Version} loaded");
    }

    private void Update()
    {
        if (!Settings.ToggleKey.Value.IsDown())
        {
            return;
        }

        // Changing the entry persists it and rebuilds the snapshot via SettingChanged.
        Settings.Enabled.Value = !Settings.Enabled.Value;
        string state = Settings.Enabled.Value ? "ON" : "OFF";
        Logger.LogInfo($"AnimCancel {state}");

        if (MessageHud.instance != null)
        {
            MessageHud.instance.ShowMessage(MessageHud.MessageType.TopLeft, $"AnimCancel {state}");
        }
    }
}
