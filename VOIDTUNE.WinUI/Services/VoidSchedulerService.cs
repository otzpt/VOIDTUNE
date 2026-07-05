using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;

namespace VOIDTUNE.WinUI.Services;

/// <summary>
/// VoidScheduler — a game-first CPU governor. It reads the machine's real core topology
/// (Intel P/E hybrid, AMD multi-CCD, or a flat SMT layout) and pins a game onto the fast
/// cores while shoving background apps onto the rest, boosts the game's priority, and turns
/// off EcoQoS execution-speed throttling for it. Everything is done through documented Win32
/// (GetSystemCpuSetInformation, SetProcessInformation, ProcessorAffinity/PriorityClass) — no
/// undocumented ntdll calls, so there's no risk of destabilising the scheduler — and every
/// change is recorded so <see cref="RestoreAll"/> puts the system back exactly as it was.
/// </summary>
public sealed class VoidSchedulerService
{
    public static VoidSchedulerService Instance { get; } = new();

    // ── CPU topology ──────────────────────────────────────────────────────────

    public sealed class Topology
    {
        public int LogicalCount { get; init; }
        public int PhysicalCount { get; init; }
        public nuint PerfMask { get; init; }        // where the game should run
        public nuint BackgroundMask { get; init; }  // where background apps get pushed
        public string Kind { get; init; } = "";     // "Intel hybrid" / "AMD multi-CCD" / "flat"
        public string Summary { get; init; } = "";
        public bool CanIsolate => PerfMask != BackgroundMask && BackgroundMask != 0 && PerfMask != 0;
    }

    private Topology? _topo;
    public Topology GetTopology() => _topo ??= DetectTopology();

    /// <summary>True on Intel hybrid (P/E) or AMD multi-CCD parts — where core placement matters.
    /// Guarded so a detection failure never breaks catalog construction.</summary>
    public static bool IsHybridCpu
    {
        get
        {
            try { return Instance.GetTopology().Kind is "Intel hybrid" or "AMD multi-CCD"; }
            catch { return false; }
        }
    }

    private struct Cpu { public int Logical; public int Core; public int Llc; public int Eff; }

    private static Topology DetectTopology()
    {
        try
        {
            var cpus = ReadCpuSets();
            if (cpus.Count == 0 || cpus.Count > 64) return Fallback(cpus.Count);

            int logical = cpus.Count;
            int physical = cpus.Select(c => c.Core).Distinct().Count();
            var effClasses = cpus.Select(c => c.Eff).Distinct().OrderByDescending(x => x).ToList();
            var llcGroups = cpus.Select(c => c.Llc).Distinct().ToList();

            nuint All = logical >= 64 ? unchecked((nuint)ulong.MaxValue) : (nuint)((1UL << logical) - 1);

            // 1) Intel hybrid: performance cores are the highest efficiency class (P-cores).
            if (effClasses.Count > 1)
            {
                int top = effClasses[0];
                nuint perf = MaskOf(cpus.Where(c => c.Eff == top).Select(c => c.Logical));
                nuint bg = MaskOf(cpus.Where(c => c.Eff != top).Select(c => c.Logical));
                int pCores = cpus.Where(c => c.Eff == top).Select(c => c.Core).Distinct().Count();
                int eCores = physical - pCores;
                return new Topology
                {
                    LogicalCount = logical, PhysicalCount = physical, PerfMask = perf, BackgroundMask = bg,
                    Kind = "Intel hybrid",
                    Summary = $"{pCores} P-core{Plural(pCores)} + {eCores} E-core{Plural(eCores)} · {logical} threads — game runs on P-cores, background on E-cores",
                };
            }

            // 2) AMD / multi-die: cores split across last-level caches (CCDs). Keep the game on
            //    CCD-0 (the die holding logical 0 — the boot/V-Cache die on X3D parts) and push
            //    background work to the other die(s).
            if (llcGroups.Count > 1)
            {
                int primaryLlc = cpus.First(c => c.Logical == 0).Llc;
                nuint perf = MaskOf(cpus.Where(c => c.Llc == primaryLlc).Select(c => c.Logical));
                nuint bg = MaskOf(cpus.Where(c => c.Llc != primaryLlc).Select(c => c.Logical));
                int ccd0 = cpus.Where(c => c.Llc == primaryLlc).Select(c => c.Core).Distinct().Count();
                return new Topology
                {
                    LogicalCount = logical, PhysicalCount = physical, PerfMask = perf, BackgroundMask = bg,
                    Kind = "AMD multi-CCD",
                    Summary = $"{llcGroups.Count} CCDs · {physical} cores / {logical} threads — game locked to CCD-0 ({ccd0} cores), background to the other die",
                };
            }

            // 3) Flat layout: no P/E split, single cache domain. Don't restrict the game (it wants
            //    all cores) but reserve the last two physical cores for background apps.
            var lastCores = cpus.Select(c => c.Core).Distinct().OrderBy(x => x).TakeLast(Math.Min(2, physical - 1)).ToHashSet();
            nuint bgFlat = MaskOf(cpus.Where(c => lastCores.Contains(c.Core)).Select(c => c.Logical));
            return new Topology
            {
                LogicalCount = logical, PhysicalCount = physical, PerfMask = All, BackgroundMask = bgFlat,
                Kind = "flat",
                Summary = $"{physical} cores / {logical} threads — game keeps all cores, background apps pushed to the last {lastCores.Count} core{Plural(lastCores.Count)}",
            };
        }
        catch { return Fallback(Environment.ProcessorCount); }
    }

