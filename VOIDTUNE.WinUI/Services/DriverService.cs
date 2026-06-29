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

        try
        {
            using var doc = JsonDocument.Parse(r.Output);
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

    private static string Categorize(string deviceClass) => deviceClass switch
    {
        "Display" => "GPU",
        "MEDIA" or "AudioEndpoint" => "Audio",
        "Net" => "Network",
        "USB" => "USB",
        "HDC" or "SCSIAdapter" or "DiskDrive" => "Storage",
        "Processor" => "CPU",
        "System" => "System",
        "HIDClass" or "Keyboard" or "Mouse" => "Input",
        "Bluetooth" => "Bluetooth",
        "" => "Other",
        _ => deviceClass,
    };
}
