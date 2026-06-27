# VOIDTUNE - modules/drivers.ps1
# Copyright (C) 2026 @OTZPT - GPL v3
# Driver info, GPU health data collection

# ── DRIVER DATA MODEL ─────────────────────────────────────────────────────────
# DRI model is defined in models.ps1 - used here as reference

# ── GET INSTALLED DRIVERS ─────────────────────────────────────────────────────
function Get-DriverList {
    try {
        $raw = Get-CimInstance Win32_PnPSignedDriver -EA SilentlyContinue |
               Where-Object { $_.DeviceName -and $_.DriverVersion } |
               Sort-Object DeviceClass, DeviceName

        $results = foreach ($d in $raw) {
            $cat = switch -Wildcard ($d.DeviceClass) {
                'Display'           { 'GPU' }
                'MEDIA'             { 'Audio' }
                'Net'               { 'Network' }
                'USB'               { 'USB' }
                'HDC'               { 'Storage' }
                'SCSIAdapter'       { 'Storage' }
                'DiskDrive'         { 'Storage' }
                'Processor'         { 'CPU' }
                'System'            { 'System' }
                'HIDClass'          { 'Input' }
                'Keyboard'          { 'Input' }
                'Mouse'             { 'Input' }
                'Bluetooth'         { 'Bluetooth' }
                'AudioEndpoint'     { 'Audio' }
                default             { if ($d.DeviceClass) { $d.DeviceClass } else { 'Other' } }
            }
            # Parse driver date
            $dateStr = 'Unknown'
            if ($d.DriverDate) {
                try { $dateStr = $d.DriverDate.ToString('yyyy-MM-dd') } catch {}
            }
            [PSCustomObject]@{
                Name     = $d.DeviceName.Trim()
                Version  = $d.DriverVersion
                Date     = $dateStr
                Mfg      = if ($d.Manufacturer) { $d.Manufacturer.Trim() } else { 'Unknown' }
                Cat      = $cat
                Class    = $d.DeviceClass
            }
        }
        return @($results)
    } catch { return @() }
}

