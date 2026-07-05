using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Text.Json;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;
using VOIDTUNE.WinUI.Models;
using VOIDTUNE.WinUI.Services;
using Windows.ApplicationModel.DataTransfer;

namespace VOIDTUNE.WinUI.Views;

public sealed partial class DevToolsPage : Page
{
    private readonly TweakEngine _engine = TweakEngine.Instance;
    private readonly DispatcherTimer _procTimer = new() { Interval = TimeSpan.FromSeconds(3) };
    private bool _probed;
    private Dictionary<string, string>? _snapshotA;
    private string _snapshotRoot = "";
    private List<DiffRow> _lastDiff = new();
    private List<ProbeItem> _probeItems = new();

    /// <summary>Blocks in the Tweak Lab builder (bound to the ItemsControl via x:Bind).</summary>
    public ObservableCollection<TweakBlock> Blocks { get; } = new();

    public DevToolsPage()
    {
        this.InitializeComponent();
        _procTimer.Tick += async (_, _) => await SampleProcesses();
    }

    // ── live apply/revert log (surfaces TweakEngine.Log, which nothing else listens to) ──
    protected override void OnNavigatedTo(NavigationEventArgs e)
    {
        _engine.Log += OnEngineLog;
        if (!_probed) { _probed = true; _ = RunProbe(); _ = RefreshNet(); }
        // Processes is the first (default) tab, so its controls are realized now — kick it off.
        StartProcessMonitorIfSelected();
    }

    protected override void OnNavigatedFrom(NavigationEventArgs e)
    {
        _engine.Log -= OnEngineLog;
        _procTimer.Stop();
    }

    // ── DevTools pivot: start/stop the process monitor only while its tab is showing ──
    private void Pivot_Changed(object sender, SelectionChangedEventArgs e) => StartProcessMonitorIfSelected();

    private void StartProcessMonitorIfSelected()
    {
        bool onProcesses = (DevPivot?.SelectedItem as PivotItem)?.Header as string == "Processes";
        if (!onProcesses) { _procTimer.Stop(); return; }

        // The tab's controls can lag one layout pass behind SelectionChanged — defer so
        // ProcMonList/ProcAutoToggle are realized before we touch them (that was the "shows
        // nothing" bug: the handler ran before the tab's content existed).
        DispatcherQueue.TryEnqueue(() =>
        {
            if (ProcMonList == null) return;
            if ((ProcAutoToggle?.IsOn ?? true) && !_procTimer.IsEnabled) _procTimer.Start();
            _ = SampleProcesses();
        });
    }

    // ── Process Monitor ─────────────────────────────────────────────────────────────
    private bool _procSampling;

    private async void RefreshProcMon_Click(object sender, RoutedEventArgs e) => await SampleProcesses();

    private void ProcAuto_Toggled(object sender, RoutedEventArgs e)
    {
        if (ProcAutoToggle.IsOn) { _procTimer.Start(); _ = SampleProcesses(); }
        else _procTimer.Stop();
    }

    private async System.Threading.Tasks.Task SampleProcesses()
    {
        if (_procSampling) return;              // a sample takes ~0.5s; don't overlap
        _procSampling = true;
        ProcMonBusy.IsActive = true;
        try
        {
            var rows = await ProcessMonitorService.TopAsync();
            ProcMonList.ItemsSource = rows;
            ProcMonStatus.Text = $"{rows.Count} shown · sorted by impact · {DateTime.Now:HH:mm:ss}";
        }
        catch (Exception ex) { ProcMonStatus.Text = "sample failed: " + ex.Message; }
        finally { ProcMonBusy.IsActive = false; _procSampling = false; }
    }

