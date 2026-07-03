using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;
using VOIDTUNE.WinUI.Models;
using VOIDTUNE.WinUI.Services;

namespace VOIDTUNE.WinUI.Views;

public sealed partial class RestorePage : Page
{
    private readonly TweakEngine _engine = TweakEngine.Instance;

    public RestorePage()
    {
        this.InitializeComponent();
    }

    protected override void OnNavigatedTo(NavigationEventArgs e)
    {
        ResetList.ItemsSource = _engine.Tweaks.Where(t => t.Category == "Restore").ToList();
        UpdateApplied();
    }

    private void UpdateApplied()
    {
        int n = _engine.AppliedCount;
        AppliedLine.Text = n == 0
            ? "Nothing is applied right now — Windows is at its defaults."
            : $"{n} tweak{(n == 1 ? "" : "s")} currently applied. Rolls everything back to Windows defaults.";
        RevertAllBtn.IsEnabled = n > 0;
    }

    private async void RevertAll_Click(object sender, RoutedEventArgs e)
    {
        var applied = _engine.Tweaks.Where(t => t.Applied).ToList();
        if (applied.Count == 0) { Show("Nothing to revert.", InfoBarSeverity.Informational); return; }
        SetBusy(true, "reverting everything…");
        var (ok, fail) = await _engine.RevertAsync(applied);
        SetBusy(false);
        Show($"Reverted {ok} tweak{(ok == 1 ? "" : "s")}" + (fail > 0 ? $", {fail} needed a reboot or were already default." : "."),
             fail > 0 ? InfoBarSeverity.Warning : InfoBarSeverity.Success);
        UpdateApplied();
    }

    private async void Reset_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not Button { Tag: Tweak t }) return;
        SetBusy(true, $"running {t.Name}…");
        var (ok, fail) = await _engine.ApplyAsync(new[] { t }, backup: false);
        SetBusy(false);
        Show(fail > 0 ? $"{t.Name} couldn't complete." : $"{t.Name} done.",
             fail > 0 ? InfoBarSeverity.Warning : InfoBarSeverity.Success);
    }

    private async void RestorePoint_Click(object sender, RoutedEventArgs e)
    {
        SetBusy(true, "creating restore point (can take a minute)…");
        var r = await CommandRunner.ExecAsync(
            "PS:try { Enable-ComputerRestore -Drive 'C:\\' -ErrorAction SilentlyContinue; " +
            "Checkpoint-Computer -Description 'VOIDTUNE manual checkpoint' -RestorePointType MODIFY_SETTINGS } catch {}; exit 0");
        SetBusy(false);
        Show(r.Ok
            ? "Restore point requested. Windows may skip it if one was made in the last 24h."
            : "Couldn't create a restore point — System Protection may be off for C:.",
            r.Ok ? InfoBarSeverity.Success : InfoBarSeverity.Warning);
    }

    private void SetBusy(bool busy, string hint = "")
    {
        BusyRing.IsActive = busy;
        HintLine.Text = busy ? hint : "";
        RevertAllBtn.IsEnabled = !busy && _engine.AppliedCount > 0;
        RestorePointBtn.IsEnabled = !busy;
    }

    private void Show(string msg, InfoBarSeverity sev)
    {
        StatusBar.Message = msg;
        StatusBar.Severity = sev;
        StatusBar.IsOpen = true;
    }
}
