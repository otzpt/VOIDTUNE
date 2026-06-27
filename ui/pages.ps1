# VOIDTUNE - ui/pages.ps1
# Copyright (C) 2026 @OTZPT - GPL v3
# Page refresh functions, ShowPage, FilterTweaks, DoApply

function RefreshDash {
    try {
        $os   = Get-CimInstance Win32_OperatingSystem  -EA SilentlyContinue
        $cpu  = Get-CimInstance Win32_Processor        -EA SilentlyContinue | Select-Object -First 1
        $disk = Get-CimInstance Win32_LogicalDisk -Filter "DeviceID='C:'" -EA SilentlyContinue
        $ci   = if ($cpu)  { [int]$cpu.LoadPercentage } else { 0 }
        $tot  = if ($os)   { [math]::Round($os.TotalVisibleMemorySize / 1MB, 1) } else { 1 }
        $used = if ($os)   { [math]::Round(($os.TotalVisibleMemorySize - $os.FreePhysicalMemory) / 1MB, 1) } else { 0 }
        $dp   = if ($disk -and $disk.Size -gt 0) { [math]::Round(($disk.Size - $disk.FreeSpace) / $disk.Size * 100) } else { 50 }
        $rp   = if ($tot  -gt 0) { [math]::Round($used / $tot * 100) } else { 0 }
        $dOff = (Get-Service DiagTrack -EA SilentlyContinue).StartType -eq 'Disabled'
        $diagBonus = if ($dOff) { 90 } else { 30 }
        $score = [int](([math]::Max(0,100-$ci))*0.2 + ([math]::Max(0,100-$rp))*0.2 + ([math]::Max(0,100-$dp))*0.3 + $diagBonus*0.3)
        $cl = G 'CpuLbl';    if ($cl) { $cl.Text = "CPU  $ci%" }
        $cb = G 'CpuBar';    if ($cb) { $cb.Value = $ci }
        $rl = G 'RamLbl';    if ($rl) { $rl.Text = "RAM  ${used}GB" }
        $rb = G 'RamBar';    if ($rb) { $rb.Value = $rp }
        $dc = G 'DashCpu';   if ($dc) { $dc.Text = "$ci%" }
        $dr = G 'DashRam';   if ($dr) { $dr.Text = "${used}GB" }
        $hs = G 'HealthScore'; if ($hs) { $hs.Text = $score }
        $hlTxt = if ($score -ge 80) { "GOOD" } elseif ($score -ge 50) { "FAIR" } else { "NEEDS WORK" }
        $hl = G 'HealthLabel'; if ($hl) { $hl.Text = $hlTxt }
        if ($hs) {
            if     ($score -ge 80) { $hs.Foreground = [System.Windows.Media.Brushes]::LimeGreen }
            elseif ($score -ge 50) { $hs.Foreground = New-Object System.Windows.Media.SolidColorBrush ([System.Windows.Media.Color]::FromRgb(245,158,11)) }
            else                   { $hs.Foreground = [System.Windows.Media.Brushes]::Crimson }
        }
        $procCount = (Get-Process -EA SilentlyContinue).Count
        $bn = @()
        if ($ci -gt 80)        { $bn += "HIGH CPU ($ci%) -- close background processes" }
        if ($dp -gt 90)        { $bn += "LOW DISK ($dp% full) -- clean C: drive" }
        if (-not $dOff)        { $bn += "TELEMETRY ACTIVE -- use Privacy tab to disable" }
        if ($procCount -gt 120){ $bn += "HIGH PROCESS COUNT ($procCount) -- use Process tab to kill bloat" }
        $bnTxt = if ($bn.Count -gt 0) { $bn -join "`n" } else { "No bottlenecks detected. System looks clean." }
        $bt  = G 'BnText';     if ($bt)  { $bt.Text  = $bnTxt }
        $da  = G 'DashApplied'; if ($da)  { $da.Text  = $script:applied }
        $db  = G 'DashBackups'; if ($db)  { $db.Text  = @(Get-ChildItem $script:BACKUPS -Filter '*.reg' -EA SilentlyContinue).Count }
        $dp2 = G 'DashProcs';   if ($dp2) { $dp2.Text = $procCount }
    } catch { AddLog "Dashboard error: $($_.Exception.Message)" '#E01818' }
}

function RefreshSvcs {
    try {
        foreach ($s in $script:svcOC) {
            $svc = Get-Service $s.Name -EA SilentlyContinue
            if ($svc) {
                $s.Status = $svc.Status.ToString().ToUpper()
                $s.SC     = if ($svc.Status -eq 'Running') { '#F59E0B' } else { '#22C55E' }
            } else {
                $s.Status = 'N/A'; $s.SC = '#3A3A3A'
            }
        }
    } catch { AddLog "Services error: $($_.Exception.Message)" '#E01818' }
}

function RefreshStartup {
    try {
        $items = @()
        $hives = @(
            'HKCU:\Software\Microsoft\Windows\CurrentVersion\Run',
            'HKCU:\Software\Microsoft\Windows\CurrentVersion\RunOnce',
            'HKLM:\SOFTWARE\Microsoft\Windows\CurrentVersion\Run',
            'HKLM:\SOFTWARE\Microsoft\Windows\CurrentVersion\RunOnce'
        )
        foreach ($hive in $hives) {
            $r = Get-ItemProperty $hive -EA SilentlyContinue
            if ($r) {
                $label = if ($hive -like 'HKCU*') { 'HKCU' } else { 'HKLM' }
                $r.PSObject.Properties | Where-Object { $_.Name -notlike 'PS*' } | ForEach-Object {
                    $items += [STI]@{ Name=$_.Name; Cmd=$_.Value; Hive=$label }
                }
            }
        }
        $col = New-Object 'System.Collections.ObjectModel.ObservableCollection[STI]'
        foreach ($i in $items) { $col.Add($i) }
        $sl = G 'StartupList'; if ($sl) { $sl.ItemsSource = $col }
    } catch { AddLog "Startup error: $($_.Exception.Message)" '#E01818' }
}

