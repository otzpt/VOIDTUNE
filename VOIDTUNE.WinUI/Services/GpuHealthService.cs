using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;

namespace VOIDTUNE.WinUI.Services;

/// <summary>One GPU's live health snapshot (ported from Get-GpuHealthData in modules/drivers.ps1).</summary>
public sealed class GpuHealth
{
    public string Name = "Unknown GPU";
    public string Vendor = "Unknown";
    public string DriverVer = "N/A";
    public string DriverDate = "N/A";
    public string VramTotal = "N/A";
    public string VramFree = "N/A";
    public string TempC = "N/A";
    public string CoreClock = "N/A";
    public string MemClock = "N/A";
    public string FanSpeed = "N/A";
    public string PowerDraw = "N/A";
}

/// <summary>A single labelled GPU metric for the stat grid.</summary>
public sealed record GpuStat(string Label, string Value);

public static class GpuHealthService
{
    public static async Task<GpuHealth> GetAsync()
    {
        var d = new GpuHealth
        {
            Name = HardwareInfo.GpuName,
            Vendor = HardwareInfo.GpuVendor,
        };

        // VRAM from the registry (accurate; WMI caps AdapterRAM at 4 GB on big cards).
        if (HardwareInfo.GpuVramBytes > 0)
            d.VramTotal = $"{Math.Round(HardwareInfo.GpuVramBytes / 1073741824.0, 1)} GB";

        // Driver version/date from the Win32_VideoController matching the chosen GPU.
        try
        {
            string ps = "PS:Get-CimInstance Win32_VideoController -EA SilentlyContinue | " +
                "Select-Object Name,DriverVersion,@{N='DDate';E={ if($_.DriverDate){$_.DriverDate.ToString('yyyy-MM-dd')}else{''} }},AdapterRAM | ConvertTo-Json -Compress";
            var r = await CommandRunner.ExecAsync(ps);
            if (r.Ok && !string.IsNullOrWhiteSpace(r.Output))
            {
                using var doc = JsonDocument.Parse(r.Output);
                var controllers = new List<JsonElement>();
                if (doc.RootElement.ValueKind == JsonValueKind.Array)
                    controllers.AddRange(doc.RootElement.EnumerateArray());
                else if (doc.RootElement.ValueKind == JsonValueKind.Object)
                    controllers.Add(doc.RootElement);

                // Prefer the controller whose Name matches the chosen primary GPU.
                JsonElement el = controllers.FirstOrDefault(
                    c => c.TryGetProperty("Name", out var n) && n.ValueKind == JsonValueKind.String &&
                         string.Equals(n.GetString(), d.Name, StringComparison.OrdinalIgnoreCase));
                if (el.ValueKind != JsonValueKind.Object && controllers.Count > 0) el = controllers[0];

                if (el.ValueKind == JsonValueKind.Object)
                {
                    if (el.TryGetProperty("DriverVersion", out var dv) && dv.ValueKind == JsonValueKind.String) d.DriverVer = dv.GetString() ?? "N/A";
                    if (el.TryGetProperty("DDate", out var dd) && dd.ValueKind == JsonValueKind.String && dd.GetString()!.Length > 0) d.DriverDate = dd.GetString()!;
                    if (d.VramTotal == "N/A" && el.TryGetProperty("AdapterRAM", out var ar) && ar.TryGetInt64(out long bytes) && bytes > 0)
                        d.VramTotal = $"{Math.Round(bytes / 1073741824.0, 1)} GB";
                }
            }
        }
        catch { /* ignore */ }

        // NVIDIA: pull live telemetry from nvidia-smi.
        if (d.Vendor == "NVIDIA")
            await TryNvidiaSmi(d);

        return d;
    }

    private static async Task TryNvidiaSmi(GpuHealth d)
    {
        try
        {
            const string args = "--query-gpu=temperature.gpu,clocks.current.graphics,clocks.current.memory,fan.speed,power.draw,memory.free,memory.total --format=csv,noheader,nounits";
            var r = await CommandRunner.ExecAsync($"nvidia-smi {args}");
            if (!r.Ok || string.IsNullOrWhiteSpace(r.Output)) return;
            string line = r.Output.Split('\n')[0];
            var p = new List<string>(line.Split(','));
            for (int i = 0; i < p.Count; i++) p[i] = p[i].Trim();
            if (p.Count >= 7)
            {
                if (IsNum(p[0])) d.TempC = $"{p[0]} °C";
                if (IsNum(p[1])) d.CoreClock = $"{p[1]} MHz";
                if (IsNum(p[2])) d.MemClock = $"{p[2]} MHz";
                if (IsNum(p[3])) d.FanSpeed = $"{p[3]} %";
                if (IsNum(p[4])) d.PowerDraw = $"{p[4]} W";
                if (IsNum(p[5])) d.VramFree = $"{Math.Round(double.Parse(p[5]) / 1024.0, 1)} GB free";
                if (IsNum(p[6])) d.VramTotal = $"{Math.Round(double.Parse(p[6]) / 1024.0, 1)} GB";
            }
        }
        catch { /* nvidia-smi not present */ }
    }

    private static bool IsNum(string s) => double.TryParse(s, System.Globalization.NumberStyles.Any,
        System.Globalization.CultureInfo.InvariantCulture, out _);
}
