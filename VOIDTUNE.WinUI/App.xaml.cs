using System;
using System.Diagnostics;
using System.IO;
using System.Security.Principal;
using Microsoft.UI.Xaml;

namespace VOIDTUNE.WinUI;

public partial class App : Application
{
    public static Window? MainWindow { get; private set; }

    private static void LogCrash(string source, object detail)
    {
        try
        {
            string dir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "VOIDTUNE", "logs");
            Directory.CreateDirectory(dir);
            File.AppendAllText(Path.Combine(dir, "crash.log"), $"{DateTime.Now:O} [{source}]\n{detail}\n\n");
        }
        catch { /* nothing left to report to */ }
    }

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
        // A failed click handler should not take the app down mid-apply. State is safe to keep
        // using because tweak state is re-read from the system, not held only in memory.
        this.UnhandledException += (s, e) =>
        {
            LogCrash("UI", e.Exception);
            e.Handled = true;
        };
        AppDomain.CurrentDomain.UnhandledException += (s, e) => LogCrash("DOMAIN", e.ExceptionObject);
        System.Threading.Tasks.TaskScheduler.UnobservedTaskException += (s, e) =>
        {
            LogCrash("TASK", e.Exception);
            e.SetObserved();
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
            LogCrash("LAUNCH", ex);
            throw;
        }
    }
}
