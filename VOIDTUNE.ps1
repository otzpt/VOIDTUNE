# VOIDTUNE Optimizer
# Copyright (C) 2026 @OTZPT
# Licensed under the GNU General Public License v3.0
# See LICENSE.txt for full license text or https://www.gnu.org/licenses/gpl-3.0.txt
# VOIDTUNE Suite v0.8

trap {
    try {
        $log = Join-Path $env:TEMP 'voidtune_crash.log'
        $msg = "[$(Get-Date -Format 'HH:mm:ss')] CRASH: $($_.Exception.Message)`r`n"
        $msg += "  AT: $($_.InvocationInfo.ScriptName):$($_.InvocationInfo.ScriptLineNumber)`r`n"
        $msg += "  LINE: $($_.InvocationInfo.Line)`r`n"
        $msg += "  STACK: $($_.ScriptStackTrace)`r`n"
        Add-Content -Path $log -Value $msg
        [System.Windows.Forms.MessageBox]::Show("VOIDTUNE startup crash:`n`n$($_.Exception.Message)`n`nSee: $log", "VOIDTUNE Crash") | Out-Null
    } catch {}
    continue
}

Add-Type -AssemblyName PresentationFramework,PresentationCore,WindowsBase
Add-Type -AssemblyName System.Windows.Forms

