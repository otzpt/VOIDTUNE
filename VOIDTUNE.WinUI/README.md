# VOIDTUNE.WinUI

Source for the WinUI 3 (C# / .NET 8, Windows App SDK) application. See the [root README](../README.md) for what VOIDTUNE does and [../docs/](../docs/) for feature and tweak documentation.

## Layout

```
App.xaml(.cs)        App startup, theme, crash log
MainWindow.xaml(.cs)  Shell: NavigationView + Mica backdrop
app.manifest          requireAdministrator, PerMonitorV2 DPI
Models/                Tweak, AppItem, PersonalizeToggle, DriverItem, StartupItem, BackupItem, ServiceItem, ProcessItem
Services/              One service class per feature area — TweakCatalog, TweakEngine, HardwareInfo,
                        SystemMonitor, ServiceManager, ProcessMonitor, PersonalizeService, AppCatalog,
                        DriverService, GpuHealthService, LatencyService, BenchmarkService, StartupManager,
                        BackupService, CommandRunner
Converters/            XAML value converters
Views/                 One page per navigation item
installer/             WiX v5 MSI authoring + build.ps1
```

Build instructions: [../docs/building.md](../docs/building.md).
