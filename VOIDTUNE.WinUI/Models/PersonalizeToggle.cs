using CommunityToolkit.Mvvm.ComponentModel;

namespace VOIDTUNE.WinUI.Models;

/// <summary>A personalization on/off setting backed by the registry (ported from personalize.ps1).</summary>
public partial class PersonalizeToggle : ObservableObject
{
    public string Id { get; set; } = "";
    public string Name { get; set; } = "";
    public string Description { get; set; } = "";
    public string Group { get; set; } = "";

    [ObservableProperty] private bool _enabled;

    /// <summary>Accent color for the mod card, chosen by group.</summary>
    public string GroupHex => Group switch
    {
        "Theme"    => "#a78bfa",
        "Aero"     => "#38bdf8",
        "Taskbar"  => "#f472b6",
        "Explorer" => "#22c55e",
        "Display"  => "#f59e0b",
        _          => "#9a93a8",
    };
}
