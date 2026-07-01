using System;
using System.Diagnostics;
using System.IO;
using System.Security.Principal;
using Microsoft.UI.Xaml;

namespace VOIDTUNE.WinUI;

public partial class App : Application
{
    public static Window? MainWindow { get; private set; }

    private static readonly string CrashLog = Path.Combine(Path.GetTempPath(), "voidtune_winui_crash.log");

    public App()
    {
        // Auto-elevation. The app manifest already requests requireAdministrator, so Windows
        // normally shows the UAC prompt before the process even starts. This is a defensive
        // fallback for the rare case the process still launches unelevated (e.g. started via a
        // host that ignores the embedded manifest): relaunch ourselves with a UAC prompt and
        // exit the unelevated instance. Under normal use this is a no-op (already elevated).
        if (!IsElevated() && RelaunchElevated())
        {
            Environment.Exit(0);
            return;
        }

        this.InitializeComponent();
        this.UnhandledException += (s, e) =>
        {
            try { File.WriteAllText(CrashLog, $"{DateTime.Now}\n{e.Message}\n{e.Exception}"); } catch { }
        };
        AppDomain.CurrentDomain.UnhandledException += (s, e) =>
        {
            try { File.WriteAllText(CrashLog, $"{DateTime.Now}\nDOMAIN\n{e.ExceptionObject}"); } catch { }
        };
    }

    /// <summary>True when the current process is running with Administrator rights.</summary>
    private static bool IsElevated()
    {
        try
        {
            using var id = WindowsIdentity.GetCurrent();
            return new WindowsPrincipal(id).IsInRole(WindowsBuiltInRole.Administrator);
        }
        catch { return true; }   // assume elevated on error so we can never relaunch-loop
    }

    /// <summary>Relaunches this executable with the "runas" verb, which triggers the UAC prompt.</summary>
    private static bool RelaunchElevated()
    {
        try
        {
            string? exe = Environment.ProcessPath;
            if (string.IsNullOrEmpty(exe)) return false;
            Process.Start(new ProcessStartInfo
            {
                FileName = exe,
                UseShellExecute = true,
                Verb = "runas",                       // shows the UAC elevation prompt
                WorkingDirectory = AppContext.BaseDirectory,
            });
            return true;
        }
        catch { return false; }   // user declined UAC, or launch failed — fall through unelevated
    }

    protected override void OnLaunched(LaunchActivatedEventArgs args)
    {
        try
        {
            MainWindow = new MainWindow();
            MainWindow.Activate();
        }
        catch (Exception ex)
        {
            try { File.WriteAllText(CrashLog, $"{DateTime.Now}\nONLAUNCHED\n{ex}"); } catch { }
            throw;
        }
    }
}