# Resolve exe/script path — works both as .ps1 and ps2exe compiled .exe
$script:ROOT_ENTRY = $null
if (-not $script:ROOT_ENTRY -and $PSScriptRoot)                          { $script:ROOT_ENTRY = $PSScriptRoot }
if (-not $script:ROOT_ENTRY -and $MyInvocation.MyCommand.Path)           { $script:ROOT_ENTRY = Split-Path -Parent $MyInvocation.MyCommand.Path }
if (-not $script:ROOT_ENTRY) {
    $appBase = [System.AppDomain]::CurrentDomain.BaseDirectory
    if ($appBase) { $script:ROOT_ENTRY = $appBase.TrimEnd('\') }
}
if (-not $script:ROOT_ENTRY) {
    $exePath = [System.Diagnostics.Process]::GetCurrentProcess().MainModule.FileName
    if ($exePath) { $script:ROOT_ENTRY = Split-Path -Parent $exePath }
}
if (-not $script:ROOT_ENTRY) { $script:ROOT_ENTRY = [System.IO.Directory]::GetCurrentDirectory() }

# Admin check - show dialog if not elevated (UAC fallback)
if (-not ([Security.Principal.WindowsPrincipal][Security.Principal.WindowsIdentity]::GetCurrent()).IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)) {
    $exePath = [System.Diagnostics.Process]::GetCurrentProcess().MainModule.FileName
    if ($exePath -like '*.exe') {
        Start-Process $exePath -Verb RunAs
    } else {
        $p = if ($PSCommandPath) { $PSCommandPath } else { $MyInvocation.MyCommand.Path }
        Start-Process powershell.exe "-NoProfile -ExecutionPolicy Bypass -File `"$p`"" -Verb RunAs
    }
    exit
}

# Splash screen — shown on the main thread. A separate STA + Dispatcher.Run()
# thread tears the process down under ps2exe -noConsole, so we render inline:
# each Update-Splash pumps the dispatcher to Render priority to repaint.
function Update-Splash($win, $msg, $pct) {
    if (-not $win) { return }
    try {
        $win.FindName('SplashStatus').Text = $msg
        $win.FindName('SplashProg').Value  = $pct
        $win.Dispatcher.Invoke([action]{}, [System.Windows.Threading.DispatcherPriority]::Render)
    } catch {}
}

$script:splashPath = Join-Path $script:ROOT_ENTRY 'VOIDTUNE_SPLASH.xaml'
$script:splash     = $null
if (Test-Path $script:splashPath) {
    try {
        [xml]$sx = Get-Content $script:splashPath -Encoding UTF8 -Raw
        $script:splash = [Windows.Markup.XamlReader]::Load((New-Object System.Xml.XmlNodeReader $sx))
        $script:splash.Show()
        $script:splash.Dispatcher.Invoke([action]{}, [System.Windows.Threading.DispatcherPriority]::Render)
    } catch { $script:splash = $null }
}

# Paths
$script:ROOT    = $script:ROOT_ENTRY
$script:BACKUPS = Join-Path $script:ROOT 'backups'
$script:LOGS    = Join-Path $script:ROOT 'logs'
foreach ($d in @($script:BACKUPS, $script:LOGS)) {
    if (!(Test-Path $d)) { New-Item -ItemType Directory $d -Force | Out-Null }
}

# Core modules
. (Join-Path $script:ROOT 'core\logging.ps1')
WL "VOIDTUNE v0.8 starting as admin"

Update-Splash $script:splash 'DETECTING HARDWARE...' 15
. (Join-Path $script:ROOT 'core\hardware.ps1')

# Load XAML
Update-Splash $script:splash 'LOADING INTERFACE...' 35
$xamlFile = Join-Path $script:ROOT 'VOIDTUNE.xaml'
if (-not (Test-Path $xamlFile)) {
    [System.Windows.Forms.MessageBox]::Show("VOIDTUNE.xaml not found in:`n$($script:ROOT)", "VOIDTUNE Error")
    exit
}
try {
    [xml]$XAML  = Get-Content $xamlFile -Encoding UTF8 -Raw
    $script:Win = [Windows.Markup.XamlReader]::Load((New-Object System.Xml.XmlNodeReader $XAML))
} catch {
    [System.Windows.Forms.MessageBox]::Show("XAML failed:`n$($_.Exception.Message)", "VOIDTUNE Error")
    exit
}

Update-Splash $script:splash 'COMPILING MODELS...' 50
. (Join-Path $script:ROOT 'core\models.ps1')

Update-Splash $script:splash 'LOADING MODULES...' 65
. (Join-Path $script:ROOT 'core\helpers.ps1')
. (Join-Path $script:ROOT 'modules\data.ps1')
. (Join-Path $script:ROOT 'modules\personalize.ps1')
. (Join-Path $script:ROOT 'modules\drivers.ps1')

Update-Splash $script:splash 'WIRING COMPONENTS...' 80
. (Join-Path $script:ROOT 'ui\pages.ps1')
. (Join-Path $script:ROOT 'ui\events.ps1')

# Restore applied tweak state from last session
Load-TweakState

# Launch
ShowPage 'PgDash'
$hwStr = "CPU: $($script:HW.CpuVendor) [$($script:HW.CpuName.Split(' ')[0..2] -join ' ')]  |  GPU: $($script:HW.GpuVendor) [$($script:HW.GpuName)]  |  Build: $($script:HW.WinBuild)  |  $(if($script:HW.IsLaptop){'LAPTOP'}else{'DESKTOP'})  |  Winget: $(if($script:HW.WingetOK){'OK'}else{'NOT FOUND'})"
AddLog "VOIDTUNE v0.8 ready. Running as Administrator." '#7c3aed'
AddLog $hwStr '#555555'

if ($script:HW.WinBuild -lt 19041) { AddLog "WARNING: Build $($script:HW.WinBuild) - HW GPU Scheduling not supported." '#F59E0B' }
if ($script:HW.IsLaptop)           { AddLog "LAPTOP DETECTED: Avoid EXTREME power tweaks on battery." '#F59E0B' }
if (-not $script:HW.WingetOK -and -not $script:HW.ChocoOK) { AddLog "WINGET AND CHOCO NOT FOUND: App Installer won't work." '#E01818' }
if ($script:ARCH_TWEAKS.Count -gt 0) {
    $cpuTw = @($script:ARCH_TWEAKS | Where-Object { $_.Cat -eq 'cpu' }).Count
    $gpuTw = @($script:ARCH_TWEAKS | Where-Object { $_.Cat -eq 'gpu' }).Count
    AddLog "Arch tweaks: $cpuTw CPU ($($script:HW.CpuVendor)) + $gpuTw GPU ($($script:HW.GpuVendor)) unlocked." '#38BDF8'
}
WL "Window shown"

Update-Splash $script:splash 'READY.' 100
Start-Sleep -Milliseconds 300
try { if ($script:splash) { $script:splash.Dispatcher.Invoke([action]{ $script:splash.Close() }) } } catch {}
$script:splash = $null

# Disclaimer
$disclaimer = @"
VOIDTUNE OPTIMIZER v0.8 - DISCLAIMER

By clicking OK you acknowledge and agree to the following:

1. USE AT YOUR OWN RISK
   VOIDTUNE modifies Windows registry entries, services and system settings.
   These changes may affect system stability, performance or behaviour.
   The author(s) accept no responsibility for data loss, hardware damage,
   system instability or any other issues arising from use of this software.

2. NO WARRANTY
   VOIDTUNE is provided AS-IS without any warranty of any kind, express or
   implied. There is no guarantee that tweaks will improve your system or
   produce the same results on all hardware configurations.

3. ADMINISTRATOR PRIVILEGES
   This software requires and runs with administrator privileges. The Script
   Runner tab can execute any system command with full admin rights.
   Only run scripts you trust.

4. BACKUPS
   Always create a Windows System Restore Point before applying tweaks.
   The built-in registry backup does not cover service or power plan changes.

5. OPEN SOURCE - GPL v3
   VOIDTUNE is an independent open source project licensed under GPL v3.
   Review the source code before use. See LICENSE.txt for full terms.

CANCEL closes VOIDTUNE without making any changes to your system.
"@

$dlResult = [System.Windows.MessageBox]::Show(
    $disclaimer,
    'VOIDTUNE - Legal Disclaimer',
    [System.Windows.MessageBoxButton]::OKCancel,
    [System.Windows.MessageBoxImage]::Information)

if ($dlResult -ne 'OK') { WL 'User declined disclaimer - exiting'; exit }
WL 'Disclaimer accepted'

$script:Win.ShowDialog() | Out-Null
$script:timer.Stop()
WL "VOIDTUNE closed"
