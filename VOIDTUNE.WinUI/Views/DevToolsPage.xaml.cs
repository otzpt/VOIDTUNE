using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;
using VOIDTUNE.WinUI.Models;
using VOIDTUNE.WinUI.Services;

namespace VOIDTUNE.WinUI.Views;

public sealed partial class DevToolsPage : Page
{
    private readonly TweakEngine _engine = TweakEngine.Instance;
    private bool _probed;

    /// <summary>Blocks in the Tweak Lab builder (bound to the ItemsControl via x:Bind).</summary>
    public ObservableCollection<TweakBlock> Blocks { get; } = new();

    public DevToolsPage()
    {
        this.InitializeComponent();
    }

    // ── live apply/revert log (surfaces TweakEngine.Log, which nothing else listens to) ──
    protected override void OnNavigatedTo(NavigationEventArgs e)
    {
        _engine.Log += OnEngineLog;
        if (!_probed) { _probed = true; _ = RunProbe(); }
    }

    protected override void OnNavigatedFrom(NavigationEventArgs e) => _engine.Log -= OnEngineLog;

    private void OnEngineLog(string line)
    {
        // Log fires from whatever thread the apply resumes on — marshal to the UI thread.
        DispatcherQueue.TryEnqueue(() =>
        {
            if (LogText.Text.StartsWith("Waiting", StringComparison.Ordinal)) LogText.Text = "";
            LogText.Text += $"{DateTime.Now:HH:mm:ss}  {line}\n";
        });
    }

    // ── System Probe ──────────────────────────────────────────────────────────────────
    private async void RunProbe_Click(object sender, RoutedEventArgs e) => await RunProbe();

    private async System.Threading.Tasks.Task RunProbe()
    {
        RunProbeBtn.IsEnabled = false;
        ProbeBusy.IsActive = true;
        ProbeSummary.Text = "Probing…";
        try
        {
            var items = await DevToolsService.ProbeAsync();
            ProbeList.ItemsSource = items;
            int optimized = 0;
            foreach (var i in items) if (i.State == "OPTIMIZED") optimized++;
            ProbeSummary.Text = $"{optimized} / {items.Count} in optimized state";
        }
        catch (Exception ex)
        {
            ProbeSummary.Text = "Probe failed: " + ex.Message;
        }
        finally
        {
            ProbeBusy.IsActive = false;
            RunProbeBtn.IsEnabled = true;
        }
    }

    // ── Console ───────────────────────────────────────────────────────────────────────
    private async void RunCmd_Click(object sender, RoutedEventArgs e)
    {
        string body = CmdBox.Text;
        if (string.IsNullOrWhiteSpace(body)) { OutputText.Text = "(nothing to run)"; return; }

        bool powershell = ShellBox.SelectedIndex == 0;
        RunCmdBtn.IsEnabled = false;
        CmdBusy.IsActive = true;
        OutputText.Text = $"$ {body.Trim()}\n\nRunning…";

        var r = await CommandRunner.RunScriptAsync(body, powershell);

        CmdBusy.IsActive = false;
        RunCmdBtn.IsEnabled = true;
        string status = r.Ok ? "exit 0" : "non-zero exit";
        string output = string.IsNullOrWhiteSpace(r.Output) ? "(no output)" : r.Output;
        OutputText.Text = $"$ {body.Trim()}\n\n{output}\n\n[{status}]";
    }

    private void ClearConsole_Click(object sender, RoutedEventArgs e)
    {
        CmdBox.Text = "";
        OutputText.Text = "Output appears here.";
    }

    // ── Tweak Lab (block builder) ───────────────────────────────────────────────────────
    private void AddReg_Click(object sender, RoutedEventArgs e) => AddBlock(new TweakBlock
    {
        Kind = "reg", Title = "Registry value", AccentHex = "#a78bfa",
        Label1 = "Key", Ph1 = @"HKLM\SOFTWARE\...",
        Label2 = "Value name", Ph2 = "ValueName", Has2 = true,
        Label3 = "Data", Ph3 = "1  (number = DWORD, text = string)", Has3 = true,
    });

    private void AddSvc_Click(object sender, RoutedEventArgs e) => AddBlock(new TweakBlock
    {
        Kind = "svc", Title = "Disable a service", AccentHex = "#f59e0b",
        Label1 = "Service name", Ph1 = "e.g. DiagTrack",
    });

    private void AddCmd_Click(object sender, RoutedEventArgs e) => AddBlock(new TweakBlock
    {
        Kind = "cmd", Title = "Run a command", AccentHex = "#22c55e",
        Label1 = "Apply command", Ph1 = "e.g. netsh int tcp set global rss=enabled",
        Label2 = "Revert command (optional)", Ph2 = "e.g. netsh int tcp set global rss=disabled", Has2 = true,
    });

