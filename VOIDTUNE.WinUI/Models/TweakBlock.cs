using CommunityToolkit.Mvvm.ComponentModel;

namespace VOIDTUNE.WinUI.Models;

/// <summary>
/// One "block" in the DevTools visual Tweak Builder. A tweak is a stack of these blocks;
/// each compiles to an apply + revert command. Uniform shape so a single template renders all kinds.
/// </summary>
public partial class TweakBlock : ObservableObject
{
    public string Kind { get; set; } = "reg";        // reg | svc | cmd
    public string Title { get; set; } = "";
    public string AccentHex { get; set; } = "#a78bfa";

    public string Label1 { get; set; } = "";
    public string Label2 { get; set; } = "";
    public string Label3 { get; set; } = "";
    public string Ph1 { get; set; } = "";
    public string Ph2 { get; set; } = "";
    public string Ph3 { get; set; } = "";
    public bool Has2 { get; set; }
    public bool Has3 { get; set; }

    [ObservableProperty] private string _v1 = "";
    [ObservableProperty] private string _v2 = "";
    [ObservableProperty] private string _v3 = "";
}