function RefreshDiag {
    try {
        $cpu   = Get-CimInstance Win32_Processor      -EA SilentlyContinue | Select-Object -First 1
        $gpu   = Get-DiscreteGpu
        $os    = Get-CimInstance Win32_OperatingSystem -EA SilentlyContinue
        $disk  = Get-CimInstance Win32_LogicalDisk -Filter "DeviceID='C:'" -EA SilentlyContinue
        $board = Get-CimInstance Win32_BaseBoard -EA SilentlyContinue
        $cards = @()
        if ($cpu)  { $cards += [DC]@{Lbl='CPU';     Val=$cpu.Name.Trim();  Sub="$($cpu.NumberOfCores)c / $($cpu.NumberOfLogicalProcessors)t"} }
        if ($gpu)  {
            $allGpuStr = if ($script:HW.AllGpus.Count -gt 1) { 'Adapters: ' + ($script:HW.AllGpus -join ' | ') } else { '' }
            $cards += [DC]@{Lbl="GPU [$($script:HW.GpuVendor)]"; Val=$gpu.Name.Trim(); Sub="Driver $($gpu.DriverVersion)$(if($allGpuStr){' | '+$allGpuStr})"}
        }
        if ($os)   { $cards += [DC]@{Lbl='RAM';     Val="$([math]::Round($os.TotalVisibleMemorySize/1MB,1))GB"; Sub="$([math]::Round($os.FreePhysicalMemory/1MB,1))GB free"} }
        if ($disk) { $cards += [DC]@{Lbl='DISK C:'; Val="$([math]::Round($disk.Size/1GB,0))GB"; Sub="$([math]::Round($disk.FreeSpace/1GB,1))GB free"} }
        if ($os)   { $cards += [DC]@{Lbl='OS';      Val=$os.Caption.Trim(); Sub="Build $($os.BuildNumber)"} }
        if ($board){ $cards += [DC]@{Lbl='BOARD';   Val=$board.Product.Trim(); Sub=$board.Manufacturer.Trim()} }
        if ($os)   {
            $up = (Get-Date) - $os.LastBootUpTime
            $cards += [DC]@{Lbl='UPTIME'; Val=("{0}d {1}h {2}m" -f [int]$up.TotalDays,$up.Hours,$up.Minutes); Sub='Since last boot'}
        }
        $dl = G 'DiagList'
        if ($dl) {
            $col = New-Object 'System.Collections.ObjectModel.ObservableCollection[DC]'
            foreach ($c in $cards) { $col.Add($c) }
            $dl.ItemsSource = $col
        }
    } catch { AddLog "Diag error: $($_.Exception.Message)" '#E01818' }
}

function Get-ProcCategory($name) {
    switch -Wildcard ($name.ToLower()) {
        'svchost'                   { return 'SYSTEM' }
        'explorer'                  { return 'SYSTEM' }
        'dwm'                       { return 'SYSTEM' }
        'csrss'                     { return 'SYSTEM' }
        'winlogon'                  { return 'SYSTEM' }
        'lsass'                     { return 'SYSTEM' }
        'services'                  { return 'SYSTEM' }
        'wininit'                   { return 'SYSTEM' }
        'taskhostw'                 { return 'SYSTEM' }
        'sihost'                    { return 'SYSTEM' }
        'fontdrvhost'               { return 'SYSTEM' }
        'audiodg'                   { return 'SYSTEM' }
        'spoolsv'                   { return 'SYSTEM' }
        'smss'                      { return 'SYSTEM' }
        'registry'                  { return 'SYSTEM' }
        'memory compression'        { return 'SYSTEM' }
        'ntoskrnl'                  { return 'SYSTEM' }
        'chrome'                    { return 'BROWSER' }
        'msedge'                    { return 'BROWSER' }
        'firefox'                   { return 'BROWSER' }
        'brave'                     { return 'BROWSER' }
        'opera'                     { return 'BROWSER' }
        'iexplore'                  { return 'BROWSER' }
        'steam'                     { return 'GAMING' }
        'steamwebhelper'            { return 'GAMING' }
        'epicgameslauncher'         { return 'GAMING' }
        'eadesktop'                 { return 'GAMING' }
        'eabackgroundservice'       { return 'GAMING' }
        'ubisoftconnect'            { return 'GAMING' }
        'battlenet'                 { return 'GAMING' }
        'xboxapp'                   { return 'GAMING' }
        'gamebarpresencewriter'     { return 'GAMING' }
        'xboxgamebar*'              { return 'GAMING' }
        'spotify'                   { return 'MEDIA' }
        'vlc'                       { return 'MEDIA' }
        'obs64'                     { return 'MEDIA' }
        'obs32'                     { return 'MEDIA' }
        'plex*'                     { return 'MEDIA' }
        'discord'                   { return 'COMM' }
        'teams'                     { return 'COMM' }
        'microsoftteams'            { return 'COMM' }
        'slack'                     { return 'COMM' }
        'telegram'                  { return 'COMM' }
        'whatsapp'                  { return 'COMM' }
        'msmpeng'                   { return 'SECURITY' }
        'nissrv'                    { return 'SECURITY' }
        'mbam*'                     { return 'SECURITY' }
        'avp'                       { return 'SECURITY' }
        'avgnt'                     { return 'SECURITY' }
        'onedrive'                  { return 'BLOAT' }
        'cortana'                   { return 'BLOAT' }
        'widgets'                   { return 'BLOAT' }
        'widgetservice'             { return 'BLOAT' }
        'yourphone*'                { return 'BLOAT' }
        'phoneexperiencehost'       { return 'BLOAT' }
        'searchapp'                 { return 'BLOAT' }
        'runtimebroker'             { return 'BLOAT' }
        'shellexperiencehost'       { return 'BLOAT' }
        'startmenuexperiencehost'   { return 'BLOAT' }
        'speechruntime'             { return 'BLOAT' }
        'speechmodeldownload'       { return 'BLOAT' }
        'tabtip*'                   { return 'BLOAT' }
        'adobearm'                  { return 'BLOAT' }
        'acrotray'                  { return 'BLOAT' }
        'discord_updater'           { return 'BLOAT' }
        'igcctray'                  { return 'BLOAT' }
        'searchindexer'             { return 'BLOAT' }
        'winstore.app'              { return 'BLOAT' }
        default                     { return 'USER' }
    }
}