# ── GPU HEALTH DATA ───────────────────────────────────────────────────────────
function Get-GpuHealthData {
    $data = @{
        Name         = $script:HW.GpuName
        Vendor       = $script:HW.GpuVendor
        DriverVer    = 'N/A'
        DriverDate   = 'N/A'
        VramTotal    = 'N/A'
        VramFree     = 'N/A'
        Usage        = 0
        TempC        = 'N/A'
        CoreClock    = 'N/A'
        MemClock     = 'N/A'
        FanSpeed     = 'N/A'
        PowerDraw    = 'N/A'
        AllGpus      = $script:HW.AllGpus
    }

    try {
        $gpuWmi = Get-CimInstance Win32_VideoController -EA SilentlyContinue |
                  Where-Object { $_.Name -eq $script:HW.GpuName } |
                  Select-Object -First 1

        if ($gpuWmi) {
            $data.DriverVer  = $gpuWmi.DriverVersion
            $data.DriverDate = try { $gpuWmi.DriverDate.ToString('yyyy-MM-dd') } catch { 'N/A' }
            # AdapterRAM do WMI e limitado a 4GB em versoes antigas - tentar via registry primeiro
            $vramBytes = 0
            try {
                $regPath = 'HKLM:\SYSTEM\CurrentControlSet\Control\Class\{4d36e968-e325-11ce-bfc1-08002be10318}'
                $subkeys = Get-ChildItem $regPath -EA SilentlyContinue | Where-Object { $_.Name -notlike '*Properties*' }
                foreach ($sk in $subkeys) {
                    $drvDesc = (Get-ItemProperty $sk.PSPath -EA SilentlyContinue).DriverDesc
                    if ($drvDesc -and $script:HW.GpuName -and $drvDesc.Trim() -eq $script:HW.GpuName.Trim()) {
                        $mem = (Get-ItemProperty $sk.PSPath -EA SilentlyContinue).HardwareInformation.qwMemorySize
                        if ($mem -gt 0) { $vramBytes = $mem; break }
                    }
                }
            } catch {}
            if ($vramBytes -le 0) { $vramBytes = $gpuWmi.AdapterRAM }
            $data.VramTotal = if ($vramBytes -gt 0) {
                "$([math]::Round($vramBytes / 1GB, 1)) GB"
            } else { 'N/A' }
        }
    } catch {}

    # GPU Usage via performance counter
    try {
        $counter = '\GPU Engine(*engtype_3D)\Utilization Percentage'
        $samples = Get-Counter $counter -EA SilentlyContinue -MaxSamples 1 -SampleInterval 1
        if ($samples) {
            $vals = $samples.CounterSamples | Where-Object { $_.CookedValue -gt 0 }
            if ($vals) { $data.Usage = [int]($vals | Measure-Object CookedValue -Sum).Sum }
            $data.Usage = [math]::Min(100, $data.Usage)
        }
    } catch {}

    # NVIDIA-specific: nvidia-smi
    if ($data.Vendor -eq 'NVIDIA') {
        try {
            $smiPath = $null
            # Tentar varios caminhos possiveis
            $candidates = @(
                'nvidia-smi.exe',
                'C:\Windows\System32\nvidia-smi.exe',
                'C:\Program Files\NVIDIA Corporation\NVSMI\nvidia-smi.exe',
                "$env:ProgramFiles\NVIDIA Corporation\NVSMI\nvidia-smi.exe",
                "$env:SystemRoot\System32\nvidia-smi.exe"
            )
            foreach ($c in $candidates) {
                try {
                    if (Test-Path $c -EA SilentlyContinue) { $smiPath = $c; break }
                } catch {}
            }
            # Fallback: procurar no PATH
            if (-not $smiPath) {
                $found = Get-Command 'nvidia-smi.exe' -EA SilentlyContinue
                if ($found) { $smiPath = $found.Source }
            }
            if ($smiPath) {
                $query  = '--query-gpu=temperature.gpu,clocks.current.graphics,clocks.current.memory,fan.speed,power.draw,memory.free,memory.total'
                $format = '--format=csv,noheader,nounits'
                $result = & $smiPath $query $format 2>$null
                if ($result) {
                    $parts = ($result -split ',') | ForEach-Object { $_.Trim() }
                    $deg = [char]176  # grau - PS5.1 nao suporta \u{00B0}
                    if ($parts.Count -ge 7) {
                        $data.TempC     = if ($parts[0] -match '^\d+$') { "$($parts[0])$deg C" }    else { 'N/A' }
                        $data.CoreClock = if ($parts[1] -match '^\d+$') { "$($parts[1]) MHz" }       else { 'N/A' }
                        $data.MemClock  = if ($parts[2] -match '^\d+$') { "$($parts[2]) MHz" }       else { 'N/A' }
                        $data.FanSpeed  = if ($parts[3] -match '^\d+$') { "$($parts[3])%" }          else { 'N/A' }
                        $data.PowerDraw = if ($parts[4] -match '[\d.]+') { "$($parts[4]) W" }        else { 'N/A' }
                        $data.VramFree  = if ($parts[5] -match '^\d+$') {
                            "$([math]::Round([int]$parts[5]/1024, 1)) GB free"
                        } else { 'N/A' }
                        # Usar total do smi em vez do WMI (mais fiavel)
                        if ($parts[6] -match '^\d+$') {
                            $data.VramTotal = "$([math]::Round([int]$parts[6]/1024, 1)) GB"
                        }
                    }
                }
            }
        } catch {}
    }

    # AMD-specific: WMI extended
    if ($data.Vendor -eq 'AMD') {
        try {
            # AMD doesn't have a simple CLI tool like nvidia-smi
            # Try to get data from performance counters
            $amdCounter = '\GPU Adapter Memory(*)\Dedicated Usage'
            $s = Get-Counter $amdCounter -EA SilentlyContinue -MaxSamples 1 -SampleInterval 1
            if ($s) {
                $vramUsed = ($s.CounterSamples | Measure-Object CookedValue -Sum).Sum
                $data.VramFree = "$([math]::Round($vramUsed / 1GB, 1)) GB used"
            }
        } catch {}
    }

    return $data
}

# ── LATENCY MEASUREMENTS ──────────────────────────────────────────────────────
function Get-SystemTimerResolution {
    # Read current timer resolution from registry/perf
    try {
        Add-Type -TypeDefinition @'
using System;
using System.Runtime.InteropServices;
public class TimerRes {
    [DllImport("ntdll.dll")]
    public static extern int NtQueryTimerResolution(
        out uint MinimumResolution,
        out uint MaximumResolution,
        out uint CurrentResolution);
}
'@ -EA SilentlyContinue
        $min = 0; $max = 0; $cur = 0
        [TimerRes]::NtQueryTimerResolution([ref]$min, [ref]$max, [ref]$cur) | Out-Null
        return @{
            Min     = [math]::Round($min  / 10000.0, 2)  # convert 100ns units to ms
            Max     = [math]::Round($max  / 10000.0, 2)
            Current = [math]::Round($cur  / 10000.0, 2)
        }
    } catch {
        return @{ Min = 0.5; Max = 15.6; Current = 15.6 }
    }
}

function Measure-DiskLatency {
    # Measure single random 4KB read latency (simulates game asset streaming)
    try {
        $times = @()
        $testFile = "$env:TEMP\vt_lattest.tmp"
        # Write 10MB test file
        [System.IO.File]::WriteAllBytes($testFile, (New-Object byte[] (10 * 1024 * 1024)))
        $fs = [System.IO.File]::OpenRead($testFile)
        $buf = New-Object byte[] 4096
        $rng = New-Object System.Random
        for ($i = 0; $i -lt 20; $i++) {
            $pos = $rng.Next(0, 10 * 1024 * 1024 - 4096)
            $sw  = [System.Diagnostics.Stopwatch]::StartNew()
            $fs.Seek($pos, [System.IO.SeekOrigin]::Begin) | Out-Null
            $fs.Read($buf, 0, 4096) | Out-Null
            $sw.Stop()
            $times += $sw.Elapsed.TotalMilliseconds
        }
        $fs.Dispose()
        Remove-Item $testFile -Force -EA SilentlyContinue
        $avg = [math]::Round(($times | Measure-Object -Average).Average, 3)
        $max = [math]::Round(($times | Measure-Object -Maximum).Maximum, 3)
        return @{ AvgMs = $avg; MaxMs = $max; OK = $true }
    } catch { return @{ AvgMs = 0; MaxMs = 0; OK = $false } }
}

