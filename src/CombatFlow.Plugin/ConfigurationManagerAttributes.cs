using System;
using System.Diagnostics.CodeAnalysis;
using BepInEx.Configuration;

/// <summary>
/// Metadata an in-game configuration manager reads off a setting: the order to
/// show it in, whether to hide it behind the advanced toggle, and so on.
///
/// Deliberately in the global namespace and deliberately not referencing the
/// manager. Configuration managers recognise this by **type name** and copy any
/// member whose name they know, so a mod can annotate its settings without taking
/// a dependency on any particular manager — or on the manager being installed at
/// all. Passed as a tag to <see cref="ConfigDescription"/>.
///
/// Field names and types match the shape those managers expect; do not rename
/// them. Members left null are simply not applied.
///
/// If this project ever references Jotunn, delete this file: Jotunn ships the same
/// class in the global namespace and the two would be ambiguous.
/// </summary>
[SuppressMessage("Style", "IDE1006", Justification = "Names are fixed by the managers that read them.")]
public sealed class ConfigurationManagerAttributes
{
    /// <summary>Higher sorts earlier within its section.</summary>
    public int? Order;

    /// <summary>Hide behind the manager's "advanced" toggle.</summary>
    public bool? IsAdvanced;

    /// <summary>Show in the list at all.</summary>
    public bool? Browsable;

    /// <summary>Show but do not allow editing.</summary>
    public bool? ReadOnly;

    /// <summary>Override the section this setting appears under.</summary>
    public string? Category;

    /// <summary>Override the label, which otherwise comes from the config key.</summary>
    public string? DispName;

    /// <summary>Override the description shown under the label.</summary>
    public string? Description;

    /// <summary>Draw a numeric range as a percentage rather than raw values.</summary>
    public bool? ShowRangeAsPercent;

    /// <summary>Hide the per-setting reset button.</summary>
    public bool? HideDefaultButton;

    /// <summary>Hide the setting's name, for a drawer that renders its own.</summary>
    public bool? HideSettingName;

    /// <summary>Draw the value yourself instead of using the default editor.</summary>
    public Action<ConfigEntryBase>? CustomDrawer;
}
