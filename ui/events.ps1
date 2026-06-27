# VOIDTUNE - ui/events.ps1
# Copyright (C) 2026 @OTZPT - GPL v3
# All event handlers, data binding, timer

# ── DATA BINDING ──────────────────────────────────────────────────────────────
$script:TweakTabs = G 'TweakTabs'

$script:svcOC = New-Object 'System.Collections.ObjectModel.ObservableCollection[SI]'
foreach ($s in $script:SVCS_DATA) { $script:svcOC.Add($s) }
$sl = G 'SvcList'; if ($sl) { $sl.ItemsSource = $script:svcOC }

$privCol = New-Object 'System.Collections.ObjectModel.ObservableCollection[TI]'
foreach ($t in $script:PRIVTWEAKS) { $privCol.Add($t) }
$pl = G 'PrivList'; if ($pl) { $pl.ItemsSource = $privCol }

$appCol = New-Object 'System.Collections.ObjectModel.ObservableCollection[AI]'
foreach ($a in $script:APPS_DATA) { $appCol.Add($a) }
$al = G 'AppList'
if ($al) {
    $al.ItemsSource = $appCol
    $view = [System.Windows.Data.CollectionViewSource]::GetDefaultView($appCol)
    $view.GroupDescriptions.Clear()
    $view.GroupDescriptions.Add((New-Object System.Windows.Data.PropertyGroupDescription "Cat"))
    $view.SortDescriptions.Add((New-Object System.ComponentModel.SortDescription "Cat",  "Ascending"))
    $view.SortDescriptions.Add((New-Object System.ComponentModel.SortDescription "Name", "Ascending"))
}

# App search bar
$appSearchBox = G 'AppSearch'
if ($appSearchBox) {
    $appSearchBox.Add_TextChanged({
        try {
            $q    = (G 'AppSearch').Text.Trim().ToLower()
            $alEl = G 'AppList'; if (-not $alEl) { return }
            $view = [System.Windows.Data.CollectionViewSource]::GetDefaultView($alEl.ItemsSource)
            if ([string]::IsNullOrEmpty($q)) {
                $view.Filter = $null
            } else {
                $view.Filter = [Predicate[object]]{
                    param($item)
                    $item.Name.ToLower().Contains($q) -or $item.Cat.ToLower().Contains($q)
                }
            }
        } catch {}
    })
}
$btnSearchClear = G 'BtnAppSearchClear'
if ($btnSearchClear) {
    $btnSearchClear.Add_Click({
        try {
            $sb = G 'AppSearch'; if ($sb) { $sb.Text = '' }
            $alEl = G 'AppList'; if (-not $alEl) { return }
            $view = [System.Windows.Data.CollectionViewSource]::GetDefaultView($alEl.ItemsSource)
            $view.Filter = $null
        } catch {}
    })
}

if ($script:TweakTabs) { $script:TweakTabs.Add_SelectionChanged({ try { FilterTweaks } catch {} }) }
FilterTweaks

# ── NAVIGATION ────────────────────────────────────────────────────────────────
(G 'BtnDash').Add_Click({       try { ShowPage 'PgDash' }       catch {} })
(G 'BtnTweaks').Add_Click({     try { ShowPage 'PgTweaks' }     catch {} })
(G 'BtnApps').Add_Click({       try { ShowPage 'PgApps' }       catch {} })
(G 'BtnServices').Add_Click({   try { ShowPage 'PgServices' }   catch {} })
(G 'BtnStartup').Add_Click({    try { ShowPage 'PgStartup' }    catch {} })
(G 'BtnPrivacy').Add_Click({    try { ShowPage 'PgPrivacy' }    catch {} })
(G 'BtnDiag').Add_Click({       try { ShowPage 'PgDiag' }       catch {} })
(G 'BtnBench').Add_Click({      try { ShowPage 'PgBench' }      catch {} })
(G 'BtnSafety').Add_Click({     try { ShowPage 'PgSafety' }     catch {} })
(G 'BtnScript').Add_Click({     try { ShowPage 'PgScript' }     catch {} })
(G 'BtnProc').Add_Click({       try { ShowPage 'PgProc' }       catch {} })
(G 'BtnPersonalize').Add_Click({ try { ShowPage 'PgPersonalize' } catch {} })
(G 'BtnDrivers').Add_Click({    try { ShowPage 'PgDrivers' }    catch {} })
(G 'BtnGpuHealth').Add_Click({  try { ShowPage 'PgGpuHealth' }  catch {} })
(G 'BtnLatency').Add_Click({    try { ShowPage 'PgLatency' }    catch {} })

# ── SIDEBAR ACTIONS ───────────────────────────────────────────────────────────

# Helper: run DoApply on background thread, disabling a button while running
function Invoke-Apply($btnName) {
    $btn = G $btnName; if ($btn) { $btn.IsEnabled = $false }
    [System.Threading.Tasks.Task]::Run([System.Action]{
        try   { DoApply }
        catch { AddLog "Apply error: $($_.Exception.Message)" '#E01818' }
        finally { UI { $b = G $btnName; if ($b) { $b.IsEnabled = $true } } }
    }) | Out-Null
}

function Invoke-GamingServices {
    $gaming = @('DiagTrack','dmwappushservice','SysMain','WSearch','XboxGip',
                'XblAuthManager','XblGameSave','XboxNetApiSvc','MapsBroker',
                'RetailDemo','WMPNetworkSvc','TabletInputService','Fax',
                'DoSvc','BITS','WbioSrvc','SensorService','SensrSvc')
    foreach ($svc in $gaming) {
        RunC "sc config $svc start= disabled" | Out-Null
        RunC "sc stop $svc" | Out-Null
    }
    AddLog "Gaming services profile applied ($($gaming.Count) services disabled)." '#F59E0B'
    RefreshSvcs
}

$btnSelectAll = G 'BtnSelectAll'
if ($btnSelectAll) {
    $btnSelectAll.Add_Click({
        try {
            foreach ($t in $script:TWEAKS + $script:ARCH_TWEAKS) {
                $t.Sel = ($t.Cat -ne 'rst')
            }
            foreach ($t in $script:PRIVTWEAKS) { $t.Sel = $true }
            FilterTweaks
            $tweakCount = @($script:TWEAKS + $script:ARCH_TWEAKS | Where-Object { $_.Sel }).Count
            $privCount  = @($script:PRIVTWEAKS | Where-Object { $_.Sel }).Count
            AddLog "SELECT ALL: $tweakCount tweaks + $privCount privacy tweaks selected (restore tab excluded)." '#C084FC'
            # Apply gaming services on background thread — avoids freezing UI while 18+ sc commands run
            [System.Threading.Tasks.Task]::Run([System.Action]{
                try { Invoke-GamingServices } catch {}
            }) | Out-Null
        } catch {}
    })
}

