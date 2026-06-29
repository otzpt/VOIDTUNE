using CommunityToolkit.Mvvm.ComponentModel;

namespace VOIDTUNE.WinUI.Models;

public enum TweakTier
{
    Safe,
    Extreme,
    Nuclear
}

/// <summary>A single reversible system tweak. Mirrors the [TI] model from the PowerShell edition.</summary>
public partial class Tweak : ObservableObject
{
    public string Id { get; init; } = "";
    public string Category { get; init; } = "";
    public string Name { get; init; } = "";
    public string Description { get; init; } = "";
    public TweakTier Tier { get; init; } = TweakTier.Safe;

    /// <summary>Command to apply. Prefix "PS:" routes to PowerShell; otherwise cmd.exe.</summary>
    public string ApplyCmd { get; init; } = "";
    public string RevertCmd { get; init; } = "";

    [ObservableProperty] private bool _applied;
    [ObservableProperty] private bool _selected;

    public string TierLabel => Tier switch
    {
        TweakTier.Safe => "SAFE",
        TweakTier.Extreme => "EXTREME",
        TweakTier.Nuclear => "NUCLEAR",
        _ => ""
    };

    public string TierHex => Tier switch
    {
        TweakTier.Safe => "#22C55E",
        TweakTier.Extreme => "#A855F7",
        TweakTier.Nuclear => "#EF4444",
        _ => "#888888"
    };
}
