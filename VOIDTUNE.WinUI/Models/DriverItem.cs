namespace VOIDTUNE.WinUI.Models;

/// <summary>An installed device driver row (from Win32_PnPSignedDriver).</summary>
public sealed class DriverItem
{
    public string Name { get; init; } = "";
    public string Version { get; init; } = "";
    public string Date { get; init; } = "";
    public string Mfg { get; init; } = "";
    public string Category { get; init; } = "";
}