function RefreshProc {
    try {
        $allProcs = Get-Process -EA SilentlyContinue
        $procs = $allProcs |
            Where-Object { $_.WorkingSet64 -gt 512KB } |
            Sort-Object WorkingSet64 -Descending |
            Select-Object -First 80
        $col = New-Object 'System.Collections.ObjectModel.ObservableCollection[PI]'
        foreach ($p in $procs) {
            $ramMB = [math]::Round($p.WorkingSet64 / 1MB, 0)
            $cat   = Get-ProcCategory $p.ProcessName
            $col.Add([PI]@{
                Name     = $p.ProcessName
                Pid      = $p.Id
                Ram      = "${ramMB} MB"
                Cpu      = ''
                Category = $cat
            })
        }
        $pl = G 'ProcList'
        if ($pl) {
            $pl.ItemsSource = $col
            $view = [System.Windows.Data.CollectionViewSource]::GetDefaultView($col)
            $view.GroupDescriptions.Clear()
            $view.GroupDescriptions.Add((New-Object System.Windows.Data.PropertyGroupDescription 'Category'))
            $view.SortDescriptions.Clear()
            $view.SortDescriptions.Add((New-Object System.ComponentModel.SortDescription 'Category', 'Ascending'))
            $view.SortDescriptions.Add((New-Object System.ComponentModel.SortDescription 'Ram',      'Descending'))
        }
        $total = $allProcs.Count
        $pc = G 'ProcCount'; if ($pc) { $pc.Text = "TOTAL: $total processes" }
    } catch { AddLog "Process error: $($_.Exception.Message)" '#E01818' }
}

function Remove-Bloat {
    try {
        $bloatNames = @(
            'OneDrive','SearchApp','Cortana','WidgetService','widgets',
            'GameBarPresenceWriter','XboxGameBarWidgets','XboxGameBar',
            'YourPhone','YourPhoneServer','PhoneExperienceHost',
            'MicrosoftTeams','Teams','WinStore.App',
            'SpeechRuntime','SpeechModelDownload',
            'TabTip','TabTip32',
            'AdobeARM','armsvc','acrotray',
            'EABackgroundService','EADesktop',
            'IGCCTray',
            'SkypeApp','SkypeBackgroundHost',
            'HxOutlook','HxCalendarAppImm','HxAccounts',
            'Microsoft.Photos','Video.UI',
            'MixedRealityPortal','HolographicShell'
        )
        $protected = @('csrss','winlogon','lsass','smss','wininit','services',
                       'System','Idle','dwm','svchost','explorer','powershell',
                       'VOIDTUNE','cmd')
        # Single Get-Process call, then filter — avoids 20+ individual WMI lookups
        $running   = @(Get-Process -EA SilentlyContinue | ForEach-Object { $_.ProcessName })
        $killed    = 0
        foreach ($name in $bloatNames) {
            if ($protected -contains $name) { continue }
            if ($running -contains $name) {
                Stop-Process -Name $name -Force -EA SilentlyContinue
                AddLog "  Killed: $name" '#22C55E'
                $killed++
            }
        }
        $total = (Get-Process -EA SilentlyContinue).Count
        AddLog "Bloat sweep done. Killed $killed processes. Total now: $total" '#22C55E'
        RefreshProc
    } catch { AddLog "Bloat kill error: $($_.Exception.Message)" '#E01818' }
}

function RefreshBackups {
    try {
        $files = Get-ChildItem $script:BACKUPS -Filter '*.reg' -EA SilentlyContinue |
                 Sort-Object LastWriteTime -Descending
        $col = New-Object 'System.Collections.ObjectModel.ObservableCollection[BKI]'
        foreach ($f in $files) {
            # Validate: check registry header line
            $header = Get-Content $f.FullName -TotalCount 1 -Encoding Unicode -EA SilentlyContinue
            $valid  = $header -like '*Windows Registry Editor*'

            # Read metadata companion file
            $base     = [System.IO.Path]::GetFileNameWithoutExtension($f.Name)
            $metaFile = Join-Path $script:BACKUPS "${base}_meta.txt"
            $detail   = ''
            if (Test-Path $metaFile) {
                $meta    = Get-Content $metaFile -Encoding UTF8 -EA SilentlyContinue
                $twLine  = $meta | Where-Object { $_ -like 'AppliedTweaks=*' }
                $noteLine = $meta | Where-Object { $_ -like 'Note=*' }
                $cnt  = if ($twLine)   { ($twLine   -split '=',2)[1].Trim() } else { '?' }
                $note = if ($noteLine) { ($noteLine -split '=',2)[1].Trim() } else { '' }
                $hasSvc = Test-Path (Join-Path $script:BACKUPS "${base}_svc.txt")
                $detail = "[$note] $cnt tweaks$(if($hasSvc){' + services'}else{''})"
            }

            $col.Add([BKI]@{
                Name     = $f.Name
                FileName = $f.Name
                Date     = $f.LastWriteTime.ToString('yyyy-MM-dd HH:mm')
                Size     = "$([math]::Round($f.Length/1KB,1)) KB"
                Valid    = if ($valid) { '✓' } else { '⚠' }
                Detail   = $detail
            })
        }
        $bl = G 'BackupList'; if ($bl) { $bl.ItemsSource = $col }
    } catch { AddLog "Backup list error: $($_.Exception.Message)" '#E01818' }
}

