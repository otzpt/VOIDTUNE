using System;
using System.IO;
using Microsoft.UI.Xaml;

namespace VOIDTUNE.WinUI;

public partial class App : Application
{
    public static Window? MainWindow { get; private set; }

    private static readonly string CrashLog = Path.Combine(Path.GetTempPath(), "voidtune_winui_crash.log");

    public App()
    {
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