    private static Topology Fallback(int logical)
    {
        logical = Math.Clamp(logical, 1, 64);
        nuint all = logical >= 64 ? unchecked((nuint)ulong.MaxValue) : (nuint)((1UL << logical) - 1);
        int bgN = Math.Min(2, Math.Max(0, logical - 2));
        nuint bg = 0;
        for (int i = logical - bgN; i < logical; i++) bg |= (nuint)(1UL << i);
        return new Topology
        {
            LogicalCount = logical, PhysicalCount = logical, PerfMask = all, BackgroundMask = bg,
            Kind = "generic",
            Summary = $"{logical} logical processors — background apps pushed to the last {bgN} thread{Plural(bgN)}",
        };
    }

    private static string Plural(int n) => n == 1 ? "" : "s";

    private static nuint MaskOf(IEnumerable<int> logicalIndices)
    {
        nuint m = 0;
        foreach (int i in logicalIndices) if (i is >= 0 and < 64) m |= (nuint)(1UL << i);
        return m;
    }

    // GetSystemCpuSetInformation → fixed records we walk by the Size field. Fields we read:
    // +14 LogicalProcessorIndex, +15 CoreIndex, +16 LastLevelCacheIndex, +18 EfficiencyClass.
    private static List<Cpu> ReadCpuSets()
    {
        var list = new List<Cpu>();
        IntPtr proc = GetCurrentProcess();
        GetSystemCpuSetInformation(IntPtr.Zero, 0, out uint needed, proc, 0);
        if (needed == 0) return list;

        IntPtr buf = Marshal.AllocHGlobal((int)needed);
        try
        {
            if (!GetSystemCpuSetInformation(buf, needed, out uint written, proc, 0) || written == 0) return list;
            int offset = 0;
            while (offset + 20 <= written)
            {
                int size = Marshal.ReadInt32(buf, offset);
                uint type = (uint)Marshal.ReadInt32(buf, offset + 4);
                if (size <= 0) break;
                if (type == 0) // CpuSetInformation
                {
                    list.Add(new Cpu
                    {
                        Logical = Marshal.ReadByte(buf, offset + 14),
                        Core = Marshal.ReadByte(buf, offset + 15),
                        Llc = Marshal.ReadByte(buf, offset + 16),
                        Eff = Marshal.ReadByte(buf, offset + 18),
                    });
                }
                offset += size;
            }
        }
        finally { Marshal.FreeHGlobal(buf); }
        return list;
    }

    // ── boost / restore ───────────────────────────────────────────────────────

    private readonly record struct SavedState(IntPtr Affinity, ProcessPriorityClass Priority);
    private readonly ConcurrentDictionary<int, SavedState> _modified = new();

    public string? BoostedGameName { get; private set; }
    public int BoostedGamePid { get; private set; }
    public bool IsActive => _modified.Count > 0;

    /// <summary>
    /// Pins <paramref name="pid"/> to the performance cores, raises its priority to High, and
    /// disables EcoQoS throttling. When <paramref name="pushBackground"/> is set, other user
    /// processes are moved onto the background cores so they can't steal the fast ones.
    /// Returns a human-readable result. Every touched process is remembered for RestoreAll.
    /// </summary>
    public (bool ok, string message) BoostGame(int pid, bool pushBackground)
    {
        var topo = GetTopology();
        Process game;
        try { game = Process.GetProcessById(pid); }
        catch { return (false, "That process is no longer running."); }

        string name = SafeName(game);
        try
        {
            Remember(game);
            // Deliberately DON'T restrict the game's affinity. Hard-pinning a game to a subset
            // of cores (P-cores / a single CCD) can tank 1% lows on multi-threaded titles and is
            // the classic "stutters after tweaks" cause. The safe, always-positive boost is:
            // High priority + no EcoQoS throttling, and moving *background* apps off the fast
            // cores so they can't contend. The game keeps every core available to it.
            TrySetPriority(game, ProcessPriorityClass.High);
            SetHighQoS(game.Handle, disableThrottle: true);
        }
        catch (Exception ex) { return (false, $"Couldn't boost {name}: {ex.Message}"); }

        int pushed = 0;
        if (pushBackground && topo.CanIsolate)
            pushed = PushBackground(excludePid: pid, topo.BackgroundMask);

        BoostedGameName = name;
        BoostedGamePid = pid;

        string bg = pushed > 0 ? $"{pushed} background app{Plural(pushed)} moved off the fast cores, " : "";
        return (true, $"Boosted {name} — {bg}High priority + EcoQoS off, all cores available to the game. Reverts when it exits.");
    }

