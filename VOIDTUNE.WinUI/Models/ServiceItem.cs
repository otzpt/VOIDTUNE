using CommunityToolkit.Mvvm.ComponentModel;

namespace VOIDTUNE.WinUI.Models;

public partial class ServiceItem : ObservableObject
{
    public string Name { get; init; } = "";
    public string Description { get; init; } = "";

    [ObservableProperty] private string _status = "—";
    [ObservableProperty] private string _statusHex = "#888888";
}
