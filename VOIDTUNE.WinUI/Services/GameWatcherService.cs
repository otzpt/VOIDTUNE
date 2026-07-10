using System;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using System.Threading;

namespace VOIDTUNE.WinUI.Services;

/// <summary>
/// The automatic half of VoidScheduler: a lightweight app-wide watcher (one 3-second timer)
/// that detects when a fullscreen game takes the foreground, boosts it through
/// <see cref="VoidSchedulerService"/>, and restores everything the moment the game exits.
/// Enabled by the "Auto Game Boost" tweak; state persists in settings.json.
/// Every code path is guarded — the watcher must never throw or add measurable load.
/// </summary>
public static class GameWatcherService
{
    private static Timer? _timer;
    private static int _ticking;                 // reentrancy guard
    private static readonly object _gate = new();

    public static bool IsRunning { get { lock (_gate) return _timer != null; } }

    public static void Start()
    {
        lock (_gate)
        {
            if (_timer != null) return;
            _timer = new Timer(Tick, null, TimeSpan.FromSeconds(3), TimeSpan.FromSeconds(3));
            TweakEngine.Instance.EmitLog("Auto Game Boost: watching for fullscreen games…");
        }
    }

    public static void Stop()
    {
        lock (_gate)
        {
            if (_timer == null) return;
            _timer.Dispose();
            _timer = null;
        }
        // undo any active boost so "off" really means back-to-default
        int n = VoidSchedulerService.Instance.RestoreAll();
        RestorePowerPlanNow();
        TweakEngine.Instance.EmitLog($"Auto Game Boost: off{(n > 0 ? $" — restored {n} processes" : "")}.");
    }

    private static void Tick(object? state)
    {
        if (Interlocked.Exchange(ref _ticking, 1) == 1) return;   // skip overlapping ticks
        try
        {
            var sched = VoidSchedulerService.Instance;

            // A game is already being handled (boosted and/or power-switched): wait for it to
            // exit, then restore everything automatically.
            int watched = sched.IsActive ? sched.BoostedGamePid : _powerGamePid;
            if (watched > 0)
            {
                if (!IsAlive(watched))
                {
                    if (sched.IsActive)
                    {
                        string? name = sched.BoostedGameName;
                        int n = sched.RestoreAll();
                        TweakEngine.Instance.EmitLog($"Auto Game Boost: {name} closed — restored {n} processes.");
                    }
                    RestorePowerPlanNow();
                    _powerGamePid = 0;
                }
                return;
            }

            int pid = DetectFullscreenGame();
            if (pid <= 0) return;

            if (AppSettingsStore.AutoGameBoost)
            {
                // pushBackground:false — never move other processes onto a core subset. That crammed
                // background apps (incl. the game's own helpers, ShareX, browsers) onto a few cores and
                // hurt FPS/hotkeys. The boost is now purely High priority + EcoQoS off on the game.
                var (ok, msg) = sched.BoostGame(pid, pushBackground: false);
                if (ok) TweakEngine.Instance.EmitLog("Auto Game Boost: " + msg);
            }

            if (AppSettingsStore.GameTimePowerPlan)
            {
                _powerGamePid = pid;
                SwitchToVoidPlan();
            }
        }
        catch { /* the watcher must never crash the app */ }
        finally { Interlocked.Exchange(ref _ticking, 0); }
    }

    // ── game-time power plan ────────────────────────────────────────────────
    // The Bitsum-recommended pattern: run the aggressive plan only WHILE a game is running,
    // instead of 24/7 — desktops get max clocks during play, and idle/browsing keeps normal
    // power draw and heat. The VOIDTUNE plan itself is hardware-aware (Balanced-based on
    // laptops), so this switch is safe on both.

    private static int _powerGamePid;
    private static string? _savedSchemeGuid;
    private static bool _warnedNoPlan;
    private static readonly Regex SchemeGuid = new(@"[0-9a-fA-F]{8}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{12}");

    private static void SwitchToVoidPlan()
    {
        try
        {
            var m = Regex.Match(RunPowercfg("-list"), SchemeGuid + @"(?=[^\r\n]*\(VOIDTUNE Performance\))");
            if (!m.Success)
            {
                if (!_warnedNoPlan)
                {
                    _warnedNoPlan = true;
                    TweakEngine.Instance.EmitLog("Game-Time Power Plan: the \"VOIDTUNE Power Plan\" tweak isn't applied — nothing to switch to.");
                }
                return;
            }

            var active = SchemeGuid.Match(RunPowercfg("-getactivescheme"));
            if (!active.Success || string.Equals(active.Value, m.Value, StringComparison.OrdinalIgnoreCase)) return;

            _savedSchemeGuid = active.Value;
            RunPowercfg("-setactive " + m.Value);
            TweakEngine.Instance.EmitLog("Game-Time Power Plan: switched to VOIDTUNE Performance for the game.");
        }
        catch { /* power switching is best-effort; never disturb the watcher */ }
    }