    private void AddBlock(TweakBlock b) { Blocks.Add(b); UpdateBlockEmpty(); }

    private void RemoveBlock_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button { Tag: TweakBlock b }) { Blocks.Remove(b); UpdateBlockEmpty(); }
    }

    private void UpdateBlockEmpty()
        => BlockEmpty.Visibility = Blocks.Count == 0 ? Visibility.Visible : Visibility.Collapsed;

    private (string apply, string revert) Compose()
    {
        var applies = new List<string>();
        var reverts = new List<string>();
        foreach (var b in Blocks)
        {
            switch (b.Kind)
            {
                case "reg":
                    string key = b.V1.Trim(), nm = b.V2.Trim(), da = b.V3.Trim();
                    if (key.Length == 0 || nm.Length == 0) continue;
                    string type = long.TryParse(da, out _) ? "REG_DWORD" : "REG_SZ";
                    string qn = nm.Contains(' ') ? $"\"{nm}\"" : nm;
                    applies.Add($"reg add \"{key}\" /v {qn} /t {type} /d {da} /f");
                    reverts.Add($"reg delete \"{key}\" /v {qn} /f 2>nul");
                    break;
                case "svc":
                    string sv = b.V1.Trim();
                    if (sv.Length == 0) continue;
                    applies.Add($"sc config {sv} start= disabled & sc stop {sv}");
                    reverts.Add($"sc config {sv} start= demand & sc start {sv}");
                    break;
                case "cmd":
                    if (b.V1.Trim().Length == 0) continue;
                    applies.Add(b.V1.Trim());
                    if (b.V2.Trim().Length > 0) reverts.Add(b.V2.Trim());
                    break;
            }
        }
        string apply = applies.Count > 0 ? string.Join(" & ", applies) + " & exit /b 0" : "";
        string revert = reverts.Count > 0 ? string.Join(" & ", reverts) + " & exit /b 0" : "";
        return (apply, revert);
    }

    private void Preview_Click(object sender, RoutedEventArgs e)
    {
        var (apply, revert) = Compose();
        PreviewText.Text = apply.Length == 0
            ? "Add at least one block and fill its fields."
            : $"APPLY:\n{apply}\n\nREVERT:\n{(revert.Length == 0 ? "(none)" : revert)}";
    }

    private void SaveTweak_Click(object sender, RoutedEventArgs e)
    {
        string name = LabName.Text.Trim();
        var (apply, revert) = Compose();
        if (name.Length == 0 || apply.Length == 0) { PreviewText.Text = "Give the tweak a name and at least one filled block first."; return; }
        var tier = LabTier.SelectedIndex switch { 1 => TweakTier.Extreme, 2 => TweakTier.Nuclear, _ => TweakTier.Safe };
        _engine.Tweaks.Add(new Tweak
        {
            Id = "custom_" + Guid.NewGuid().ToString("N")[..6],
            Name = name,
            Category = string.IsNullOrWhiteSpace(LabCategory.Text) ? "Custom" : LabCategory.Text.Trim(),
            Tier = tier,
            Description = "Custom tweak built in DevTools.",
            ApplyCmd = apply,
            RevertCmd = revert,
        });
        PreviewText.Text = $"Added \"{name}\" to your Tweaks list (this session). Submit it for approval to ship it to everyone.";
    }

    private void Submit_Click(object sender, RoutedEventArgs e)
    {
        string name = LabName.Text.Trim();
        var (apply, revert) = Compose();
        if (name.Length == 0 || apply.Length == 0) { PreviewText.Text = "Give the tweak a name and at least one filled block first."; return; }
        string cat = string.IsNullOrWhiteSpace(LabCategory.Text) ? "Custom" : LabCategory.Text.Trim();
        string tier = (LabTier.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "Safe";
        string body = $"### Proposed tweak\n\n**Name:** {name}\n**Category:** {cat}\n**Tier:** {tier}\n\n```\nApply:  {apply}\nRevert: {revert}\n```\n\n_Submitted from the VOIDTUNE DevTools Tweak Builder._";
        string url = "https://github.com/otzpt/VOIDTUNE/issues/new"
            + "?title=" + Uri.EscapeDataString("Tweak: " + name)
            + "&body=" + Uri.EscapeDataString(body)
            + "&labels=community-tweak";
        try { Process.Start(new ProcessStartInfo(url) { UseShellExecute = true }); } catch { /* ignore */ }
        PreviewText.Text = "Opened a pre-filled GitHub issue in your browser — review and submit it there for approval.";
    }
}
