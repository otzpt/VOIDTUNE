using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;
using System.Threading.Tasks;
using VOIDTUNE.WinUI.Services;

namespace VOIDTUNE.WinUI.Views;

public sealed partial class ServicesPage : Page
{
    private readonly ServiceManager _mgr = new();

    public ServicesPage()
    {
        this.InitializeComponent();
        List.ItemsSource = _mgr.Services;
    }

    protected override void OnNavigatedTo(NavigationEventArgs e) => _mgr.Refresh();

    private void Refresh_Click(object sender, RoutedEventArgs e) => _mgr.Refresh();

    private async void Disable_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button { Tag: string name }) await Run(() => _mgr.SetAsync(name, false), $"{name} disabled.");
    }

    private async void Enable_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button { Tag: string name }) await Run(() => _mgr.SetAsync(name, true), $"{name} enabled.");
    }

    private async void Gaming_Click(object sender, RoutedEventArgs e)
        => await Run(() => _mgr.ApplyGamingProfileAsync(), "Gaming profile applied — non-essential services disabled.");

    private async void Normal_Click(object sender, RoutedEventArgs e)
        => await Run(() => _mgr.ApplyNormalProfileAsync(), "Normal profile applied — safe defaults restored.");

    private async Task Run(System.Func<Task> action, string okMessage)
    {
        BusyRing.IsActive = true;
        await action();
        BusyRing.IsActive = false;
        StatusBar.Message = okMessage;
        StatusBar.Severity = InfoBarSeverity.Success;
        StatusBar.IsOpen = true;
    }
}