$btnGamingMode = G 'BtnGamingMode'
if ($btnGamingMode) {
    $btnGamingMode.Add_Click({
        try {
            $gamingCats = @('cpu','gpu','game','ram','lat','deb')
            foreach ($t in $script:TWEAKS + $script:ARCH_TWEAKS) {
                $t.Sel = ($t.Badge -eq 'SAFE' -and $gamingCats -contains $t.Cat -and $t.Cat -ne 'rst')
            }
            foreach ($t in $script:PRIVTWEAKS) { $t.Sel = $false }
            FilterTweaks
            $count = @($script:TWEAKS + $script:ARCH_TWEAKS | Where-Object { $_.Sel }).Count
            AddLog "GAMING MODE: $count SAFE tweaks selected (CPU/GPU/Game/RAM/Latency/Debloat)." '#F59E0B'
        } catch {}
    })
}

$btnApplyAll = G 'BtnApplyAll'
if ($btnApplyAll) {
    $btnApplyAll.Add_Click({
        try { Invoke-Apply 'BtnApplyAll' } catch {}
    })
}
$btnSafeMode = G 'BtnSafeMode'
if ($btnSafeMode) {
    $btnSafeMode.Add_Click({
        try {
            foreach ($t in $script:TWEAKS + $script:PRIVTWEAKS + $script:ARCH_TWEAKS) { $t.Sel = ($t.Badge -eq 'SAFE') }
            FilterTweaks
            AddLog "SELECT SAFE: all SAFE tweaks selected. Click APPLY SELECTED to apply." '#22C55E'
        } catch {}
    })
}
$btnClearAll = G 'BtnClearAll'
if ($btnClearAll) {
    $btnClearAll.Add_Click({
        try {
            foreach ($t in $script:TWEAKS + $script:PRIVTWEAKS + $script:ARCH_TWEAKS) { $t.Sel = $false }
            foreach ($a in $script:APPS_DATA) { $a.Sel = $false }
            FilterTweaks
            AddLog "Cleared all selections." '#3A3A3A'
        } catch {}
    })
}

# ── TOPBAR TIERED QUICK-APPLY ─────────────────────────────────────────────────
# Selects the tier on the UI thread, then DoApply runs in background (with its
# own EXTREME/NUCLEAR confirmation dialogs).
$btnTopSafe = G 'BtnTopSafe'
if ($btnTopSafe) {
    $btnTopSafe.Add_Click({
        try {
            Select-Tier 'SAFE'
            FilterTweaks
            $c = @($script:TWEAKS + $script:ARCH_TWEAKS | Where-Object { $_.Sel }).Count
            AddLog "APPLY SAFE: $c safe tweaks selected - applying..." '#22C55E'
            Invoke-Apply 'BtnTopSafe'
        } catch {}
    })
}
$btnTopExtreme = G 'BtnTopExtreme'
if ($btnTopExtreme) {
    $btnTopExtreme.Add_Click({
        try {
            Select-Tier 'EXTREME'
            FilterTweaks
            $c = @($script:TWEAKS + $script:ARCH_TWEAKS | Where-Object { $_.Sel }).Count
            AddLog "APPLY EXTREME: $c safe+extreme tweaks selected - applying..." '#7c3aed'
            Invoke-Apply 'BtnTopExtreme'
        } catch {}
    })
}
$btnTopNuclear = G 'BtnTopNuclear'
if ($btnTopNuclear) {
    $btnTopNuclear.Add_Click({
        try {
            Select-Tier 'NUCLEAR'
            FilterTweaks
            $c = @($script:TWEAKS + $script:ARCH_TWEAKS | Where-Object { $_.Sel }).Count
            AddLog "APPLY NUCLEAR: $c tweaks selected (all tiers) - applying..." '#EF4444'
            Invoke-Apply 'BtnTopNuclear'
        } catch {}
    })
}
$btnTopRevert = G 'BtnTopRevert'
if ($btnTopRevert) {
    $btnTopRevert.Add_Click({
        try {
            $confirm = [System.Windows.MessageBox]::Show(
                "Revert EVERY applied tweak back to Windows defaults?`n`nThis runs the revert command for every tweak currently marked as applied.",
                "VOIDTUNE - Revert All",
                [System.Windows.MessageBoxButton]::YesNo,
                [System.Windows.MessageBoxImage]::Question)
            if ($confirm -ne 'Yes') { return }
            $btn = G 'BtnTopRevert'; if ($btn) { $btn.IsEnabled = $false }
            [System.Threading.Tasks.Task]::Run([System.Action]{
                try   { Invoke-RevertAll }
                catch { AddLog "Revert error: $($_.Exception.Message)" '#E01818' }
                finally { UI { $b = G 'BtnTopRevert'; if ($b) { $b.IsEnabled = $true } } }
            }) | Out-Null
        } catch {}
    })
}

