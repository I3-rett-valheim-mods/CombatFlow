using UnityEngine;

namespace CombatFlow;

/// <summary>
/// Decides whether the toggle key press is meant for us or for something the
/// player is typing into or driving (chat, console, menus, a keybind capture).
/// Ported from NearbyPanel, minus the build-mode check that does not matter for a
/// toggle.
/// </summary>
internal static class InputGate
{
    /// <summary>
    /// Whether an IMGUI control held keyboard focus the last time OnGUI ran.
    /// GUIUtility.keyboardControl is only meaningful inside OnGUI, so it is
    /// sampled there and read from Update.
    /// </summary>
    private static bool _imguiHasFocus;

    /// <summary>Call once at the top of OnGUI.</summary>
    public static void SampleGuiFocus() => _imguiHasFocus = GUIUtility.keyboardControl != 0;

    public static bool AcceptsHotkey() => Blocker() == null;

    /// <summary>The check swallowing the hotkey, or null when it should be accepted.</summary>
    public static string? Blocker()
    {
        if (!Player.m_localPlayerExists || Player.m_localPlayer == null)
        {
            return "no local player";
        }

        if (Chat.instance != null && Chat.instance.HasFocus())
        {
            return "chat has focus";
        }

        if (Console.IsVisible())
        {
            return "console is open";
        }

        if (TextInput.IsVisible())
        {
            return "a text prompt is open";
        }

        if (Menu.IsVisible())
        {
            return "the menu is open";
        }

        if (InventoryGui.IsVisible())
        {
            return "the inventory is open";
        }

        if (_imguiHasFocus)
        {
            return "an IMGUI text field has focus";
        }

        return null;
    }
}
