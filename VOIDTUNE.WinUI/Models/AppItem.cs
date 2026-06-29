using CommunityToolkit.Mvvm.ComponentModel;

namespace VOIDTUNE.WinUI.Models;

/// <summary>A winget-installable app. Mirrors the [AI] model from the PowerShell edition.</summary>
public partial class AppItem : ObservableObject
{
    public string Id { get; init; } = "";       // winget package id
    public string Name { get; init; } = "";
    public string Category { get; init; } = "";

    [ObservableProperty] private bool _selected;
    [ObservableProperty] private bool _installed;
    [ObservableProperty] private bool _busy;
    [ObservableProperty] private string _statusHex = "#3A3A3A";
    [ObservableProperty] private string _status = "";
}