function Measure-MemoryLatency {
    # Sequential memory access latency
    try {
        $size = 64MB
        $buf  = New-Object byte[] $size
        $sw   = [System.Diagnostics.Stopwatch]::StartNew()
        # Read in 64-byte strides (cache line size)
        for ($i = 0; $i -lt $size; $i += 64) { $x = $buf[$i] }
        $sw.Stop()
        $nsPerAccess = [math]::Round(($sw.Elapsed.TotalNanoseconds) / ($size / 64), 2)
        return @{ NsPerAccess = $nsPerAccess; OK = $true }
    } catch { return @{ NsPerAccess = 0; OK = $false } }
}

function Measure-NetworkLatency($hosts) {
    $results = @()
    foreach ($h in $hosts) {
        try {
            $times = @()
            $ping = New-Object System.Net.NetworkInformation.Ping
            for ($i = 0; $i -lt 5; $i++) {
                try {
                    $r = $ping.Send($h, 1500)
                    if ($r.Status -eq 'Success') { $times += $r.RoundtripTime }
                } catch {}
            }
            if ($times.Count -gt 0) {
                $results += [PSCustomObject]@{
                    Host   = $h
                    AvgMs  = [math]::Round(($times | Measure-Object -Average).Average, 1)
                    MinMs  = ($times | Measure-Object -Minimum).Minimum
                    MaxMs  = ($times | Measure-Object -Maximum).Maximum
                    Jitter = [math]::Round(($times | Measure-Object -Maximum).Maximum - ($times | Measure-Object -Minimum).Minimum, 1)
                    OK     = $true
                }
            } else {
                $results += [PSCustomObject]@{ Host=$h; AvgMs=999; MinMs=999; MaxMs=999; Jitter=0; OK=$false }
            }
        } catch {
            $results += [PSCustomObject]@{ Host=$h; AvgMs=999; MinMs=999; MaxMs=999; Jitter=0; OK=$false }
        }
    }
    return $results
}

function Measure-DnsLatency($hosts) {
    $results = @()
    foreach ($h in $hosts) {
        try {
            $times = @()
            for ($i = 0; $i -lt 3; $i++) {
                $sw = [System.Diagnostics.Stopwatch]::StartNew()
                try { [System.Net.Dns]::GetHostEntry($h) | Out-Null } catch {}
                $sw.Stop()
                $times += $sw.Elapsed.TotalMilliseconds
            }
            $results += [PSCustomObject]@{
                Host  = $h
                AvgMs = [math]::Round(($times | Measure-Object -Average).Average, 1)
                OK    = $true
            }
        } catch {
            $results += [PSCustomObject]@{ Host=$h; AvgMs=999; OK=$false }
        }
    }
    return $results
}

# ── DPC LATENCY HEURISTIC ─────────────────────────────────────────────────────
# Real DPC latency requires kernel-level access (LatencyMon). We estimate
# by checking known problematic drivers and timer resolution.
function Get-DpcLatencyHeuristic {
    $issues = @()
    $score  = 100

    # Check timer resolution
    $timer = Get-SystemTimerResolution
    if ($timer.Current -gt 1.5) {
        $issues += "Timer resolution is $($timer.Current)ms (ideal: 0.5ms) -- apply TIMER RESOLUTION tweak"
        $score -= 20
    }

    # Check for known high-DPC drivers
    $badDrivers = @('USBHUB3.SYS','nvlddmkm.sys','dxgkrnl.sys','storport.sys')
    try {
        $loaded = Get-CimInstance Win32_SystemDriver -EA SilentlyContinue |
                  Where-Object { $_.State -eq 'Running' }
        foreach ($bd in $badDrivers) {
            $found = $loaded | Where-Object { $_.PathName -like "*$bd*" }
            if ($found) { $issues += "$bd is loaded (can cause DPC spikes on some systems)" }
        }
    } catch {}

    # Check power plan
    try {
        $plan = (powercfg -getactivescheme 2>$null)
        if ($plan -like '*Balanced*') {
            $issues += "Power plan is Balanced -- High Performance reduces CPU state transitions"
            $score -= 15
        }
    } catch {}

    # Check HPET
    try {
        $hpet = (bcdedit /enum 2>$null | Select-String 'useplatformclock')
        if ($hpet -match 'Yes') {
            $issues += "HPET is enabled -- can increase interrupt latency on modern CPUs"
            $score -= 10
        }
    } catch {}

    return @{
        Score   = [math]::Max(0, $score)
        Issues  = $issues
        Timer   = $timer
    }
}
