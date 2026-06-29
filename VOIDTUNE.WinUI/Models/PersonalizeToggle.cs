using CommunityToolkit.Mvvm.ComponentModel;

namespace VOIDTUNE.WinUI.Models;

/// <summary>A personalization on/off setting backed by the registry (ported from personalize.ps1).</summary>
public partial class PersonalizeToggle : ObservableObject
{
    public string Id { get; init; } = "";
    public string Name { get; init; } = "";
    public string Description { get; init; } = "";
    public string Group { get; init; } = "";

    [ObservableProperty] private bool _enabled;
}
