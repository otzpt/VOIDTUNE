using System.Collections.Generic;
using System.Text.Json;
using System.Threading.Tasks;
using VOIDTUNE.WinUI.Models;

namespace VOIDTUNE.WinUI.Services;

/// <summary>Enumerates installed signed drivers via CIM (ported from modules/drivers.ps1, Get-DriverList).</summary>
public static class DriverService
{
    public static async Task<List<DriverItem>> GetDriversAsync()
    {
        var result = new List<DriverItem>();
        // Single-quoted PS so it survives CommandRunner's outer double quotes.
        const string ps = "PS:Get-CimInstance Win32_PnPSignedDriver -EA SilentlyContinue | " +
            "Where-Object { $_.DeviceName -and $_.DriverVersion } | " +
            "Select-Object DeviceName,DriverVersion,@{N='DDate';E={ if($_.DriverDate){$_.DriverDate.ToString('yyyy-MM-dd')}else{''} }},Manufacturer,DeviceClass | " +
            "Sort-Object DeviceClass,DeviceName | ConvertTo-Json -Compress";

        var r = await CommandRunner.ExecAsync(ps);
        if (!r.Ok || string.IsNullOrWhiteSpace(r.Output)) return result;

        // Defensive: slice out just the JSON payload in case any stray text (a stderr line, a
        // BOM, a progress record) is glued onto it — a single trailing char makes Parse throw.
        string json = ExtractJson(r.Output);
        if (string.IsNullOrEmpty(json)) return result;

        try
        {
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;
            if (root.ValueKind == JsonValueKind.Array)
            {
                foreach (var el in root.EnumerateArray()) Add(el, result);
            }
            else if (root.ValueKind == JsonValueKind.Object)
            {
                Add(root, result);
            }
        }
        catch { /* malformed json — return what we have */ }
        return result;
    }

    private static void Add(JsonElement el, List<DriverItem> list)
    {
        string cls = Str(el, "DeviceClass");
        list.Add(new DriverItem
        {
            Name = Str(el, "DeviceName"),
            Version = Str(el, "DriverVersion"),
            Date = Str(el, "DDate"),
            Mfg = Str(el, "Manufacturer"),
            Category = Categorize(cls),
        });
    }

    private static string Str(JsonElement el, string name)
        => el.TryGetProperty(name, out var p) && p.ValueKind == JsonValueKind.String ? p.GetString() ?? "" : "";

    /// <summary>Returns the substring from the first [ or { to its matching close, or "" if none.</summary>
    private static string ExtractJson(string s)
    {
        int start = s.IndexOfAny(new[] { '[', '{' });
        if (start < 0) return "";
        char open = s[start];
        char close = open == '[' ? ']' : '}';
        int end = s.LastIndexOf(close);
        return end > start ? s.Substring(start, end - start + 1) : "";
    }

    // Win32_PnPSignedDriver returns DeviceClass in upper-case on many systems (DISPLAY, NET,
    // AUDIOENDPOINT…), so match case-insensitively.
    private static string Categorize(string deviceClass) => deviceClass.ToUpperInvariant() switch
    {
        "DISPLAY" => "GPU",
        "MEDIA" or "AUDIOENDPOINT" or "AUDIOPROCESSINGOBJECT" => "Audio",
        "NET" => "Network",
        "USB" => "USB",
        "HDC" or "SCSIADAPTER" or "DISKDRIVE" or "VOLUME" => "Storage",
        "PROCESSOR" => "CPU",
        "SYSTEM" or "COMPUTER" or "FIRMWARE" or "SOFTWARECOMPONENT" or "SOFTWAREDEVICE" => "System",
        "HIDCLASS" or "KEYBOARD" or "MOUSE" => "Input",
        "MONITOR" => "Monitor",
        "CAMERA" => "Camera",
        "BLUETOOTH" => "Bluetooth",
        "SECURITYDEVICES" => "Security",
        "WPD" => "Portable",
        "" => "Other",
        _ => deviceClass,
    };
}