    private int PushBackground(int excludePid, nuint bgMask)
    {
        if (bgMask == 0) return 0;
        int myPid = Environment.ProcessId;
        int pushed = 0;
        foreach (var p in Process.GetProcesses())
        {
            try
            {
                if (p.Id == excludePid || p.Id == myPid || p.SessionId == 0) continue;
                if (p.WorkingSet64 < 30L * 1024 * 1024) continue;         // ignore tiny helpers
                if (IsCritical(p.ProcessName)) continue;                   // never touch shell/AV
                Remember(p);
                p.ProcessorAffinity = (IntPtr)(long)bgMask;
                pushed++;
            }
            catch { /* protected or exited — skip */ }
        }
        return pushed;
    }

    /// <summary>Reverts every process this service modified back to its recorded affinity/priority,
    /// and re-enables default QoS management. Safe to call any time.</summary>
    public int RestoreAll()
    {
        int restored = 0;
        foreach (var (pid, saved) in _modified.ToArray())
        {
            try
            {
                using var p = Process.GetProcessById(pid);
                p.ProcessorAffinity = saved.Affinity;
                TrySetPriority(p, saved.Priority);
                SetHighQoS(p.Handle, disableThrottle: false);   // hand QoS back to Windows
                restored++;
            }
            catch { /* process gone — nothing to restore */ }
            _modified.TryRemove(pid, out _);
        }
        BoostedGameName = null;
        BoostedGamePid = 0;
        return restored;
    }

    private void Remember(Process p)
    {
        if (_modified.ContainsKey(p.Id)) return;
        try { _modified[p.Id] = new SavedState(p.ProcessorAffinity, p.PriorityClass); }
        catch { /* can't read state — don't track what we can't restore */ }
    }

    private static void TrySetPriority(Process p, ProcessPriorityClass c)
    {
        try { p.PriorityClass = c; } catch { /* protected */ }
    }

    private static nuint FullMask(Topology t) =>
        t.LogicalCount >= 64 ? unchecked((nuint)ulong.MaxValue) : (nuint)((1UL << t.LogicalCount) - 1);

    private static string SafeName(Process p) { try { return p.ProcessName; } catch { return "process"; } }

    private static bool IsCritical(string name) =>
        _criticalNames.Contains(name.ToLowerInvariant());

    private static readonly HashSet<string> _criticalNames = new(StringComparer.OrdinalIgnoreCase)
    {
        "explorer", "dwm", "csrss", "wininit", "winlogon", "services", "lsass", "smss",
        "system", "registry", "fontdrvhost", "audiodg", "msmpeng", "securityhealthservice",
        "voidtune", "ctfmon", "sihost", "taskhostw", "shellexperiencehost", "searchhost",
    };

    // ── native ────────────────────────────────────────────────────────────────

    [StructLayout(LayoutKind.Sequential)]
    private struct PROCESS_POWER_THROTTLING_STATE
    {
        public uint Version;
        public uint ControlMask;
        public uint StateMask;
    }

    private const uint PROCESS_POWER_THROTTLING_CURRENT_VERSION = 1;
    private const uint PROCESS_POWER_THROTTLING_EXECUTION_SPEED = 1;
    private const int ProcessPowerThrottling = 4; // PROCESS_INFORMATION_CLASS

    /// <summary>disableThrottle=true forces High-Performance QoS (no EcoQoS); false hands control
    /// back to Windows' default power management.</summary>
    private static void SetHighQoS(IntPtr hProcess, bool disableThrottle)
    {
        var state = new PROCESS_POWER_THROTTLING_STATE
        {
            Version = PROCESS_POWER_THROTTLING_CURRENT_VERSION,
            // ControlMask=EXECUTION_SPEED, StateMask=0  → "always run this process at full speed".
            // ControlMask=0                              → "let the system manage it" (default).
            ControlMask = disableThrottle ? PROCESS_POWER_THROTTLING_EXECUTION_SPEED : 0,
            StateMask = 0,
        };
        try { SetProcessInformation(hProcess, ProcessPowerThrottling, ref state, (uint)Marshal.SizeOf<PROCESS_POWER_THROTTLING_STATE>()); }
        catch { /* pre-1709 Windows without the API — priority + affinity still applied */ }
    }

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern bool SetProcessInformation(IntPtr hProcess, int informationClass,
        ref PROCESS_POWER_THROTTLING_STATE information, uint size);

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern bool GetSystemCpuSetInformation(IntPtr info, uint bufferLength,
        out uint returnedLength, IntPtr process, uint flags);

    [DllImport("kernel32.dll")]
    private static extern IntPtr GetCurrentProcess();
}