# ── Invoke-VerifyState ────────────────────────────────────────────────────────
# Checks each applied tweak against the actual registry to confirm it's active.
function Invoke-VerifyState {
    try {
        $all = @($script:TWEAKS + $script:PRIVTWEAKS + $script:ARCH_TWEAKS | Where-Object { $_.Applied })
        if ($all.Count -eq 0) { AddLog "VERIFY: No tweaks marked as applied." '#F59E0B'; return }
        AddLog "VERIFY: Checking $($all.Count) applied tweaks against registry..." '#38BDF8'
        $confirmed = 0; $mismatch = 0; $unknown = 0

        foreach ($tw in $all) {
            $cmd = $tw.Cmd
            if ([string]::IsNullOrEmpty($cmd) -or $cmd.StartsWith('PS:') -or
                $cmd -like '*sc config*' -or $cmd -like '*powercfg*' -or
                $cmd -like '*netsh*'     -or $cmd -like '*schtasks*' -or
                $cmd -like '*fsutil*'   -or $cmd -like '*bcdedit*') {
                AddLog "  -- $($tw.Name) — non-registry, cannot verify" '#3D3560'
                $unknown++
                continue
            }
            # Parse: reg add "HKLM\X" /v Name /t REG_DWORD /d Value
            if ($cmd -match 'reg add "([^"]+)" /v "?([^"/ ]+)"? /t REG_DWORD /d (\d+)') {
                $raw = $Matches[1]
                $kp  = if ($raw -like 'HKLM\*') { 'Registry::HKEY_LOCAL_MACHINE\' + $raw.Substring(5) }
                       elseif ($raw -like 'HKCU\*') { 'Registry::HKEY_CURRENT_USER\' + $raw.Substring(5) }
                       else { $null }
                if (-not $kp) { $unknown++; continue }
                $vn  = $Matches[2].Trim()
                $exp = [int]$Matches[3]
                try {
                    $actual = (Get-ItemProperty $kp -EA SilentlyContinue).$vn
                    if ($null -ne $actual -and [int]$actual -eq $exp) {
                        AddLog "  ✓ $($tw.Name)" '#22C55E'; $confirmed++
                    } else {
                        AddLog "  ✗ $($tw.Name) — expected $exp, got $actual" '#E01818'; $mismatch++
                    }
                } catch { AddLog "  -- $($tw.Name) — key unreadable" '#3D3560'; $unknown++ }
            } elseif ($cmd -match 'reg add "([^"]+)" /v "?([^"/ ]+)"? /t REG_SZ /d "?([^"/]+)"?') {
                $raw = $Matches[1]
                $kp  = if ($raw -like 'HKLM\*') { 'Registry::HKEY_LOCAL_MACHINE\' + $raw.Substring(5) }
                       elseif ($raw -like 'HKCU\*') { 'Registry::HKEY_CURRENT_USER\' + $raw.Substring(5) }
                       else { $null }
                if (-not $kp) { $unknown++; continue }
                $vn  = $Matches[2].Trim()
                $exp = $Matches[3].Trim()
                try {
                    $actual = (Get-ItemProperty $kp -EA SilentlyContinue).$vn
                    if ("$actual" -eq $exp) {
                        AddLog "  ✓ $($tw.Name)" '#22C55E'; $confirmed++
                    } else {
                        AddLog "  ✗ $($tw.Name) — expected '$exp', got '$actual'" '#E01818'; $mismatch++
                    }
                } catch { AddLog "  -- $($tw.Name) — key unreadable" '#3D3560'; $unknown++ }
            } else {
                AddLog "  -- $($tw.Name) — command format not parseable" '#3D3560'; $unknown++
            }
        }
        AddLog "VERIFY: ✓ $confirmed active · ✗ $mismatch drifted · -- $unknown non-verifiable" '#38BDF8'
        if ($mismatch -gt 0) {
            AddLog "  → Re-apply drifted tweaks, or use REVERT ALL + re-apply." '#F59E0B'
        }
    } catch { AddLog "Verify error: $($_.Exception.Message)" '#E01818' }
}

function FilterTweaks {
    try {
        if (-not $script:TweakTabs) { return }
        $idx = $script:TweakTabs.SelectedIndex
        if ($idx -lt 0) { return }
        $cat = $script:catMap[$idx]
        if (-not $cat)  { return }
        $col = New-Object 'System.Collections.ObjectModel.ObservableCollection[TI]'
        foreach ($t in $script:TWEAKS)      { if ($t.Cat -eq $cat) { $col.Add($t) } }
        foreach ($t in $script:ARCH_TWEAKS) { if ($t.Cat -eq $cat) { $col.Add($t) } }
        $tl = G 'TweakList'; if ($tl) { $tl.ItemsSource = $col }
    } catch {}
}

# Install a single app - tries winget first, falls back to choco
function Install-App($app) {
    if ($script:HW.WingetOK) {
        try {
            $proc = Start-Process -FilePath "winget" `
                -ArgumentList "install --id $($app.Id) -e --accept-source-agreements --accept-package-agreements --scope machine" `
                -NoNewWindow -Wait -PassThru
            if ($proc.ExitCode -eq 0) { return @{ OK=$true; Via='winget' } }
        } catch {}
    }
    if ($script:HW.ChocoOK) {
        try {
            $chocoMap = @{
                'Google.Chrome'                   = 'googlechrome'
                'Mozilla.Firefox'                 = 'firefox'
                'Brave.Brave'                     = 'brave'
                'Opera.OperaGX'                   = 'opera-gx'
                'Discord.Discord'                 = 'discord'
                'Telegram.TelegramDesktop'        = 'telegram'
                'Valve.Steam'                     = 'steam'
                'TechPowerUp.GPU-Z'               = 'gpu-z'
                'CPUID.CPU-Z'                     = 'cpu-z'
                'REALiX.HWiNFO'                  = 'hwinfo'
                'MSI.MSIAfterburner'              = 'msiafterburner'
                'CrystalDewWorld.CrystalDiskInfo' = 'crystaldiskinfo'
                '7zip.7zip'                       = '7zip'
                'RARLab.WinRAR'                   = 'winrar'
                'VideoLAN.VLC'                    = 'vlc'
                'OBSProject.OBSStudio'            = 'obs-studio'
                'Microsoft.VisualStudioCode'      = 'vscode'
                'Notepad++.Notepad++'             = 'notepadplusplus'
                'SublimeHQ.SublimeText.4'         = 'sublimetext4'
                'Git.Git'                         = 'git'
                'GitHub.GitHubDesktop'            = 'github-desktop'
                'OpenJS.NodeJS'                   = 'nodejs'
                'Python.Python.3'                 = 'python'
                'Postman.Postman'                 = 'postman'
                'Neovim.Neovim'                   = 'neovim'
                'Microsoft.WindowsTerminal'       = 'microsoft-windows-terminal'
                'TimKosse.FileZilla.Client'       = 'filezilla'
                'WinSCP.WinSCP'                   = 'winscp'
                'Spotify.Spotify'                 = 'spotify'
                'GIMP.GIMP'                       = 'gimp'
                'Audacity.Audacity'               = 'audacity'
                'Malwarebytes.Malwarebytes'       = 'malwarebytes'
                'Bitwarden.Bitwarden'             = 'bitwarden'
                'Microsoft.PowerToys'             = 'powertoys'
                'voidtools.Everything'            = 'everything'
                'HandBrake.HandBrake'             = 'handbrake'
                'Rufus.Rufus'                     = 'rufus'
                'ShareX.ShareX'                   = 'sharex'
                'AutoHotkey.AutoHotkey'           = 'autohotkey'
            }
            $chocoId = $chocoMap[$app.Id]
            if ($chocoId) {
                $proc = Start-Process -FilePath "choco" `
                    -ArgumentList "install $chocoId -y" `
                    -NoNewWindow -Wait -PassThru
                if ($proc.ExitCode -eq 0) { return @{ OK=$true; Via='choco' } }
            }
        } catch {}
    }
    return @{ OK=$false; Via='none' }
}

function DoApply {
    try {
        $sel   = @($script:TWEAKS + $script:PRIVTWEAKS + $script:ARCH_TWEAKS | Where-Object { $_.Sel })
        $apps  = @($script:APPS_DATA | Where-Object { $_.Sel })
        $total = $sel.Count + $apps.Count
        if ($total -eq 0) {
            UI { [System.Windows.MessageBox]::Show("Nothing selected.", "VOIDTUNE") }
            return
        }

        # Warn about EXTREME tweaks
        $extremeSel = @($sel | Where-Object { $_.Badge -eq 'EXTREME' })
        if ($extremeSel.Count -gt 0) {
            $extremeNames = ($extremeSel | ForEach-Object { $_.Name }) -join "`n  - "
            $warn  = "You have $($extremeSel.Count) EXTREME tweak(s) selected:`n`n  - $extremeNames`n`nThese are aggressive settings that may cause instability on some hardware."
            if ($sel | Where-Object { $_.Id -eq 'gpu5'  }) { $warn += "`n`n[!] DISABLE MPO can cause flickering or black screens on some GPU/driver combos. Revertible." }
            if ($sel | Where-Object { $_.Id -eq 'gpu12' }) { $warn += "`n`n[!] NO GPU PREEMPT can cause TDR crashes under heavy GPU load. Revertible." }
            if ($sel | Where-Object { $_.Id -eq 'gpu13' }) { $warn += "`n`n[!] MAX GPU CLOCKS raises idle power draw significantly on desktop GPUs." }
            if ($sel | Where-Object { $_.Id -eq 'lat3'  }) { $warn += "`n`n[!] IRQ PRIORITY can cause device conflicts on certain hardware configurations." }
            if ($sel | Where-Object { $_.Id -eq 'cpu12' }) { $warn += "`n`n[!] NO SPECTRE MITIG disables CPU security mitigations against Spectre/Meltdown. NEVER use on shared, work or public machines. Performance only." }
            if ($sel | Where-Object { $_.Id -eq 'stor6' }) { $warn += "`n`n[!] DISABLE NTFS JOURNAL breaks Volume Shadow Copy (VSS) and Windows Backup on C:." }
            $warn += "`n`nA registry backup will be created. Continue?"
            $confirm = $null
            UI { $confirm = [System.Windows.MessageBox]::Show($warn, "VOIDTUNE - EXTREME Tweaks", [System.Windows.MessageBoxButton]::YesNo, [System.Windows.MessageBoxImage]::Warning) }
            if ($confirm -ne 'Yes') { return }
        }

        # Warn about NUCLEAR tweaks - these disable core Windows security
        $nukeSel = @($sel | Where-Object { $_.Badge -eq 'NUCLEAR' })
        if ($nukeSel.Count -gt 0) {
            $nukeNames = ($nukeSel | ForEach-Object { $_.Name }) -join "`n  - "
            $nwarn  = "!!!  NUCLEAR TWEAKS SELECTED ($($nukeSel.Count))  !!!`n`n  - $nukeNames`n`n"
            $nwarn += "These DISABLE core Windows security (Defender, SmartScreen, UAC, memory integrity, firewall). "
            $nwarn += "Your system will be MUCH more vulnerable to malware and exploits.`n`n"
            $nwarn += "Only proceed on an isolated machine you fully control. Every change here is revertible via REVERT ALL.`n`n"
            $nwarn += "Are you ABSOLUTELY sure?"
            $nconfirm = $null
            UI { $nconfirm = [System.Windows.MessageBox]::Show($nwarn, "VOIDTUNE - NUCLEAR WARNING", [System.Windows.MessageBoxButton]::YesNo, [System.Windows.MessageBoxImage]::Warning) }
            if ($nconfirm -ne 'Yes') { return }
        }

        if ($apps.Count -gt 0 -and -not $script:HW.WingetOK -and -not $script:HW.ChocoOK) {
            AddLog "WARNING: Neither winget nor Chocolatey found. App installs will be skipped." '#F59E0B'
        }

        Show-ApplyOverlay 'APPLYING TWEAKS'
        SetProg 0; SetStatus 'BACKING UP...'
        Update-ApplyOverlay 'Creating registry backup...' 0 "0 / $total"
        $bk = MakeBackup 'apply'
        if ($bk -eq 'backup-failed') {
            $cont = $null
            UI { $cont = [System.Windows.MessageBox]::Show(
                "Registry backup FAILED.`n`nWithout a backup, settings cannot be restored via registry import — only per-tweak revert commands will work.`n`nContinue anyway?",
                "VOIDTUNE — Backup Failed",
                [System.Windows.MessageBoxButton]::YesNo,
                [System.Windows.MessageBoxImage]::Warning) }
            if ($cont -ne 'Yes') { Hide-ApplyOverlay; return }
            AddLog "WARNING: Backup failed. Proceeding without backup." '#F59E0B'
        } else {
            AddLog "BACKUP: $bk" '#22C55E'
        }
        AddLog "Starting $total operations..." '#AAAAAA'

        $i = 0; $ok = 0; $fail = 0
        foreach ($tw in $sel) {
            $i++
            $pct = [math]::Round($i / $total * 100)
            SetProg $pct; SetStatus $tw.Name
            Update-ApplyOverlay $tw.Name $pct "$i / $total"
            AddLog "[$i/$total] $($tw.Name)" '#AAAAAA'
            $r = Exec-Cmd $tw.Cmd
            if ($r.OK) {
                AddLog "  OK" '#22C55E'
                $ok++
                if (-not $tw.Applied) { $script:applied++ }
                $tw.Applied = $true
            } else {
                AddLog "  FAILED: $($r.Out.Split("`n")[0])" '#E01818'
                $fail++
            }
        }

        foreach ($app in $apps) {
            $i++
            $pct = [math]::Round($i / $total * 100)
            SetProg $pct; SetStatus "Installing $($app.Name)"
            Update-ApplyOverlay "Installing $($app.Name)" $pct "$i / $total"
            AddLog "[$i/$total] INSTALL $($app.Name)" '#AAAAAA'
            $result = Install-App $app
            if ($result.OK) { AddLog "  OK [via $($result.Via)]" '#22C55E'; $ok++ }
            else             { AddLog "  FAILED: winget and choco both unavailable or package not found" '#E01818'; $fail++ }
        }

        $summary = "$ok applied / $fail failed"
        SetProg 100; SetStatus 'DONE'
        Update-ApplyOverlay "Done — $summary" 100 $summary
        UI {
            $lc = G 'LogCount';    if ($lc) { $lc.Text  = $summary }
            $da = G 'DashApplied'; if ($da) { $da.Text  = $script:applied }
        }
        AddLog "DONE: $ok applied, $fail failed. Restart recommended." '#22C55E'
        Save-TweakState
        UI { FilterTweaks }
        Start-Sleep -Milliseconds 500
        Hide-ApplyOverlay
    } catch { AddLog "Apply error: $($_.Exception.Message)" '#E01818'; Hide-ApplyOverlay }
}

# ── Tiered selection: pick all tweaks up to $maxTier, skipping the Restore tab ──
# SAFE -> safe only | EXTREME -> safe+extreme | NUCLEAR -> everything
function Select-Tier($maxTier) {
    $rank   = @{ 'SAFE' = 0; 'EXTREME' = 1; 'NUCLEAR' = 2 }
    $maxIdx = $rank[$maxTier]
    foreach ($t in $script:TWEAKS + $script:ARCH_TWEAKS) {
        $ti    = $rank[[string]$t.Badge]
        $t.Sel = ($null -ne $ti -and $ti -le $maxIdx -and $t.Cat -ne 'rst')
    }
    foreach ($t in $script:PRIVTWEAKS) { $t.Sel = $false }
}

# ── Revert every applied tweak back to Windows defaults ───────────────────────
function Invoke-RevertAll {
    try {
        $all     = $script:TWEAKS + $script:PRIVTWEAKS + $script:ARCH_TWEAKS
        $applied = @($all | Where-Object { $_.Applied })
        if ($applied.Count -eq 0) { AddLog "REVERT ALL: nothing is applied." '#F59E0B'; return }

        Show-ApplyOverlay 'REVERTING TWEAKS'
        AddLog "REVERT ALL: reverting $($applied.Count) applied tweaks..." '#F59E0B'

        # Safety backup BEFORE reverting — so user can undo the revert if needed
        Update-ApplyOverlay 'Creating safety backup...' 0 "0 / $($applied.Count)"
        $preBk = MakeBackup 'pre-revert'
        if ($preBk -ne 'backup-failed') {
            AddLog "  Safety backup: $preBk" '#22C55E'
        } else {
            AddLog "  WARNING: Safety backup failed. Proceeding anyway." '#F59E0B'
        }

        SetProg 0; SetStatus 'REVERTING...'
        $total2  = $applied.Count
        $i       = 0; $ok = 0; $fail = 0; $noCmd = 0
        foreach ($tw in $applied) {
            $i++
            $pct = [math]::Round($i / $total2 * 100)
            SetProg $pct; SetStatus $tw.Name
            Update-ApplyOverlay $tw.Name $pct "$i / $total2"
            if ([string]::IsNullOrEmpty($tw.RevertCmd)) {
                AddLog "  ⚠ $($tw.Name) — no revert command, marking unapplied only" '#F59E0B'
                $noCmd++
            } else {
                $r = Exec-Cmd $tw.RevertCmd
                if ($r.OK) {
                    AddLog "  ✓ $($tw.Name)" '#22C55E'
                    $ok++
                } else {
                    AddLog "  ✗ $($tw.Name) — $($r.Out.Split("`n")[0])" '#E01818'
                    $fail++
                }
            }
            $tw.Applied = $false
            $tw.Sel     = $false
        }
        $script:applied = 0

        $summary = "$ok reverted · $noCmd skipped (no cmd) · $fail failed"
        SetProg 100; SetStatus 'DONE'
        Update-ApplyOverlay "Done" 100 "$ok reverted / $fail failed"
        Save-TweakState
        UI {
            FilterTweaks
            $da = G 'DashApplied'; if ($da) { $da.Text = 0 }
            $lc = G 'LogCount';    if ($lc) { $lc.Text = "$ok reverted / $fail failed" }
        }
        AddLog "REVERT ALL done: $summary" '#22C55E'
        if ($noCmd -gt 0) {
            AddLog "  → $noCmd tweaks have no revert command. Use registry backup restore to fully reset those." '#F59E0B'
        }
        if ($ok -gt 0 -or $fail -gt 0) {
            AddLog "  → Restart recommended for kernel/driver changes to take effect." '#38BDF8'
        }
        Start-Sleep -Milliseconds 600
        Hide-ApplyOverlay
    } catch { AddLog "Revert all error: $($_.Exception.Message)" '#E01818'; Hide-ApplyOverlay }
}

function RefreshDrivers {
    try {
        $filter    = G 'DrFilter'
        $filterVal = if ($filter) { $filter.Text.Trim().ToLower() } else { '' }
        $drivers   = Get-DriverList
        if ($filterVal) {
            $drivers = $drivers | Where-Object {
                $_.Name.ToLower().Contains($filterVal) -or
                $_.Cat.ToLower().Contains($filterVal)  -or
                $_.Mfg.ToLower().Contains($filterVal)
            }
        }
        $col = New-Object 'System.Collections.ObjectModel.ObservableCollection[DRI]'
        foreach ($d in $drivers) {
            $col.Add([DRI]@{ Name=$d.Name; Version=$d.Version; Date=$d.Date; Mfg=$d.Mfg; Cat=$d.Cat })
        }
        $dl = G 'DriverList';  if ($dl) { $dl.ItemsSource = $col }
        $dc = G 'DriverCount'; if ($dc) { $dc.Text = "$($col.Count) drivers" }
    } catch { AddLog "Driver refresh error: $($_.Exception.Message)" '#E01818' }
}

function RefreshGpuHealth {
    try {
        $data  = Get-GpuHealthData
        $items = @(
            @{ K='GPU';         V=$data.Name      },
            @{ K='Vendor';      V=$data.Vendor    },
            @{ K='Driver';      V=$data.DriverVer },
            @{ K='Driver Date'; V=$data.DriverDate},
            @{ K='VRAM Total';  V=$data.VramTotal },
            @{ K='VRAM Free';   V=$data.VramFree  },
            @{ K='GPU Usage';   V="$($data.Usage)%"},
            @{ K='Temperature'; V=$data.TempC     },
            @{ K='Core Clock';  V=$data.CoreClock },
            @{ K='Mem Clock';   V=$data.MemClock  },
            @{ K='Fan Speed';   V=$data.FanSpeed  },
            @{ K='Power Draw';  V=$data.PowerDraw }
        )
        foreach ($item in $items) {
            $el = G "GH_$($item.K -replace ' ','_')"
            if ($el) { $el.Text = $item.V }
        }
        $ag = G 'GH_AllGpus'; if ($ag) { $ag.Text = ($data.AllGpus -join '  |  ') }
        $ub = G 'GH_UsageBar'; if ($ub) { $ub.Value = $data.Usage }
    } catch { AddLog "GPU health error: $($_.Exception.Message)" '#E01818' }
}

function RefreshLatencyPage {
    $lr = G 'LatResults'; if ($lr) { $lr.Text = 'Click RUN LATENCY CHECK to begin...' }
    $ls = G 'LatScore';   if ($ls) { $ls.Text = '--' }
}

function Invoke-LatencyCheck {
    try {
        $lr    = G 'LatResults'
        $score = 100
        $issues = @()

        $update = {
            param($txt)
            UI {
                if ($lr) { $lr.Text += "$txt`n" }
                $ls2 = G 'LatScroll'; if ($ls2) { $ls2.ScrollToBottom() }
            }
        }

        UI { if ($lr) { $lr.Text = '' } }
        & $update "VOIDTUNE SYSTEM LATENCY CHECKER v0.8"
        & $update "=========================================`n"

        # ── 1. Timer Resolution ──────────────────────────────────────────────
        & $update "[ 1/6 ] SYSTEM TIMER RESOLUTION"
        $timer = Get-SystemTimerResolution
        & $update "  Current : $($timer.Current) ms"
        & $update "  Min     : $($timer.Min) ms"
        & $update "  Max     : $($timer.Max) ms"
        if ($timer.Current -le 0.6) {
            & $update "  Rating  : EXCELLENT -- timer already at max precision`n"
        } elseif ($timer.Current -le 1.1) {
            & $update "  Rating  : GOOD`n"
        } else {
            & $update "  Rating  : NEEDS FIX -- apply TIMER RESOLUTION tweak in Tweaks > CPU`n"
            $score -= 20; $issues += "Timer resolution: $($timer.Current)ms (apply TIMER RESOLUTION tweak)"
        }

        # ── 2. DPC Latency Heuristic ─────────────────────────────────────────
        & $update "[ 2/6 ] DPC LATENCY HEURISTIC"
        $dpc = Get-DpcLatencyHeuristic
        & $update "  Score   : $($dpc.Score)/100"
        if ($dpc.Issues.Count -eq 0) {
            & $update "  Status  : No known DPC issues detected`n"
        } else {
            foreach ($iss in $dpc.Issues) { & $update "  ISSUE   : $iss" }
            & $update ""
            $score -= [math]::Min(25, $dpc.Issues.Count * 8)
        }

        # ── 3. Disk I/O Latency ──────────────────────────────────────────────
        & $update "[ 3/6 ] DISK I/O LATENCY (4KB random reads, C:)"
        $disk = Measure-DiskLatency
        if ($disk.OK) {
            & $update "  Avg     : $($disk.AvgMs) ms"
            & $update "  Max     : $($disk.MaxMs) ms"
            $rating = if ($disk.AvgMs -lt 0.1)  { 'EXCELLENT (NVMe)' }
                      elseif ($disk.AvgMs -lt 0.5)  { 'VERY GOOD (fast SSD)' }
                      elseif ($disk.AvgMs -lt 2.0)  { 'GOOD (SSD)' }
                      elseif ($disk.AvgMs -lt 10.0) { 'OK (SATA SSD)' }
                      else                           { 'SLOW (HDD or thermal throttle)' }
            & $update "  Rating  : $rating`n"
            if ($disk.AvgMs -gt 5.0) { $score -= 10; $issues += "Disk latency high: $($disk.AvgMs)ms avg" }
        } else {
            & $update "  Status  : Measurement failed`n"
        }

        # ── 4. Memory Latency ────────────────────────────────────────────────
        & $update "[ 4/6 ] MEMORY LATENCY (sequential, 64-byte stride)"
        $mem = Measure-MemoryLatency
        if ($mem.OK) {
            & $update "  Access  : $($mem.NsPerAccess) ns/access"
            $rating = if ($mem.NsPerAccess -lt 3)  { 'EXCELLENT (DDR5/fast DDR4)' }
                      elseif ($mem.NsPerAccess -lt 6)  { 'GOOD (DDR4)' }
                      elseif ($mem.NsPerAccess -lt 12) { 'OK (DDR4 slower)' }
                      else                             { 'SLOW (check XMP/EXPO)' }
            & $update "  Rating  : $rating`n"
            if ($mem.NsPerAccess -gt 10) { $score -= 10; $issues += "Memory latency high: $($mem.NsPerAccess)ns (enable XMP/EXPO in BIOS)" }
        } else {
            & $update "  Status  : Measurement failed`n"
        }

        # ── 5. Network Ping ──────────────────────────────────────────────────
        & $update "[ 5/6 ] NETWORK LATENCY (ping)"
        $netHosts   = @('8.8.8.8','1.1.1.1','208.67.222.222')
        $netResults = Measure-NetworkLatency $netHosts
        foreach ($r in $netResults) {
            if ($r.OK) {
                & $update "  $($r.Host.PadRight(20)) Avg: $($r.AvgMs)ms  Min: $($r.MinMs)ms  Max: $($r.MaxMs)ms  Jitter: $($r.Jitter)ms"
            } else {
                & $update "  $($r.Host.PadRight(20)) TIMEOUT"
            }
        }
        $bestPing = ($netResults | Where-Object { $_.OK } | Sort-Object AvgMs | Select-Object -First 1)
        if ($bestPing) {
            $netRating = if ($bestPing.AvgMs -lt 10) { 'EXCELLENT' }
                         elseif ($bestPing.AvgMs -lt 30) { 'GOOD' }
                         elseif ($bestPing.AvgMs -lt 80) { 'OK' }
                         else                            { 'HIGH -- check ISP / VPN' }
            & $update "  Best    : $($bestPing.AvgMs)ms -- $netRating`n"
            if ($bestPing.AvgMs -gt 80) { $score -= 10 }
            if ($bestPing.Jitter -gt 20) { $score -= 5; $issues += "High ping jitter: $($bestPing.Jitter)ms (check network adapter tweaks)" }
        } else {
            & $update "  Status  : No hosts reachable (offline?)`n"
        }

        # ── 6. DNS Resolution ────────────────────────────────────────────────
        & $update "[ 6/6 ] DNS RESOLUTION LATENCY"
        $dnsHosts   = @('google.com','cloudflare.com','github.com','youtube.com')
        $dnsResults = Measure-DnsLatency $dnsHosts
        foreach ($r in $dnsResults) {
            & $update "  $($r.Host.PadRight(22)) $($r.AvgMs)ms"
        }
        $avgDns   = [math]::Round(($dnsResults | Measure-Object AvgMs -Average).Average, 1)
        $dnsRating = if ($avgDns -lt 10) { 'EXCELLENT' }
                     elseif ($avgDns -lt 30) { 'GOOD' }
                     elseif ($avgDns -lt 80) { 'OK' }
                     else                    { 'SLOW -- try Flush DNS tweak or change DNS server' }
        & $update "  Avg     : ${avgDns}ms -- $dnsRating`n"
        if ($avgDns -gt 80) { $score -= 5; $issues += "DNS slow: ${avgDns}ms (flush DNS or use 1.1.1.1)" }

        # ── SUMMARY ──────────────────────────────────────────────────────────
        $score = [math]::Max(0, [math]::Min(100, $score))
        $scoreLabel = if ($score -ge 85) { 'EXCELLENT' } elseif ($score -ge 65) { 'GOOD' } elseif ($score -ge 40) { 'FAIR' } else { 'NEEDS WORK' }
        & $update "========================================="
        & $update "LATENCY SCORE: $score/100 -- $scoreLabel"
        & $update "========================================="
        if ($issues.Count -gt 0) {
            & $update "`nRECOMMENDATIONS:"
            foreach ($iss in $issues) { & $update "  >> $iss" }
        } else {
            & $update "`nNo major latency issues detected."
        }
        & $update "`nCompleted: $(Get-Date -f 'HH:mm:ss')"

        UI { $ls = G 'LatScore'; if ($ls) { $ls.Text = "$score" } }
        AddLog "Latency check complete. Score: $score/100 ($scoreLabel)" '#38BDF8'
    } catch { AddLog "Latency check error: $($_.Exception.Message)" '#E01818' }
}

function ShowPage($name) {
    try {
        foreach ($p in $script:allPages) {
            $pg = G $p; if ($pg) { $pg.Visibility = 'Collapsed' }
        }
        $show = G $name
        if ($show) {
            $show.Opacity = 0
            $show.Visibility = 'Visible'
            Animate-PageIn $show
        }

        $btnMap = @{
            PgDash='BtnDash'; PgTweaks='BtnTweaks'; PgApps='BtnApps'; PgServices='BtnServices'
            PgStartup='BtnStartup'; PgPrivacy='BtnPrivacy'; PgDiag='BtnDiag'; PgBench='BtnBench'
            PgSafety='BtnSafety'; PgScript='BtnScript'; PgProc='BtnProc'; PgPersonalize='BtnPersonalize'
            PgDrivers='BtnDrivers'; PgGpuHealth='BtnGpuHealth'; PgLatency='BtnLatency'
        }

        # Use Style switching — avoids mutating frozen shared WPF brushes
        $activeStyle   = try { $script:Win.Resources['SidebarButtonActive'] } catch { $null }
        $inactiveStyle = try { $script:Win.Resources['SidebarButton']       } catch { $null }

        foreach ($kv in $btnMap.GetEnumerator()) {
            $b = G $kv.Value; if (-not $b) { continue }
            if ($kv.Key -eq $name) {
                if ($activeStyle)   { $b.Style = $activeStyle }
            } else {
                if ($inactiveStyle) { $b.Style = $inactiveStyle }
            }
        }

        # Lazy refresh — only runs WMI/IO when tab is actually opened
        switch ($name) {
            'PgDash'        { RefreshDash }
            'PgServices'    { RefreshSvcs }
            'PgStartup'     { RefreshStartup }
            'PgDiag'        { RefreshDiag }
            'PgProc'        { RefreshProc }
            'PgSafety'      { RefreshBackups }
            'PgPersonalize' { RefreshPersonalize }
            'PgDrivers'     { RefreshDrivers }
            'PgGpuHealth'   { RefreshGpuHealth }
            'PgLatency'     { RefreshLatencyPage }
        }
    } catch { AddLog "Navigation error: $($_.Exception.Message)" '#E01818' }
}
