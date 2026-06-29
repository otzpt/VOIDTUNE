using CommunityToolkit.Mvvm.ComponentModel;

namespace VOIDTUNE.WinUI.Models;

/// <summary>A startup program (registry Run keys or Startup folder).</summary>
public partial class StartupItem : ObservableObject
{
    public string Name { get; init; } = "";
    public string Command { get; init; } = "";
    public string Location { get; init; } = "";   // human-readable source
    public string Scope { get; init; } = "";       // HKCU | HKLM | Folder

    [ObservableProperty] private bool _enabled;
    [ObservableProperty] private bool _busy;
}
