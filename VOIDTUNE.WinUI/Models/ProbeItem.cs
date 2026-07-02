namespace VOIDTUNE.WinUI.Models;

/// <summary>One row in the DevTools System Probe — a read-only snapshot of a system setting.</summary>
public sealed class ProbeItem
{
    public string Category { get; init; } = "";
    public string Name { get; init; } = "";
    public string Value { get; init; } = "";
    public string State { get; init; } = "";        // OPTIMIZED | DEFAULT | INFO
    public string StateHex { get; init; } = "#9a93a8";
}
