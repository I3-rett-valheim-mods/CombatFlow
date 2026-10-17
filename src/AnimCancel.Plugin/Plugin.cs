using BepInEx;

namespace AnimCancel;

[BepInPlugin(PluginInfo.Guid, PluginInfo.Name, PluginInfo.Version)]
public sealed class Plugin : BaseUnityPlugin
{
    private void Awake()
    {
        Logger.LogInfo($"{PluginInfo.Name} {PluginInfo.Version} loaded");
    }
}