    private void Kill_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not Button { Tag: ProcMonRow r }) return;
        var (ok, msg) = ProcessMonitorService.KillProcess(r.Pid, r.Name);
        ProcMonStatus.Text = msg;
        if (ok) _ = SampleProcesses();
    }

    private void OnEngineLog(string line)
    {
        DispatcherQueue.TryEnqueue(() =>
        {
            if (LogText.Text.StartsWith("Waiting", StringComparison.Ordinal)) LogText.Text = "";
            LogText.Text += $"{DateTime.Now:HH:mm:ss}  {line}\n";
        });
    }

    private static void CopyToClipboard(string text)
    {
        var dp = new DataPackage();
        dp.SetText(text);
        Clipboard.SetContent(dp);
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
            _probeItems = await DevToolsService.ProbeAsync();
            ProbeList.ItemsSource = _probeItems;
            int optimized = _probeItems.Count(i => i.State == "OPTIMIZED");
            ProbeSummary.Text = $"{optimized} / {_probeItems.Count} in optimized state";
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

    private void CopyProbe_Click(object sender, RoutedEventArgs e)
    {
        if (_probeItems.Count == 0) { ProbeSummary.Text = "Run the probe first."; return; }
        var sb = new StringBuilder("VOIDTUNE System Probe · " + DateTime.Now.ToString("yyyy-MM-dd HH:mm") + "\n");
        foreach (var i in _probeItems) sb.AppendLine($"[{i.State}] {i.Category} · {i.Name} = {i.Value}");
        CopyToClipboard(sb.ToString());
        ProbeSummary.Text = "Report copied to clipboard.";
    }

    // ── Console ───────────────────────────────────────────────────────────────────────
    private void Preset_Changed(object sender, SelectionChangedEventArgs e)
    {
        (string cmd, bool ps) = PresetBox.SelectedIndex switch
        {
            0 => ("Get-Process | Sort-Object CPU -Descending | Select-Object -First 15 Name, @{N='CPU(s)';E={[math]::Round($_.CPU,1)}}, @{N='RAM(MB)';E={[math]::Round($_.WS/1MB)}} | Format-Table -AutoSize", true),
            1 => ("Get-Process | Sort-Object WS -Descending | Select-Object -First 15 Name, @{N='RAM(MB)';E={[math]::Round($_.WS/1MB)}} | Format-Table -AutoSize", true),
            2 => ("netsh int tcp show global", false),
            3 => ("Get-CimInstance Win32_StartupCommand | Select-Object Name, Location, Command | Format-List", true),
            4 => ("taskkill /f /im explorer.exe & start explorer.exe", false),
            5 => ("sfc /scannow", false),
            _ => ("", true),
        };
        if (cmd.Length == 0) return;
        CmdBox.Text = cmd;
        ShellBox.SelectedIndex = ps ? 0 : 1;
    }

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

    private void CopyOutput_Click(object sender, RoutedEventArgs e) => CopyToClipboard(OutputText.Text);

    private void ClearConsole_Click(object sender, RoutedEventArgs e)
    {
        CmdBox.Text = "";
        OutputText.Text = "Output appears here.";
    }

    // ── Reg Diff ──────────────────────────────────────────────────────────────────────
    private async void SnapA_Click(object sender, RoutedEventArgs e)
    {
        SnapABtn.IsEnabled = false;
        DiffBusy.IsActive = true;
        DiffStatus.Text = "Snapshotting…";
        try
        {
            _snapshotRoot = DiffScope.Text.Trim();
            _snapshotA = await RegDiffService.SnapshotAsync(_snapshotRoot);
            DiffStatus.Text = $"Snapshot A: {_snapshotA.Count:N0} values under {_snapshotRoot}. Now change something, then Diff.";
            DiffBtn.IsEnabled = true;
        }
        catch (Exception ex)
        {
            DiffStatus.Text = "Snapshot failed: " + ex.Message;
            _snapshotA = null;
            DiffBtn.IsEnabled = false;
        }
        finally
        {
            DiffBusy.IsActive = false;
            SnapABtn.IsEnabled = true;
        }
    }

    private async void DiffNow_Click(object sender, RoutedEventArgs e)
    {
        if (_snapshotA is null) return;
        DiffBtn.IsEnabled = false;
        DiffBusy.IsActive = true;
        DiffStatus.Text = "Snapshotting B + diffing…";
        try
        {
            var b = await RegDiffService.SnapshotAsync(_snapshotRoot);
            _lastDiff = RegDiffService.Diff(_snapshotA, b);
            DiffList.ItemsSource = _lastDiff;
            CopyDiffBtn.IsEnabled = _lastDiff.Count > 0;
            DiffStatus.Text = _lastDiff.Count == 0
                ? "No changes detected in this scope."
                : $"{_lastDiff.Count} change(s). A stays armed — change more and diff again.";
        }
        catch (Exception ex)
        {
            DiffStatus.Text = "Diff failed: " + ex.Message;
        }
        finally
        {
            DiffBusy.IsActive = false;
            DiffBtn.IsEnabled = true;
        }
    }

    private void CopyDiff_Click(object sender, RoutedEventArgs e)
    {
        if (_lastDiff.Count == 0) return;
        CopyToClipboard(RegDiffService.ToCommands(_lastDiff, _snapshotRoot));
        DiffInfo.Message = "reg commands copied — paste them into a Tweak Lab command block.";
        DiffInfo.Severity = InfoBarSeverity.Success;
        DiffInfo.IsOpen = true;
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

    private void AddTask_Click(object sender, RoutedEventArgs e) => AddBlock(new TweakBlock
    {
        Kind = "task", Title = "Disable a scheduled task", AccentHex = "#38bdf8",
        Label1 = "Task path", Ph1 = @"e.g. \Microsoft\Windows\Application Experience\StartupAppTask",
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

    // ── Blocks / Code mode ───────────────────────────────────────────────────
    private void CodeMode_Toggled(object sender, RoutedEventArgs e)
    {
        bool code = CodeModeToggle.IsOn;
        BlocksPanel.Visibility = code ? Visibility.Collapsed : Visibility.Visible;
        CodePanel.Visibility = code ? Visibility.Visible : Visibility.Collapsed;
        ModeHint.Text = code
            ? "Raw commands — full control (cmd.exe, or prefix PS: for PowerShell)."
            : "Visual blocks — no command line needed.";
    }

    private void LoadCodeFromBlocks_Click(object sender, RoutedEventArgs e)
    {
        var (apply, revert) = ComposeFromBlocks();
        CodeApply.Text = apply;
        CodeRevert.Text = revert;
    }

    /// <summary>Apply/revert from whichever builder mode is active.</summary>
    private (string apply, string revert) Compose()
        => CodeModeToggle.IsOn
            ? (CodeApply.Text.Trim(), CodeRevert.Text.Trim())
            : ComposeFromBlocks();

    private (string apply, string revert) ComposeFromBlocks()
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
                case "task":
                    string tp = b.V1.Trim();
                    if (tp.Length == 0) continue;
                    applies.Add($"schtasks /Change /TN \"{tp}\" /Disable");
                    reverts.Add($"schtasks /Change /TN \"{tp}\" /Enable");
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
        var tier = LabTier.SelectedIndex == 1 ? TweakTier.Extreme : TweakTier.Safe;
        _engine.AddCustomTweak(new Tweak
        {
            Id = "custom_" + Guid.NewGuid().ToString("N")[..6],
            Name = name,
            Category = string.IsNullOrWhiteSpace(LabCategory.Text) ? "Custom" : LabCategory.Text.Trim(),
            Tier = tier,
            Description = string.IsNullOrWhiteSpace(LabDesc.Text) ? "Custom tweak built in DevTools." : LabDesc.Text.Trim(),
            ApplyCmd = apply,
            RevertCmd = revert,
        });
        PreviewText.Text = $"Saved \"{name}\" to your catalog — it's on the Tweaks page now and survives restarts.";
    }

    // ── share codes: JSON → gzip → base64 with a VT1: prefix ──────────────────────────
    private sealed class LabShare
    {
        public string N { get; set; } = "";
        public string C { get; set; } = "";
        public int T { get; set; }
        public string D { get; set; } = "";
        public List<LabShareBlock> B { get; set; } = new();
    }

    private sealed class LabShareBlock
    {
        public string K { get; set; } = "";
        public string V1 { get; set; } = "";
        public string V2 { get; set; } = "";
        public string V3 { get; set; } = "";
    }

    private void ExportLab_Click(object sender, RoutedEventArgs e)
    {
        if (Blocks.Count == 0) { PreviewText.Text = "Nothing to share — add blocks first."; return; }
        var share = new LabShare
        {
            N = LabName.Text.Trim(),
            C = LabCategory.Text.Trim(),
            T = LabTier.SelectedIndex,
            D = LabDesc.Text.Trim(),
            B = Blocks.Select(b => new LabShareBlock { K = b.Kind, V1 = b.V1, V2 = b.V2, V3 = b.V3 }).ToList(),
        };
        byte[] json = JsonSerializer.SerializeToUtf8Bytes(share);
        using var ms = new MemoryStream();
        using (var gz = new GZipStream(ms, CompressionLevel.SmallestSize, leaveOpen: true)) gz.Write(json);
        string code = "VT1:" + Convert.ToBase64String(ms.ToArray());
        CopyToClipboard(code);
        PreviewText.Text = $"Share code copied ({code.Length} chars) — paste it in Discord. Anyone can Import it in their Tweak Lab.\n\n{code}";
    }

    private async void ImportLab_Click(object sender, RoutedEventArgs e)
    {
        var box = new TextBox { PlaceholderText = "Paste a VT1: share code…", AcceptsReturn = true, MinWidth = 380, MaxHeight = 140, TextWrapping = TextWrapping.Wrap };
        var dlg = new ContentDialog
        {
            Title = "Import tweak code",
            Content = box,
            PrimaryButtonText = "Import",
            CloseButtonText = "Cancel",
            DefaultButton = ContentDialogButton.Primary,
            XamlRoot = this.XamlRoot,
        };
        if (await dlg.ShowAsync() != ContentDialogResult.Primary) return;

        try
        {
            string code = box.Text.Trim();
            if (!code.StartsWith("VT1:", StringComparison.Ordinal)) throw new InvalidOperationException("Not a VT1 code.");
            using var input = new MemoryStream(Convert.FromBase64String(code[4..]));
            using var gz = new GZipStream(input, CompressionMode.Decompress);
            using var output = new MemoryStream();
            gz.CopyTo(output);
            var share = JsonSerializer.Deserialize<LabShare>(output.ToArray()) ?? throw new InvalidOperationException("Empty code.");

            LabName.Text = share.N;
            LabCategory.Text = share.C;
            LabTier.SelectedIndex = Math.Clamp(share.T, 0, 1);
            LabDesc.Text = share.D;
            Blocks.Clear();
            foreach (var b in share.B)
            {
                var block = b.K switch
                {
                    "reg" => new TweakBlock { Kind = "reg", Title = "Registry value", AccentHex = "#a78bfa", Label1 = "Key", Ph1 = @"HKLM\SOFTWARE\...", Label2 = "Value name", Ph2 = "ValueName", Has2 = true, Label3 = "Data", Ph3 = "1", Has3 = true },
                    "svc" => new TweakBlock { Kind = "svc", Title = "Disable a service", AccentHex = "#f59e0b", Label1 = "Service name", Ph1 = "e.g. DiagTrack" },
                    "task" => new TweakBlock { Kind = "task", Title = "Disable a scheduled task", AccentHex = "#38bdf8", Label1 = "Task path", Ph1 = @"\Microsoft\..." },
                    _ => new TweakBlock { Kind = "cmd", Title = "Run a command", AccentHex = "#22c55e", Label1 = "Apply command", Ph1 = "", Label2 = "Revert command (optional)", Ph2 = "", Has2 = true },
                };
                block.V1 = b.V1; block.V2 = b.V2; block.V3 = b.V3;
                Blocks.Add(block);
            }
            UpdateBlockEmpty();
            PreviewText.Text = $"Imported \"{share.N}\" ({share.B.Count} block(s)) — review it, then save or submit.";
        }
        catch (Exception ex)
        {
            PreviewText.Text = "Import failed: " + ex.Message;
        }
    }

    private void Submit_Click(object sender, RoutedEventArgs e)
    {
        string name = LabName.Text.Trim();
        var (apply, revert) = Compose();
        if (name.Length == 0 || apply.Length == 0) { PreviewText.Text = "Give the tweak a name and at least one filled block first."; return; }
        string cat = string.IsNullOrWhiteSpace(LabCategory.Text) ? "Custom" : LabCategory.Text.Trim();
        string tier = (LabTier.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "Safe";
        string desc = LabDesc.Text.Trim();
        string body = $"### Proposed tweak\n\n**Name:** {name}\n**Category:** {cat}\n**Tier:** {tier}\n**Description:** {desc}\n\n```\nApply:  {apply}\nRevert: {revert}\n```\n\n_Submitted from the VOIDTUNE DevTools Tweak Builder._";
        string url = "https://github.com/otzpt/VOIDTUNE/issues/new"
            + "?title=" + Uri.EscapeDataString("Tweak: " + name)
            + "&body=" + Uri.EscapeDataString(body)
            + "&labels=community-tweak";
        try { Process.Start(new ProcessStartInfo(url) { UseShellExecute = true }); } catch { /* ignore */ }
        PreviewText.Text = "Opened a pre-filled GitHub issue in your browser — review and submit it there for approval.";
    }

    // ── Network ───────────────────────────────────────────────────────────────────────
    private async void RefreshNet_Click(object sender, RoutedEventArgs e) => await RefreshNet();

    private async System.Threading.Tasks.Task RefreshNet()
    {
        try { AdapterList.ItemsSource = await NetToolsService.GetAdaptersAsync(); }
        catch { /* adapters unavailable — leave the list */ }
    }

    private async void PingTest_Click(object sender, RoutedEventArgs e)
    {
        PingBusy.IsActive = true;
        var targets = new List<(string label, string host)>();
        string? gw = NetToolsService.GetGateway();
        if (gw != null) targets.Add(($"Router ({gw})", gw));
        targets.Add(("Cloudflare 1.1.1.1", "1.1.1.1"));
        targets.Add(("Google 8.8.8.8", "8.8.8.8"));

        var rows = targets.Select(t => new PingRow { Target = t.label, Result = "testing…" }).ToList();
        PingList.ItemsSource = rows;

        for (int i = 0; i < targets.Count; i++)
        {
            var (result, hex) = await NetToolsService.PingAsync(targets[i].host);
            rows[i] = new PingRow { Target = targets[i].label, Result = result, Hex = hex };
            PingList.ItemsSource = null;
            PingList.ItemsSource = rows;
        }
        PingBusy.IsActive = false;
    }

    private async void RefreshConns_Click(object sender, RoutedEventArgs e)
    {
        ConnBusy.IsActive = true;
        try
        {
            var (rows, summary) = await NetToolsService.GetConnectionsAsync();
            ConnList.ItemsSource = rows;
            ConnSummary.Text = summary;
        }
        catch (Exception ex) { ConnSummary.Text = "failed: " + ex.Message; }
        finally { ConnBusy.IsActive = false; }
    }

    private async void DnsCf_Click(object sender, RoutedEventArgs e)
    {
        DnsStatus.Text = "Switching to Cloudflare…";
        await NetToolsService.SetDnsAsync("1.1.1.1", "1.0.0.1");
        DnsStatus.Text = "DNS set to Cloudflare (1.1.1.1 / 1.0.0.1) on connected adapters.";
        await RefreshNet();
    }

    private async void DnsGoogle_Click(object sender, RoutedEventArgs e)
    {
        DnsStatus.Text = "Switching to Google…";
        await NetToolsService.SetDnsAsync("8.8.8.8", "8.8.4.4");
        DnsStatus.Text = "DNS set to Google (8.8.8.8 / 8.8.4.4) on connected adapters.";
        await RefreshNet();
    }

    private async void DnsReset_Click(object sender, RoutedEventArgs e)
    {
        DnsStatus.Text = "Resetting to router / DHCP…";
        await NetToolsService.ResetDnsAsync();
        DnsStatus.Text = "DNS reset — your router / DHCP decides again.";
        await RefreshNet();
    }

    // ── Core Affinity ─────────────────────────────────────────────────────────────────
    private void RefreshProcs_Click(object sender, RoutedEventArgs e) => RefreshProcs();

    private void RefreshProcs()
    {
        int cores = Environment.ProcessorCount;
        PinHalfBtn.Content = $"Pin to last {cores / 2} cores";
        PinTwoBtn.Content = "Pin to last 2 cores";

        var rows = new List<ProcRow>();
        foreach (var p in Process.GetProcesses())
        {
            try
            {
                if (p.SessionId == 0) continue;               // services — leave them alone
                long ws = p.WorkingSet64;
                if (ws < 40L * 1024 * 1024) continue;         // noise filter: >40 MB only
                string aff;
                bool pinned = false;
                try
                {
                    long mask = (long)p.ProcessorAffinity;
                    long all = (1L << cores) - 1;
                    pinned = mask != all;
                    aff = pinned ? $"pinned mask 0x{mask:X}" : "all cores";
                }
                catch { aff = "?"; }
                rows.Add(new ProcRow
                {
                    Name = p.ProcessName,
                    Pid = p.Id,
                    Detail = $"{ws / 1024 / 1024} MB · {aff}",
                    Hex = pinned ? "#F472B6" : "#9a93a8",
                });
            }
            catch { /* exited or protected */ }
        }
        ProcList.ItemsSource = rows.OrderByDescending(r => r.Hex == "#F472B6")
                                   .ThenBy(r => r.Name, StringComparer.OrdinalIgnoreCase).ToList();
        AffStatus.Text = $"{rows.Count} user processes · select and pin.";
    }

    private void ProcSel_Changed(object sender, SelectionChangedEventArgs e)
    {
        bool any = ProcList.SelectedItems.Count > 0;
        PinHalfBtn.IsEnabled = any;
        PinTwoBtn.IsEnabled = any;
        ResetAffBtn.IsEnabled = any;
    }

    private void PinHalf_Click(object sender, RoutedEventArgs e)
    {
        int cores = Environment.ProcessorCount;
        ApplyAffinity(MaskLastN(Math.Max(1, cores / 2)));
    }

    private void PinTwo_Click(object sender, RoutedEventArgs e) => ApplyAffinity(MaskLastN(2));

    private void ResetAff_Click(object sender, RoutedEventArgs e)
        => ApplyAffinity((1L << Environment.ProcessorCount) - 1);

    private static long MaskLastN(int n)
    {
        int cores = Environment.ProcessorCount;
        n = Math.Clamp(n, 1, cores);
        long mask = 0;
        for (int i = cores - n; i < cores; i++) mask |= 1L << i;
        return mask;
    }

    private void ApplyAffinity(long mask)
    {
        int ok = 0, fail = 0;
        foreach (var item in ProcList.SelectedItems.OfType<ProcRow>())
        {
            try
            {
                using var p = Process.GetProcessById(item.Pid);
                p.ProcessorAffinity = (IntPtr)mask;
                ok++;
            }
            catch { fail++; }
        }
        AffStatus.Text = $"affinity 0x{mask:X} → {ok} set" + (fail > 0 ? $", {fail} refused (protected/exited)" : "");
        RefreshProcs();
    }
}
