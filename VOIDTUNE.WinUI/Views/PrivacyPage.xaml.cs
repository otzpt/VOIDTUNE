using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;
using System.Linq;
using VOIDTUNE.WinUI.Models;
using VOIDTUNE.WinUI.Services;

namespace VOIDTUNE.WinUI.Views;

public sealed partial class PrivacyPage : Page
{
    private readonly TweakEngine _engine = TweakEngine.Instance;

    public PrivacyPage()
    {
        this.InitializeComponent();
        List.ItemsSource = _engine.Tweaks.Where(t => t.Category == "Privacy").ToList();
    }

    private async void ApplyAll_Click(object sender, RoutedEventArgs e)
    {
        var safe = _engine.Tweaks.Where(t => t.Category == "Privacy" && t.Tier == TweakTier.Safe).ToList();
        BusyRing.IsActive = true;
        var (ok, fail) = await _engine.ApplyAsync(safe);
        BusyRing.IsActive = false;
        Show($"Applied {ok} privacy tweaks" + (fail > 0 ? $", {fail} failed." : "."),
             fail > 0 ? InfoBarSeverity.Warning : InfoBarSeverity.Success);
    }

    private async void RevertAll_Click(object sender, RoutedEventArgs e)
    {
        var privacy = _engine.Tweaks.Where(t => t.Category == "Privacy" && t.Applied).ToList();
        BusyRing.IsActive = true;
        var (ok, fail) = await _engine.RevertAsync(privacy);
        BusyRing.IsActive = false;
        Show($"Reverted {ok} privacy tweaks.", InfoBarSeverity.Informational);
    }

    private void Show(string msg, InfoBarSeverity sev)
    {
        StatusBar.Message = msg;
        StatusBar.Severity = sev;
        StatusBar.IsOpen = true;
    }
}
