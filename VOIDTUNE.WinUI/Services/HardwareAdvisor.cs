using System;
using System.Collections.Generic;
using System.Linq;
using System.Management;
using System.Threading.Tasks;

namespace VOIDTUNE.WinUI.Services;

/// <summary>
/// Read-only checks for the big FPS levers VOIDTUNE cannot set itself (they live in the BIOS
/// or the GPU driver), surfaced as advice. The single biggest one: RAM running at JEDEC default
/// instead of its rated XMP/EXPO speed — worth far more FPS on most systems than any registry
/// tweak, and invisible unless something tells the user to look.
/// </summary>
public static class HardwareAdvisor
{
    /// <summary>Returns human-readable findings, empty when everything checks out.</summary>
    public static Task<List<string>> RunAsync() => Task.Run(() =>
    {
        var findings = new List<string>();
        try
        {
            // XMP/EXPO check: ConfiguredClockSpeed is what the memory controller is actually
            // running; Speed is the module's rated speed. Running meaningfully below rated
            // (>5% tolerance for rounding/gear-down) almost always means XMP/EXPO is off.
            using var searcher = new ManagementObjectSearcher(
                "SELECT Speed, ConfiguredClockSpeed FROM Win32_PhysicalMemory");
            var sticks = searcher.Get().Cast<ManagementObject>()
                .Select(m => (Rated: Convert.ToUInt32(m["Speed"] ?? 0u),
                              Running: Convert.ToUInt32(m["ConfiguredClockSpeed"] ?? 0u)))
                .Where(s => s.Rated > 0 && s.Running > 0)
                .ToList();

            if (sticks.Count > 0)
            {
                uint rated = sticks.Max(s => s.Rated);
                uint running = sticks.Min(s => s.Running);
                if (running < rated * 0.95)
                    findings.Add(
                        $"Your RAM is rated for {rated} MT/s but is running at {running} MT/s — " +
                        "XMP (Intel) / EXPO (AMD) is likely disabled in the BIOS. Enabling it is " +
                        "usually worth more FPS than every software tweak combined, especially for 1% lows.");
            }
        }
        catch { /* advisory only — never fail loudly */ }

        if (HardwareInfo.Cpu.IsRaptorLakeDesktop && MicrocodeRevision() is > 0 and < 0x12F)
            findings.Add("This 13th/14th gen CPU is on microcode older than 0x12F, which leaves it exposed to Intel's Vmin shift " +
                         "instability. Update your motherboard BIOS.");
        return findings;
    });

    // The revision is the high half of the 8-byte "Update Revision" value.
    private static uint MicrocodeRevision()
    {
        try
        {
            using var k = Microsoft.Win32.Registry.LocalMachine.OpenSubKey(@"HARDWARE\DESCRIPTION\System\CentralProcessor\0");
            return k?.GetValue("Update Revision") is byte[] { Length: >= 8 } b ? BitConverter.ToUInt32(b, 4) : 0;
        }
        catch { return 0; }
    }
}