    /// <summary>Puts the pre-game power plan back, if we switched. Safe to call any time.</summary>
    public static void RestorePowerPlanNow()
    {
        try
        {
            string? saved = Interlocked.Exchange(ref _savedSchemeGuid, null);
            if (saved is null) return;
            RunPowercfg("-setactive " + saved);
            TweakEngine.Instance.EmitLog("Game-Time Power Plan: restored your previous power plan.");
        }
        catch { /* best-effort */ }
    }

    private static string RunPowercfg(string args)
    {
        var psi = new ProcessStartInfo("powercfg", args)
        {
            RedirectStandardOutput = true,
            UseShellExecute = false,
            CreateNoWindow = true,
        };
        using var p = Process.Start(psi)!;
        string output = p.StandardOutput.ReadToEnd();
        p.WaitForExit(5000);
        return output;
    }

    // ── fullscreen game detection ───────────────────────────────────────────

    /// <summary>Processes that fullscreen legitimately but are never games.</summary>
    private static readonly string[] NotGames =
    {
        "chrome", "msedge", "firefox", "brave", "opera", "opera_gx", "vivaldi", "arc",
        "vlc", "mpc-hc64", "mpc-hc", "wmplayer", "spotify", "discord",
        "explorer", "searchhost", "lockapp", "dwm", "voidtune", "devenv", "code",
        "obs64", "obs32", "powerpnt", "acrord32", "msteams", "teams",
    };

    /// <summary>Returns the PID of a fullscreen, game-looking foreground process, or 0.</summary>
    private static int DetectFullscreenGame()
    {
        IntPtr hwnd = GetForegroundWindow();
        if (hwnd == IntPtr.Zero) return 0;

        GetWindowThreadProcessId(hwnd, out uint upid);
        int pid = (int)upid;
        if (pid <= 4 || pid == Environment.ProcessId) return 0;

        try
        {
            using var p = Process.GetProcessById(pid);
            string name = p.ProcessName.ToLowerInvariant();
            if (NotGames.Contains(name)) return 0;
            if (p.SessionId == 0) return 0;
            if (p.WorkingSet64 < 300L * 1024 * 1024) return 0;      // games are heavy; helpers aren't

            // fullscreen / borderless check: window rect covers its monitor (±2 px)
            if (!GetWindowRect(hwnd, out RECT wr)) return 0;
            IntPtr mon = MonitorFromWindow(hwnd, 2 /* MONITOR_DEFAULTTONEAREST */);
            var mi = new MONITORINFO { cbSize = Marshal.SizeOf<MONITORINFO>() };
            if (!GetMonitorInfo(mon, ref mi)) return 0;

            bool covers =
                wr.Left <= mi.rcMonitor.Left + 2 && wr.Top <= mi.rcMonitor.Top + 2 &&
                wr.Right >= mi.rcMonitor.Right - 2 && wr.Bottom >= mi.rcMonitor.Bottom - 2;
            return covers ? pid : 0;
        }
        catch { return 0; }
    }

    private static bool IsAlive(int pid)
    {
        if (pid <= 0) return false;
        try { using var p = Process.GetProcessById(pid); return !p.HasExited; }
        catch { return false; }
    }

    // ── native ──────────────────────────────────────────────────────────────

    [StructLayout(LayoutKind.Sequential)]
    private struct RECT { public int Left, Top, Right, Bottom; }

    [StructLayout(LayoutKind.Sequential)]
    private struct MONITORINFO
    {
        public int cbSize;
        public RECT rcMonitor;
        public RECT rcWork;
        public uint dwFlags;
    }

    [DllImport("user32.dll")] private static extern IntPtr GetForegroundWindow();
    [DllImport("user32.dll")] private static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint pid);
    [DllImport("user32.dll")] private static extern bool GetWindowRect(IntPtr hWnd, out RECT rect);
    [DllImport("user32.dll")] private static extern IntPtr MonitorFromWindow(IntPtr hWnd, uint flags);
    [DllImport("user32.dll", CharSet = CharSet.Unicode)] private static extern bool GetMonitorInfo(IntPtr hMonitor, ref MONITORINFO mi);
}
