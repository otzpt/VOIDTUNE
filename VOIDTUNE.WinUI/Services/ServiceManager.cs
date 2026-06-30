using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ServiceProcess;
using System.Threading.Tasks;
using VOIDTUNE.WinUI.Models;

namespace VOIDTUNE.WinUI.Services;

/// <summary>Curated Windows service manager. Status via ServiceController (locale-independent); actions via sc.exe.</summary>
public sealed class ServiceManager
{
    public ObservableCollection<ServiceItem> Services { get; } = new();

    // Bound on concurrent sc.exe processes when applying a whole profile at once.
    private static readonly int MaxParallel = Math.Clamp(Environment.ProcessorCount, 4, 12);

    private static readonly (string Name, string Desc)[] Catalog =
    {
        ("DiagTrack",          "Windows Telemetry — safe to disable"),
        ("dmwappushservice",   "WAP Push Routing — telemetry relay"),
        ("SysMain",            "Superfetch — useless on SSD"),
        ("WSearch",            "Windows Search indexing — heavy on HDD"),
        ("Spooler",            "Print Spooler — disable if no printer"),
        ("RemoteRegistry",     "Remote Registry — security risk"),
        ("Fax",                "Fax service — rarely used"),
        ("MapsBroker",         "Offline Maps — unused on most PCs"),
        ("WMPNetworkSvc",      "Windows Media Player sharing"),
        ("bthserv",            "Bluetooth — disable if no BT devices"),
        ("DoSvc",              "Delivery Optimization — P2P update relay"),
        ("XblAuthManager",     "Xbox Live Authentication"),
        ("XblGameSave",        "Xbox Game Save sync"),
        ("XboxNetApiSvc",      "Xbox Network API"),
        ("XboxGipSvc",         "Xbox Accessory Management — no Xbox pad needed"),
        ("WbioSrvc",           "Windows Biometric — disable if no reader"),
        ("TabletInputService", "Touch Keyboard / Handwriting — desktop unused"),
        ("lfsvc",              "Geolocation — disable if you don't use location"),
        ("AJRouter",           "AllJoyn Router — IoT bridge, rarely used"),
        ("SEMgrSvc",           "Payments & NFC manager — usually idle"),
        ("SensorService",      "Sensor aggregator — desktop has no sensors"),
        ("RetailDemo",         "Retail Demo Mode — store kiosks only"),
        ("W32Time",            "Time sync — safe to disable when offline"),
        ("PcaSvc",             "Program Compatibility Assistant — background"),
        ("CDPSvc",             "Connected Devices Platform — chatty background"),
        ("DPS",                "Diagnostic Policy — spawns WdiServiceHost"),
        ("WerSvc",             "Error Reporting — WerFault background work"),
        ("TrkWks",             "Distributed Link Tracking — rarely needed"),
        ("SSDPSRV",            "SSDP Discovery — only for DLNA/UPnP"),
        ("upnphost",           "UPnP Device Host — only for media sharing"),
        ("SCardSvr",           "Smart Card — disable if no reader"),
        ("stisvc",             "Image Acquisition — only for scanners"),
        ("PhoneSvc",           "Phone Service — call integration, idle"),
        ("icssvc",             "Mobile Hotspot — disable if unused"),
        ("wisvc",              "Windows Insider — not on stable channel"),
        ("SharedAccess",       "Internet Connection Sharing — rarely used"),
        ("wuauserv",           "Windows Update — disable with caution"),
    };

    private static readonly string[] GamingDisable =
    {
        "DiagTrack", "dmwappushservice", "SysMain", "WSearch",
        "XblAuthManager", "XblGameSave", "XboxNetApiSvc", "XboxGipSvc",
        "MapsBroker", "WMPNetworkSvc", "Fax", "DoSvc", "WbioSrvc",
        "TabletInputService", "lfsvc", "AJRouter", "SEMgrSvc",
        "SensorService", "RetailDemo", "PcaSvc", "CDPSvc",
        "WerSvc", "TrkWks", "SSDPSRV", "upnphost", "SCardSvr",
        "stisvc", "PhoneSvc", "icssvc", "wisvc", "SharedAccess",
    };

    public ServiceManager()
    {
        foreach (var (name, desc) in Catalog)
            Services.Add(new ServiceItem { Name = name, Description = desc });
    }

    public void Refresh()
    {
        foreach (var s in Services)
        {
            try
            {
                using var sc = new ServiceController(s.Name);
                var status = sc.Status;
                s.Status = status.ToString().ToUpperInvariant();
                s.StatusHex = status == ServiceControllerStatus.Running ? "#F59E0B" : "#22C55E";
            }
            catch
            {
                s.Status = "N/A";
                s.StatusHex = "#3A3A3A";
            }
        }
    }

    public async Task SetAsync(string name, bool enable)
    {
        if (enable)
        {
            await CommandRunner.ExecAsync($"sc config {name} start= demand & sc start {name} & exit /b 0");
        }
        else
        {
            await CommandRunner.ExecAsync($"sc config {name} start= disabled & sc stop {name} & exit /b 0");
        }
        Refresh();
    }

    public async Task ApplyGamingProfileAsync()
    {
        // Each service is independent, so disable them concurrently instead of waiting on
        // ~30 sc.exe processes one after another.
        await Parallel.ForEachAsync(GamingDisable, new ParallelOptions { MaxDegreeOfParallelism = MaxParallel },
            async (name, _) => await CommandRunner.ExecAsync($"sc config {name} start= disabled & sc stop {name} & exit /b 0"));
        Refresh();
    }

    public async Task ApplyNormalProfileAsync()
    {
        var restore = new Dictionary<string, string>
        {
            ["SysMain"] = "auto", ["WSearch"] = "auto", ["bthserv"] = "auto",
            ["Spooler"] = "auto", ["DoSvc"] = "demand", ["wuauserv"] = "demand",
            ["TabletInputService"] = "demand", ["lfsvc"] = "demand", ["AJRouter"] = "demand",
            ["SEMgrSvc"] = "demand", ["SensorService"] = "demand", ["PcaSvc"] = "auto",
            ["CDPSvc"] = "auto", ["DPS"] = "auto", ["W32Time"] = "demand",
        };
        await Parallel.ForEachAsync(restore, new ParallelOptions { MaxDegreeOfParallelism = MaxParallel },
            async (kv, _) => await CommandRunner.ExecAsync($"sc config {kv.Key} start= {kv.Value} & sc start {kv.Key} & exit /b 0"));
        Refresh();
    }
}