# ── DISABLE TWEAK (revert applied tweak) ──────────────────────────────────────
# AddHandler on TweakList to catch bubbled Button.Click from DataTemplate
$tl = G 'TweakList'
if ($tl) {
    $tl.AddHandler(
        [System.Windows.Controls.Button]::ClickEvent,
        [System.Windows.RoutedEventHandler]{
            param($s, $e)
            $btn = $e.OriginalSource
            if (-not ($btn -is [System.Windows.Controls.Button])) { return }
            if ($btn.Name -ne 'BtnDisableTweak') { return }
            $e.Handled = $true
            $id  = $btn.Tag
            $all = $script:TWEAKS + $script:PRIVTWEAKS + $script:ARCH_TWEAKS
            $tw  = $all | Where-Object { $_.Id -eq $id } | Select-Object -First 1
            if (-not $tw) { return }
            try {
                $hasCmd = -not [string]::IsNullOrEmpty($tw.RevertCmd)
                $msg = if ($hasCmd) {
                    "Revert: $($tw.Name)?`n`nThis will run the revert command to restore the original setting."
                } else {
                    "Mark '$($tw.Name)' as unapplied?`n`n" +
                    "⚠  This tweak has NO automatic revert command.`n`n" +
                    "The actual system setting will NOT change — only VOIDTUNE's tracking will be updated.`n`n" +
                    "To fully undo this setting use REVERT ALL with a registry backup, or change it manually."
                }
                $icon = if ($hasCmd) { [System.Windows.MessageBoxImage]::Question } else { [System.Windows.MessageBoxImage]::Warning }
                $confirm = [System.Windows.MessageBox]::Show($msg, "VOIDTUNE — Revert Tweak", [System.Windows.MessageBoxButton]::YesNo, $icon)
                if ($confirm -ne 'Yes') { return }

                if ($hasCmd) {
                    AddLog "Reverting: $($tw.Name)" '#F59E0B'
                    $r = Exec-Cmd $tw.RevertCmd
                    if ($r.OK) { AddLog "  ✓ Reverted OK" '#22C55E' }
                    else       { AddLog "  ✗ Revert failed: $($r.Out.Split("`n")[0])" '#E01818' }
                } else {
                    AddLog "⚠ $($tw.Name) — marked unapplied (no revert command)" '#F59E0B'
                }

                $tw.Applied     = $false
                $tw.Sel         = $false
                $script:applied = [math]::Max(0, $script:applied - 1)
                $da = G 'DashApplied'; if ($da) { $da.Text = $script:applied }
                Save-TweakState
                FilterTweaks
            } catch { AddLog "Disable error: $($_.Exception.Message)" '#E01818' }
        }
    )
}
(G 'BtnLogClear').Add_Click({ try { $lo = G 'LogOut'; if ($lo) { $lo.Text = '' } } catch {} })

# ── SERVICES ──────────────────────────────────────────────────────────────────
# Gaming mode - disable non-essential services
(G 'BtnSvcGaming').Add_Click({
    try {
        $gaming = @('DiagTrack','dmwappushservice','SysMain','WSearch','XboxGip',
                    'XblAuthManager','XblGameSave','XboxNetApiSvc','MapsBroker',
                    'RetailDemo','WMPNetworkSvc','TabletInputService','Fax',
                    'DoSvc','BITS','WbioSrvc','SensorService','SensrSvc')
        AddLog "Applying GAMING profile..." '#22C55E'
        foreach ($svc in $gaming) {
            RunC "sc config $svc start= disabled" | Out-Null
            RunC "sc stop $svc" | Out-Null
        }
        AddLog "GAMING profile applied. $($gaming.Count) services disabled." '#22C55E'
        RefreshSvcs
    } catch { AddLog "Profile error: $($_.Exception.Message)" '#E01818' }
})

# Normal mode - restore safe defaults
(G 'BtnSvcNormal').Add_Click({
    try {
        $restore = @{
            'SysMain'='auto'; 'WSearch'='auto'; 'bthserv'='auto'
            'Spooler'='auto'; 'W32Time'='auto'; 'BITS'='demand'
        }
        $disable = @('DiagTrack','dmwappushservice','XblGameSave','XboxNetApiSvc',
                     'TabletInputService','Fax','MapsBroker','RetailDemo','WMPNetworkSvc',
                     'DoSvc','WbioSrvc','SensorService','SensrSvc')
        AddLog "Applying NORMAL profile..." '#38BDF8'
        foreach ($kv in $restore.GetEnumerator()) {
            RunC "sc config $($kv.Key) start= $($kv.Value)" | Out-Null
            RunC "sc start $($kv.Key)" | Out-Null
        }
        foreach ($svc in $disable) {
            RunC "sc config $svc start= disabled" | Out-Null
            RunC "sc stop $svc" | Out-Null
        }
        AddLog "NORMAL profile applied." '#38BDF8'
        RefreshSvcs
    } catch { AddLog "Profile error: $($_.Exception.Message)" '#E01818' }
})

# Disable all - nuke everything in the list
(G 'BtnSvcDisableAll').Add_Click({
    try {
        $confirm = [System.Windows.MessageBox]::Show(
            "Disable ALL services in the list?`nThis will disable telemetry, Xbox, search, print spooler and more.`n`nA registry backup is recommended first.",
            "VOIDTUNE - Disable All Services",
            [System.Windows.MessageBoxButton]::YesNo,
            [System.Windows.MessageBoxImage]::Warning)
        if ($confirm -ne 'Yes') { return }
        AddLog "Disabling all listed services..." '#F59E0B'
        $count = 0
        foreach ($s in $script:SVCS_DATA) {
            RunC "sc config $($s.Name) start= disabled" | Out-Null
            RunC "sc stop $($s.Name)" | Out-Null
            $count++
        }
        AddLog "Disabled $count services." '#22C55E'
        RefreshSvcs
    } catch { AddLog "Disable all error: $($_.Exception.Message)" '#E01818' }
})

(G 'BtnSvcRefresh').Add_Click({ try { RefreshSvcs } catch {} })
(G 'SvcList').Add_PreviewMouseLeftButtonUp({
    param($s, $e)
    try {
        $el = $e.OriginalSource; $i = 0
        while ($el -and $el -isnot [System.Windows.Controls.Button] -and $i -lt 10) {
            try { $el = [System.Windows.Media.VisualTreeHelper]::GetParent($el) } catch { break }
            $i++
        }
        if ($el -and $el -is [System.Windows.Controls.Button] -and $el.Tag) {
            $svcName = $el.Tag
            $deps = $script:SVC_DEPS[$svcName]
            if ($el.Content -eq 'STOP' -and $deps -and $deps.Count -gt 0) {
                $depList = $deps -join ', '
                $confirm = [System.Windows.MessageBox]::Show(
                    "Stopping '$svcName' may affect: $depList`nContinue?",
                    "VOIDTUNE - Dependency Warning",
                    [System.Windows.MessageBoxButton]::YesNo,
                    [System.Windows.MessageBoxImage]::Warning)
                if ($confirm -ne 'Yes') { return }
            }
            if ($el.Content -eq 'STOP') { RunC "sc stop $svcName"  | Out-Null }
            else                        { RunC "sc start $svcName" | Out-Null }
            Start-Sleep -Milliseconds 600
            RefreshSvcs
        }
    } catch {}
})

# ── STARTUP ───────────────────────────────────────────────────────────────────
(G 'BtnStartupRefresh').Add_Click({ try { RefreshStartup } catch {} })
(G 'StartupList').Add_PreviewMouseLeftButtonUp({
    param($s, $e)
    try {
        $el = $e.OriginalSource; $i = 0
        while ($el -and $el -isnot [System.Windows.Controls.Button] -and $i -lt 10) {
            try { $el = [System.Windows.Media.VisualTreeHelper]::GetParent($el) } catch { break }
            $i++
        }
        if ($el -and $el -is [System.Windows.Controls.Button] -and $el.Tag) {
            $k = 'HKCU:\Software\Microsoft\Windows\CurrentVersion\Explorer\StartupApproved\Run'
            if (!(Test-Path $k)) { New-Item $k -Force | Out-Null }
            Set-ItemProperty -Path $k -Name $el.Tag -Value ([byte[]](0x03,0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00)) -EA SilentlyContinue
            AddLog "Disabled startup: $($el.Tag)" '#22C55E'
            RefreshStartup
        }
    } catch {}
})

# ── PROCESS MONITOR ───────────────────────────────────────────────────────────
(G 'BtnProcRefresh').Add_Click({ try { RefreshProc } catch {} })

(G 'BtnKillBloat').Add_Click({
    try {
        $confirm = [System.Windows.MessageBox]::Show(
            "Kill all known bloat processes?`n`nThis will close: OneDrive, Cortana, Widgets, Xbox Game Bar, Teams background, Adobe ARM, and other non-essential Microsoft/vendor processes.`n`nYour browser, games and apps will NOT be closed.",
            "VOIDTUNE - Kill Bloat",
            [System.Windows.MessageBoxButton]::YesNo,
            [System.Windows.MessageBoxImage]::Warning)
        if ($confirm -ne 'Yes') { return }
        AddLog "Killing bloat processes..." '#F59E0B'
        Remove-Bloat
    } catch {}
})
(G 'ProcList').Add_PreviewMouseLeftButtonUp({
    param($s, $e)
    try {
        $el = $e.OriginalSource; $i = 0
        while ($el -and $el -isnot [System.Windows.Controls.Button] -and $i -lt 10) {
            try { $el = [System.Windows.Media.VisualTreeHelper]::GetParent($el) } catch { break }
            $i++
        }
        if ($el -and $el -is [System.Windows.Controls.Button] -and $el.Tag) {
            $pid2  = [int]$el.Tag
            $proc  = Get-Process -Id $pid2 -EA SilentlyContinue
            $pname = if ($proc) { $proc.ProcessName } else { 'unknown' }
            $blocked = @('csrss','winlogon','lsass','smss','wininit','services','System','Idle','dwm','svchost')
            if ($blocked -contains $pname) {
                [System.Windows.MessageBox]::Show(
                    "Cannot kill '$pname' - critical Windows process.`nTerminating it would cause a BSOD.",
                    "VOIDTUNE - Protected Process",
                    [System.Windows.MessageBoxButton]::OK,
                    [System.Windows.MessageBoxImage]::Warning)
                return
            }
            if ([System.Windows.MessageBox]::Show("Kill process: $pname (PID $pid2)?","VOIDTUNE",[System.Windows.MessageBoxButton]::YesNo) -eq 'Yes') {
                Stop-Process -Id $pid2 -Force -EA SilentlyContinue
                AddLog "Killed: $pname (PID $pid2)" '#22C55E'
                Start-Sleep -Milliseconds 400
                RefreshProc
            }
        }
    } catch {}
})

# ── DASHBOARD & DIAG ──────────────────────────────────────────────────────────
(G 'BtnRefreshDash').Add_Click({ try { RefreshDash } catch {} })
(G 'BtnDiagRefresh').Add_Click({ try { RefreshDiag } catch {} })

# ── QUICK ACTIONS ─────────────────────────────────────────────────────────────
(G 'BtnQFlushDns').Add_Click({
    try {
        $r  = RunC 'ipconfig /flushdns'
        $ql = G 'QuickActLog'; if ($ql) { $ql.Text = if ($r.OK) { 'DNS cache flushed.' } else { 'DNS flush failed.' } }
        AddLog "Quick Action: Flush DNS -$(if($r.OK){'OK'}else{'FAIL'})" '#22C55E'
    } catch {}
})
(G 'BtnQClearTemp').Add_Click({
    try {
        RunC 'del /q /f /s "%TEMP%\*" 2>nul' | Out-Null
        RunC 'del /q /f /s "%SystemRoot%\Temp\*" 2>nul' | Out-Null
        $ql = G 'QuickActLog'; if ($ql) { $ql.Text = "Temp files cleared." }
        AddLog "Quick Action: Clear Temp -OK" '#22C55E'
    } catch {}
})
(G 'BtnQRestartExp').Add_Click({
    try {
        RunC 'taskkill /f /im explorer.exe' | Out-Null
        Start-Sleep -Milliseconds 800
        Start-Process explorer.exe
        $ql = G 'QuickActLog'; if ($ql) { $ql.Text = "Explorer restarted." }
        AddLog "Quick Action: Restart Explorer -OK" '#22C55E'
    } catch {}
})
(G 'BtnQRecycleBin').Add_Click({
    try {
        Clear-RecycleBin -Force -EA SilentlyContinue
        $ql = G 'QuickActLog'; if ($ql) { $ql.Text = "Recycle Bin emptied." }
        AddLog "Quick Action: Empty Recycle Bin -OK" '#22C55E'
    } catch {}
})
(G 'BtnQDefrag').Add_Click({
    try {
        $ql = G 'QuickActLog'; if ($ql) { $ql.Text = "Defrag started in background..." }
        Start-Process -FilePath 'defrag.exe' -ArgumentList 'C: /U /V' -WindowStyle Hidden
        AddLog "Quick Action: Defrag C: started" '#22C55E'
    } catch {}
})

# ── BACKUP & RESTORE ──────────────────────────────────────────────────────────
(G 'BtnBkRefresh').Add_Click({ try { RefreshBackups } catch {} })
(G 'BtnMkBackup').Add_Click({
    try {
        $n = MakeBackup 'manual'
        AddLog "Backup created: $n" '#22C55E'
        RefreshBackups
    } catch { AddLog "Backup error: $($_.Exception.Message)" '#E01818' }
})
(G 'BtnRestorePt').Add_Click({
    try {
        # Enable system restore on C: first, then create the checkpoint
        RunPS { Enable-ComputerRestore -Drive 'C:\' -ErrorAction SilentlyContinue } | Out-Null
        $r2 = RunPS { Checkpoint-Computer -Description 'VOIDTUNE Backup' -RestorePointType MODIFY_SETTINGS -ErrorAction Stop }
        if ($r2.OK) { AddLog "Windows restore point created." '#22C55E' }
        else        { AddLog "Restore point failed. Try: Control Panel > System Protection > Enable on C: -- $($r2.Out)" '#E01818' }
    } catch { AddLog "Restore point error: $($_.Exception.Message)" '#E01818' }
})
(G 'BackupList').Add_PreviewMouseLeftButtonUp({
    param($s, $e)
    try {
        $el = $e.OriginalSource; $i = 0
        while ($el -and $el -isnot [System.Windows.Controls.Button] -and $i -lt 10) {
            try { $el = [System.Windows.Media.VisualTreeHelper]::GetParent($el) } catch { break }
            $i++
        }
        if ($el -and $el -is [System.Windows.Controls.Button] -and $el.Tag) {
            $fname = $el.Tag
            $f = Join-Path $script:BACKUPS $fname
            if (-not (Test-Path $f)) { AddLog "File not found: $fname" '#E01818'; return }

            $msg = "Restore from:`n$fname`n`n" +
                   "This will:`n" +
                   "  · Import registry settings`n" +
                   "  · Restore service startup states`n" +
                   "  · Reset power plan to backup state`n" +
                   "  · Clear VOIDTUNE's applied tweak tracking`n`n" +
                   "A system restart is recommended after.`n`n" +
                   "Continue?"
            if ([System.Windows.MessageBox]::Show($msg, "VOIDTUNE — Full Restore",
                [System.Windows.MessageBoxButton]::YesNo,
                [System.Windows.MessageBoxImage]::Warning) -ne 'Yes') { return }

            Show-ApplyOverlay 'RESTORING BACKUP'
            Update-ApplyOverlay "Importing $fname..." 5 ''
            $fn = $fname
            [System.Threading.Tasks.Task]::Run([System.Action]{
                try {
                    $ok = Restore-BackupFull $fn
                    Update-ApplyOverlay (if ($ok) { 'Restore complete.' } else { 'Restore had errors.' }) 100 ''
                    Start-Sleep -Milliseconds 700
                    Hide-ApplyOverlay
                    UI { RefreshBackups }
                } catch {
                    AddLog "Restore error: $($_.Exception.Message)" '#E01818'
                    Hide-ApplyOverlay
                }
            }) | Out-Null
        }
    } catch {}
})

$btnVerify = G 'BtnVerifyState'
if ($btnVerify) {
    $btnVerify.Add_Click({
        try {
            [System.Threading.Tasks.Task]::Run([System.Action]{
                try { Invoke-VerifyState } catch { AddLog "Verify error: $($_.Exception.Message)" '#E01818' }
            }) | Out-Null
        } catch {}
    })
}

# ── BENCHMARKS ────────────────────────────────────────────────────────────────
function UpdateBH {
    try { UI { $bh = G 'BenchHist'; if ($bh) { $bh.Text = ($script:benchLog | Select-Object -Last 8) -join "`n" } } } catch {}
}
$script:allBenchBtns = @('BtnBDisk','BtnBDiskR','BtnBRam','BtnBCpu','BtnBNet','BtnBDns','BtnBRunAll')
function LockBench   { $script:benchRunning = $true;  foreach ($bn in $script:allBenchBtns) { $b = G $bn; if ($b) { UI { $b.IsEnabled = $false } } } }
function UnlockBench { $script:benchRunning = $false; foreach ($bn in $script:allBenchBtns) { $b = G $bn; if ($b) { UI { $b.IsEnabled = $true  } } } }

(G 'BtnBDisk').Add_Click({
    try {
        if ($script:benchRunning) { return }
        LockBench
        $dv = G 'BDiskVal'; UI { if ($dv) { $dv.Text = '...' } }
        [System.Threading.Tasks.Task]::Run([System.Action]{
            try {
                $t  = "$env:TEMP\vt_bench_w.tmp"
                $sw = [System.Diagnostics.Stopwatch]::StartNew()
                [System.IO.File]::WriteAllBytes($t, (New-Object byte[](52428800)))
                $sw.Stop()
                Remove-Item $t -Force -EA SilentlyContinue
                $v = [math]::Round(50 / $sw.Elapsed.TotalSeconds)
                UI {
                    $dv2 = G 'BDiskVal'; if ($dv2) { $dv2.Text = $v }
                    $db  = G 'BDiskBar'; if ($db)  { $db.Value  = [math]::Min(100, $v / 6) }
                }
                $script:benchLog += "DISK WRITE: ${v} MB/s  $(Get-Date -f HH:mm)"; UpdateBH
            } catch { UI { $dv2 = G 'BDiskVal'; if ($dv2) { $dv2.Text = 'ERR' } } }
            UnlockBench
        }) | Out-Null
    } catch { UnlockBench }
})
(G 'BtnBDiskR').Add_Click({
    try {
        if ($script:benchRunning) { return }
        LockBench
        $rv = G 'BDiskRVal'; UI { if ($rv) { $rv.Text = '...' } }
        [System.Threading.Tasks.Task]::Run([System.Action]{
            try {
                $t = "$env:TEMP\vt_bench_r.tmp"
                [System.IO.File]::WriteAllBytes($t, (New-Object byte[] 52428800))
                $sw = [System.Diagnostics.Stopwatch]::StartNew()
                [System.IO.File]::ReadAllBytes($t) | Out-Null
                $sw.Stop()
                Remove-Item $t -Force -EA SilentlyContinue
                $v = [math]::Round(50 / $sw.Elapsed.TotalSeconds)
                UI {
                    $rv2 = G 'BDiskRVal'; if ($rv2) { $rv2.Text = $v }
                    $rb  = G 'BDiskRBar'; if ($rb)  { $rb.Value  = [math]::Min(100, $v / 6) }
                }
                $script:benchLog += "DISK READ: ${v} MB/s  $(Get-Date -f HH:mm)"; UpdateBH
            } catch { UI { $rv2 = G 'BDiskRVal'; if ($rv2) { $rv2.Text = 'ERR' } } }
            UnlockBench
        }) | Out-Null
    } catch { UnlockBench }
})
(G 'BtnBRam').Add_Click({
    try {
        if ($script:benchRunning) { return }
        LockBench
        $rv = G 'BRamVal'; UI { if ($rv) { $rv.Text = '...' } }
        [System.Threading.Tasks.Task]::Run([System.Action]{
            try {
                $ms  = New-Object System.IO.MemoryStream 134217728
                $buf = New-Object byte[] 1048576
                $sw  = [System.Diagnostics.Stopwatch]::StartNew()
                for ($ii = 0; $ii -lt 128; $ii++) { $ms.Write($buf, 0, $buf.Length) }
                $sw.Stop(); $ms.Dispose()
                $v = [math]::Round(128 / $sw.Elapsed.TotalSeconds)
                UI {
                    $rv2 = G 'BRamVal'; if ($rv2) { $rv2.Text = $v }
                    $rb  = G 'BRamBar'; if ($rb)  { $rb.Value  = [math]::Min(100, $v / 100) }
                }
                $script:benchLog += "RAM: ${v} MB/s  $(Get-Date -f HH:mm)"; UpdateBH
            } catch { UI { $rv2 = G 'BRamVal'; if ($rv2) { $rv2.Text = 'ERR' } } }
            UnlockBench
        }) | Out-Null
    } catch { UnlockBench }
})
(G 'BtnBCpu').Add_Click({
    try {
        if ($script:benchRunning) { return }
        LockBench
        $cv = G 'BCpuVal'; UI { if ($cv) { $cv.Text = '...' } }
        [System.Threading.Tasks.Task]::Run([System.Action]{
            try {
                $sw = [System.Diagnostics.Stopwatch]::StartNew(); $n = 0
                for ($x = 2; $x -lt 100000; $x++) {
                    $p = $true
                    for ($y = 2; $y -le [math]::Sqrt($x); $y++) { if ($x % $y -eq 0) { $p = $false; break } }
                    if ($p) { $n++ }
                }
                $sw.Stop()
                $v = [math]::Round($n / $sw.Elapsed.TotalSeconds)
                UI {
                    $cv2 = G 'BCpuVal'; if ($cv2) { $cv2.Text = $v }
                    $cb  = G 'BCpuBar'; if ($cb)  { $cb.Value  = [math]::Min(100, $v / 1000) }
                }
                $script:benchLog += "CPU: ${v} primes/sec  $(Get-Date -f HH:mm)"; UpdateBH
            } catch { UI { $cv2 = G 'BCpuVal'; if ($cv2) { $cv2.Text = 'ERR' } } }
            UnlockBench
        }) | Out-Null
    } catch { UnlockBench }
})
(G 'BtnBNet').Add_Click({
    try {
        if ($script:benchRunning) { return }
        LockBench
        $nv = G 'BNetVal'; UI { if ($nv) { $nv.Text = '...' } }
        [System.Threading.Tasks.Task]::Run([System.Action]{
            try {
                $times = @()
                1..5 | ForEach-Object {
                    $sw = [System.Diagnostics.Stopwatch]::StartNew()
                    try { (New-Object Net.NetworkInformation.Ping).Send('8.8.8.8', 1000) | Out-Null } catch {}
                    $sw.Stop(); $times += $sw.Elapsed.TotalMilliseconds
                }
                $v = [math]::Round(($times | Measure-Object -Average).Average, 1)
                UI {
                    $nv2 = G 'BNetVal'; if ($nv2) { $nv2.Text = $v }
                    $nb  = G 'BNetBar'; if ($nb)  { $nb.Value  = [math]::Min(100, [math]::Max(0, 100 - $v)) }
                }
                $script:benchLog += "NET: ${v}ms  $(Get-Date -f HH:mm)"; UpdateBH
            } catch { UI { $nv2 = G 'BNetVal'; if ($nv2) { $nv2.Text = 'ERR' } } }
            UnlockBench
        }) | Out-Null
    } catch { UnlockBench }
})
(G 'BtnBDns').Add_Click({
    try {
        if ($script:benchRunning) { return }
        LockBench
        $dv = G 'BDnsVal'; UI { if ($dv) { $dv.Text = '...' } }
        [System.Threading.Tasks.Task]::Run([System.Action]{
            try {
                $hosts = @('google.com','cloudflare.com','github.com','microsoft.com','youtube.com')
                $times = @()
                foreach ($h in $hosts) {
                    $sw = [System.Diagnostics.Stopwatch]::StartNew()
                    try { [System.Net.Dns]::GetHostEntry($h) | Out-Null } catch {}
                    $sw.Stop(); $times += $sw.Elapsed.TotalMilliseconds
                }
                $v = [math]::Round(($times | Measure-Object -Average).Average, 1)
                UI {
                    $dv2 = G 'BDnsVal'; if ($dv2) { $dv2.Text = $v }
                    $db  = G 'BDnsBar'; if ($db)  { $db.Value  = [math]::Min(100, [math]::Max(0, 100 - $v)) }
                }
                $script:benchLog += "DNS: ${v}ms avg  $(Get-Date -f HH:mm)"; UpdateBH
            } catch { UI { $dv2 = G 'BDnsVal'; if ($dv2) { $dv2.Text = 'ERR' } } }
            UnlockBench
        }) | Out-Null
    } catch { UnlockBench }
})
(G 'BtnBRunAll').Add_Click({
    try {
        if ($script:benchRunning) { return }
        LockBench
        AddLog "Running all benchmarks..." '#38BDF8'
        [System.Threading.Tasks.Task]::Run([System.Action]{
            try {
                # Disk Write
                try {
                    $t = "$env:TEMP\vt_bench_w.tmp"
                    $sw = [System.Diagnostics.Stopwatch]::StartNew()
                    [System.IO.File]::WriteAllBytes($t, (New-Object byte[](52428800))); $sw.Stop()
                    Remove-Item $t -Force -EA SilentlyContinue
                    $v = [math]::Round(50/$sw.Elapsed.TotalSeconds)
                    UI { $e = G 'BDiskVal'; if($e){$e.Text=$v}; $e2 = G 'BDiskBar'; if($e2){$e2.Value=[math]::Min(100,$v/6)} }
                    $script:benchLog += "DISK WRITE: ${v} MB/s  $(Get-Date -f HH:mm)"
                } catch {}
                # Disk Read
                try {
                    $t = "$env:TEMP\vt_bench_r.tmp"
                    [System.IO.File]::WriteAllBytes($t, (New-Object byte[] 52428800))
                    $sw = [System.Diagnostics.Stopwatch]::StartNew()
                    [System.IO.File]::ReadAllBytes($t) | Out-Null; $sw.Stop()
                    Remove-Item $t -Force -EA SilentlyContinue
                    $v = [math]::Round(50/$sw.Elapsed.TotalSeconds)
                    UI { $e = G 'BDiskRVal'; if($e){$e.Text=$v}; $e2 = G 'BDiskRBar'; if($e2){$e2.Value=[math]::Min(100,$v/6)} }
                    $script:benchLog += "DISK READ: ${v} MB/s  $(Get-Date -f HH:mm)"
                } catch {}
                # RAM
                try {
                    $ms = New-Object System.IO.MemoryStream 134217728; $buf = New-Object byte[] 1048576
                    $sw = [System.Diagnostics.Stopwatch]::StartNew()
                    for ($ii = 0; $ii -lt 128; $ii++) { $ms.Write($buf,0,$buf.Length) }
                    $sw.Stop(); $ms.Dispose()
                    $v = [math]::Round(128/$sw.Elapsed.TotalSeconds)
                    UI { $e = G 'BRamVal'; if($e){$e.Text=$v}; $e2 = G 'BRamBar'; if($e2){$e2.Value=[math]::Min(100,$v/100)} }
                    $script:benchLog += "RAM: ${v} MB/s  $(Get-Date -f HH:mm)"
                } catch {}
                # CPU
                try {
                    $sw = [System.Diagnostics.Stopwatch]::StartNew(); $n = 0
                    for ($x=2;$x -lt 100000;$x++){$p=$true;for($y=2;$y -le [math]::Sqrt($x);$y++){if($x%$y-eq 0){$p=$false;break}};if($p){$n++}}
                    $sw.Stop(); $v=[math]::Round($n/$sw.Elapsed.TotalSeconds)
                    UI { $e = G 'BCpuVal'; if($e){$e.Text=$v}; $e2 = G 'BCpuBar'; if($e2){$e2.Value=[math]::Min(100,$v/1000)} }
                    $script:benchLog += "CPU: ${v} primes/sec  $(Get-Date -f HH:mm)"
                } catch {}
                # DNS
                try {
                    $hosts = @('google.com','cloudflare.com','github.com','microsoft.com','youtube.com'); $times = @()
                    foreach ($h in $hosts) { $sw=[System.Diagnostics.Stopwatch]::StartNew(); try{[System.Net.Dns]::GetHostEntry($h)|Out-Null}catch{}; $sw.Stop(); $times+=$sw.Elapsed.TotalMilliseconds }
                    $v = [math]::Round(($times|Measure-Object -Average).Average,1)
                    UI { $e = G 'BDnsVal'; if($e){$e.Text=$v}; $e2 = G 'BDnsBar'; if($e2){$e2.Value=[math]::Min(100,[math]::Max(0,100-$v))} }
                    $script:benchLog += "DNS: ${v}ms avg  $(Get-Date -f HH:mm)"
                } catch {}
                # NET
                try {
                    $times = @(); 1..5 | ForEach-Object { $sw=[System.Diagnostics.Stopwatch]::StartNew(); try{(New-Object Net.NetworkInformation.Ping).Send('8.8.8.8',1000)|Out-Null}catch{}; $sw.Stop(); $times+=$sw.Elapsed.TotalMilliseconds }
                    $v = [math]::Round(($times|Measure-Object -Average).Average,1)
                    UI { $e = G 'BNetVal'; if($e){$e.Text=$v}; $e2 = G 'BNetBar'; if($e2){$e2.Value=[math]::Min(100,[math]::Max(0,100-$v))} }
                    $script:benchLog += "NET: ${v}ms  $(Get-Date -f HH:mm)"
                } catch {}
                UpdateBH
                AddLog "All benchmarks complete." '#22C55E'
            } finally { UnlockBench }
        }) | Out-Null
    } catch { UnlockBench }
})

# ── SCRIPT RUNNER ─────────────────────────────────────────────────────────────
$scriptRun = {
    try {
        $si = G 'ScriptIn'; $so = G 'ScriptOut'; $st = G 'ScriptType'
        if (-not $si) { return }
        $cmd = $si.Text.Trim(); if (-not $cmd) { return }
        $isPS = $st -and $st.SelectedIndex -eq 1
        if ($so) { $so.Text += "$(if($isPS){'PS'}else{'CMD'})> $cmd`n" }
        if ($isPS) {
            $r = RunPS ([scriptblock]::Create($cmd))
            if ($so) { $so.Text += "$(if($r.OK){'[OK]'}else{'[FAIL]'}) $($r.Out.Substring(0,[math]::Min(800,$r.Out.Length)))`n---`n" }
        } else {
            $r = RunC $cmd
            if ($so) { $so.Text += "$(if($r.OK){'[OK]'}else{'[FAIL]'}) $($r.Out.Substring(0,[math]::Min(800,$r.Out.Length)))`n---`n" }
        }
        if ($so) { $so.ScrollToEnd() }
    } catch {}
}

(G 'BtnRunScript').Add_Click($scriptRun)

# Enter key runs script when focus is in the input box
$siEl = G 'ScriptIn'
if ($siEl) {
    $siEl.Add_KeyDown({
        param($s, $e)
        if ($e.Key -eq [System.Windows.Input.Key]::Return -and
            [System.Windows.Input.Keyboard]::Modifiers -eq [System.Windows.Input.ModifierKeys]::Control) {
            & $scriptRun
        }
    })
}

(G 'BtnClearScript').Add_Click({
    try {
        $si = G 'ScriptIn';  if ($si) { $si.Text = '' }
        $so = G 'ScriptOut'; if ($so) { $so.Text = '' }
    } catch {}
})

# ── PERSONALIZE ───────────────────────────────────────────────────────────────
$script:wallpaperPending = ''

$personalizeIds = @(
    'DarkMode','Transparency','ClassicAltTab','NoLoginBlur','OldContextMenu','NoRoundCorners',
    'AeroPeek','AeroTitlebar','AeroGlass','AeroAnimations','AeroSnapAssist','AeroDragFull',
    'SmallIcons','HideSearch','HideWidgets','HideTaskView','CompactMode','ClearType'
)
foreach ($toggleId in $personalizeIds) {
    $btn = G "PBtn_$toggleId"
    if ($btn) {
        $btn.Add_Click({
            param($s, $e)
            try {
                $id       = $s.Tag
                $current  = Get-PersonalizeState $id
                $newState = -not $current
                AddLog "Personalize: $id -> $(if($newState){'ON'}else{'OFF'})" '#38BDF8'
                $ok = Set-PersonalizeTweak $id $newState
                if ($ok) { AddLog "  OK" '#22C55E' } else { AddLog "  FAILED" '#E01818' }
                RefreshPersonalize
            } catch {}
        })
    }
}

$accentPanel = G 'PAccentPanel'
if ($accentPanel) {
    $accentPanel.AddHandler(
        [System.Windows.Controls.Button]::ClickEvent,
        [System.Windows.RoutedEventHandler]{
            param($s, $e)
            $hex = $e.OriginalSource.Tag
            if ($hex -and $hex.StartsWith('#')) {
                Set-AccentColor $hex
                Set-PersonalizeTweak 'AeroTitlebar' $true | Out-Null
                RefreshPersonalize
            }
        }
    )
}

(G 'PBtnRefresh').Add_Click({ try { RefreshPersonalize } catch {} })
(G 'PBtnRestartExplorer').Add_Click({
    try {
        AddLog "Restarting Explorer for taskbar changes..." '#888888'
        RunC 'taskkill /f /im explorer.exe' | Out-Null
        Start-Sleep -Milliseconds 800
        Start-Process explorer.exe
        AddLog "Explorer restarted. Taskbar changes applied." '#22C55E'
        Start-Sleep -Milliseconds 500
        RefreshPersonalize
    } catch { AddLog "Explorer restart error: $($_.Exception.Message)" '#E01818' }
})

(G 'PBtnColorMgmt').Add_Click({    try { Start-Process 'colorcpl.exe'; AddLog "Opened Color Management." '#F59E0B' } catch {} })
(G 'PBtnCalibration').Add_Click({  try { Start-Process 'dccw.exe';     AddLog "Opened Display Calibration Wizard." '#F59E0B' } catch {} })
(G 'PBtnClearTypeTune').Add_Click({ try { Start-Process 'cttune.exe';  AddLog "Opened ClearType Tuner." '#F59E0B' } catch {} })
(G 'PBtnNightLight').Add_Click({   try { Start-Process 'ms-settings:nightlight'; AddLog "Opened Night Light settings." '#F59E0B' } catch {} })

(G 'PBtnWallpaperPick').Add_Click({
    try {
        $dlg = New-Object System.Windows.Forms.OpenFileDialog
        $dlg.Filter = "Image files (*.jpg;*.jpeg;*.png;*.bmp)|*.jpg;*.jpeg;*.png;*.bmp|All files (*.*)|*.*"
        $dlg.Title  = "Select Wallpaper"
        if ($dlg.ShowDialog() -eq 'OK') {
            $script:wallpaperPending = $dlg.FileName
            $wl = G 'PWallpaperPath'; if ($wl) { $wl.Text = $dlg.FileName }
            AddLog "Wallpaper selected: $($dlg.FileName)" '#38BDF8'
        }
    } catch {}
})
(G 'PBtnWallpaperApply').Add_Click({
    try {
        if (-not $script:wallpaperPending) { AddLog "No wallpaper selected. Use BROWSE first." '#F59E0B'; return }
        $path = $script:wallpaperPending
        if (-not (Test-Path $path)) { AddLog "Wallpaper file not found: $path" '#E01818'; return }
        RunC "reg add `"HKCU\Control Panel\Desktop`" /v Wallpaper      /t REG_SZ /d `"$path`" /f" | Out-Null
        RunC "reg add `"HKCU\Control Panel\Desktop`" /v WallpaperStyle /t REG_SZ /d 10       /f" | Out-Null
        RunC 'rundll32.exe user32.dll,UpdatePerUserSystemParameters' | Out-Null
        AddLog "Wallpaper applied: $path" '#22C55E'
        $script:wallpaperPending = ''
        $wl = G 'PWallpaperPath'; if ($wl) { $wl.Text = '' }
        RefreshPersonalize
    } catch { AddLog "Wallpaper error: $($_.Exception.Message)" '#E01818' }
})

# ── DRIVERS PAGE ──────────────────────────────────────────────────────────────
(G 'BtnDriverRefresh').Add_Click({ try { RefreshDrivers } catch {} })

$drFilter = G 'DrFilter'
if ($drFilter) {
    $drFilter.Add_TextChanged({ try { RefreshDrivers } catch {} })
}

(G 'BtnDriverExport').Add_Click({
    try {
        $drivers = Get-DriverList
        $ts   = Get-Date -Format 'yyyyMMdd_HHmmss'
        $file = Join-Path $script:LOGS "drivers_$ts.csv"
        $drivers | Select-Object Cat, Name, Version, Date, Mfg |
            Export-Csv -Path $file -NoTypeInformation -Encoding UTF8
        AddLog "Drivers exported to: $file" '#22C55E'
    } catch { AddLog "Export error: $($_.Exception.Message)" '#E01818' }
})

# ── GPU HEALTH PAGE ───────────────────────────────────────────────────────────
(G 'BtnGpuRefresh').Add_Click({ try { RefreshGpuHealth } catch {} })

(G 'BtnOpenNvidiaSmi').Add_Click({
    try {
        $paths = @(
            'C:\Windows\System32\nvidia-smi.exe',
            'C:\Program Files\NVIDIA Corporation\NVSMI\nvidia-smi.exe'
        )
        $found = $paths | Where-Object { Test-Path $_ } | Select-Object -First 1
        if ($found) {
            Start-Process 'cmd.exe' "/k `"$found`""
            AddLog "Opened nvidia-smi." '#22C55E'
        } else {
            AddLog "nvidia-smi not found. Install NVIDIA drivers." '#F59E0B'
        }
    } catch {}
})

(G 'BtnOpenGpuZ').Add_Click({
    try {
        $gpuz = Get-Command 'GPU-Z.exe' -EA SilentlyContinue
        if ($gpuz) { Start-Process $gpuz.Source }
        else        { AddLog "GPU-Z not found. Install it from App Installer." '#F59E0B' }
    } catch {}
})

# ── LATENCY PAGE ──────────────────────────────────────────────────────────────
(G 'BtnRunLatency').Add_Click({
    try {
        $btn = G 'BtnRunLatency'; if ($btn) { $btn.IsEnabled = $false }
        # Run on background thread to keep UI responsive during latency tests
        [System.Threading.Tasks.Task]::Run([System.Action]{
            try   { Invoke-LatencyCheck }
            catch { AddLog "Latency error: $($_.Exception.Message)" '#E01818' }
            finally { UI { $b = G 'BtnRunLatency'; if ($b) { $b.IsEnabled = $true } } }
        }) | Out-Null
    } catch {}
})

(G 'BtnLatCopyLog').Add_Click({
    try {
        $lr = G 'LatResults'
        if ($lr -and $lr.Text) {
            [System.Windows.Clipboard]::SetText($lr.Text)
            AddLog "Latency results copied to clipboard." '#22C55E'
        }
    } catch {}
})

(G 'BtnLatSaveLog').Add_Click({
    try {
        $lr = G 'LatResults'
        if ($lr -and $lr.Text) {
            $ts   = Get-Date -Format 'yyyyMMdd_HHmmss'
            $file = Join-Path $script:LOGS "latency_$ts.txt"
            Set-Content -Path $file -Value $lr.Text -Encoding UTF8
            AddLog "Latency results saved to: $file" '#22C55E'
        }
    } catch {}
})

# ── TIMER (sidebar CPU/RAM bars) ──────────────────────────────────────────────
# Cache OS every 3 ticks (~12s) to reduce WMI overhead.
# WMI is fetched on a background thread; only the UI update runs on dispatcher.
$script:timerTick = 0
$script:cachedOS  = $null
$script:timer     = New-Object System.Windows.Threading.DispatcherTimer
$script:timer.Interval = [TimeSpan]::FromSeconds(4)
$script:timer.Add_Tick({
    try {
        $script:timerTick++
        [System.Threading.Tasks.Task]::Run([System.Action]{
            try {
                if ($script:timerTick % 3 -eq 0 -or -not $script:cachedOS) {
                    $script:cachedOS = Get-CimInstance Win32_OperatingSystem -EA SilentlyContinue
                }
                $cpu  = Get-CimInstance Win32_Processor -EA SilentlyContinue | Select-Object -First 1
                $ci   = if ($cpu) { [int]$cpu.LoadPercentage } else { 0 }
                $os   = $script:cachedOS
                $used = if ($os) { [math]::Round(($os.TotalVisibleMemorySize - $os.FreePhysicalMemory) / 1MB, 1) } else { 0 }
                $tot  = if ($os) { [math]::Round($os.TotalVisibleMemorySize / 1MB, 1) } else { 1 }
                $rp   = if ($tot -gt 0) { [math]::Round($used / $tot * 100) } else { 0 }
                UI {
                    $cl = G 'CpuLbl'; if ($cl) { $cl.Text = "CPU  $ci%" }
                    $cb = G 'CpuBar'; if ($cb) { $cb.Value = $ci }
                    $rl = G 'RamLbl'; if ($rl) { $rl.Text = "RAM  ${used}GB" }
                    $rb = G 'RamBar'; if ($rb) { $rb.Value = $rp }
                }
            } catch {}
        }) | Out-Null
    } catch {}
})
$script:timer.Start()

# ── WINDOW CONTROLS (custom chrome) ───────────────────────────────────────────
$wb = G 'BtnWinMin'
if ($wb) { $wb.Add_Click({ try { $script:Win.WindowState = 'Minimized' } catch {} }) }

$wb = G 'BtnWinMax'
if ($wb) {
    $wb.Add_Click({
        try {
            if ($script:Win.WindowState -eq 'Maximized') {
                $script:Win.WindowState = 'Normal'
            } else {
                $script:Win.WindowState = 'Maximized'
            }
        } catch {}
    })
}

$wb = G 'BtnWinClose'
if ($wb) { $wb.Add_Click({ try { $script:Win.Close() } catch {} }) }

# Drag to move -only active in the top 44px (title bar area)
$script:Win.Add_MouseLeftButtonDown({
    param($s, $e)
    try {
        $pt = $e.GetPosition($script:Win)
        if ($pt.Y -le 44) { $script:Win.DragMove() }
    } catch {}
})
