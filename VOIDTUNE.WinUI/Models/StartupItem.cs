using CommunityToolkit.Mvvm.ComponentModel;

namespace VOIDTUNE.WinUI.Models;

/// <summary>A startup program (registry Run keys or Startup folder).</summary>
public partial class StartupItem : ObservableObject
{
    public string Name { get; init; } = "";
    public string Command { get; set; } = "";       // updated in place when a folder entry moves
    public string Scope { get; init; } = "";         // HKCU | HKLM | Folder

    [ObservableProperty] private string _location = ""; // human-readable source (updates live)
    [ObservableProperty] private bool _enabled;
    [ObservableProperty] private bool _busy;

    /// <summary>
    /// Last state actually committed to the system. Lets the page tell a real user toggle
    /// apart from the echo the TwoWay IsOn binding raises when a container is (re)realized —
    /// which is what caused the toggle loop/crash.
    /// </summary>
    public bool Committed { get; set; }
}
