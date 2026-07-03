using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Threading.Tasks;
using VOIDTUNE.WinUI.Models;

namespace VOIDTUNE.WinUI.Services;

/// <summary>
/// DevTools Network tab backend: adapter summary, latency tests, live TCP connections
/// and DNS quick-switching. Reads use .NET network APIs directly (no shelling out);
/// only the DNS switch runs PowerShell.
/// </summary>
public static class NetToolsService
{
    // ── adapters ────────────────────────────────────────────────────────────

    public static Task<List<NetAdapterRow>> GetAdaptersAsync() => Task.Run(() =>
    {
        var rows = new List<NetAdapterRow>();
        foreach (var nic in NetworkInterface.GetAllNetworkInterfaces())
        {
            if (nic.NetworkInterfaceType == NetworkInterfaceType.Loopback) continue;
            bool up = nic.OperationalStatus == OperationalStatus.Up;
            // hide the parade of down virtual adapters; keep anything that's up
            if (!up && nic.NetworkInterfaceType != NetworkInterfaceType.Ethernet
                    && nic.NetworkInterfaceType != NetworkInterfaceType.Wireless80211) continue;

            var props = nic.GetIPProperties();
            string ip4 = string.Join(", ", props.UnicastAddresses
                .Where(a => a.Address.AddressFamily == AddressFamily.InterNetwork)
                .Select(a => a.Address.ToString()));
            string gw = string.Join(", ", props.GatewayAddresses
                .Where(g => g.Address.AddressFamily == AddressFamily.InterNetwork)
                .Select(g => g.Address.ToString()));
            string dns = string.Join(", ", props.DnsAddresses
                .Where(d => d.AddressFamily == AddressFamily.InterNetwork)
                .Select(d => d.ToString()));

            string speed = up && nic.Speed > 0 ? $"{nic.Speed / 1_000_000} Mbps" : "";
            rows.Add(new NetAdapterRow
            {
                Name = nic.Name,
                Detail = $"{nic.NetworkInterfaceType} · {(up ? "UP" : "DOWN")}{(speed.Length > 0 ? " · " + speed : "")}",
                Addresses = ip4.Length > 0 ? $"IP {ip4}{(gw.Length > 0 ? "  ·  GW " + gw : "")}" : "no IPv4",
                Dns = dns.Length > 0 ? "DNS " + dns : "DNS —",
                StateHex = up ? "#22C55E" : "#9a93a8",
            });
        }
        return rows.OrderByDescending(r => r.StateHex == "#22C55E").ThenBy(r => r.Name).ToList();
    });

    /// <summary>First IPv4 gateway of an UP adapter — the latency test's first target.</summary>
    public static string? GetGateway()
    {
        try
        {
            return NetworkInterface.GetAllNetworkInterfaces()
                .Where(n => n.OperationalStatus == OperationalStatus.Up)
                .SelectMany(n => n.GetIPProperties().GatewayAddresses)
                .FirstOrDefault(g => g.Address.AddressFamily == AddressFamily.InterNetwork)
                ?.Address.ToString();
        }
        catch { return null; }
    }

    // ── latency ─────────────────────────────────────────────────────────────

    /// <summary>Pings a host 4 times; returns "avg ms (min–max)" plus a quality color.</summary>
    public static async Task<(string result, string hex)> PingAsync(string host)
    {
        var times = new List<long>();
        try
        {
            using var ping = new Ping();
            for (int i = 0; i < 4; i++)
            {
                var r = await ping.SendPingAsync(host, 1200);
                if (r.Status == IPStatus.Success) times.Add(r.RoundtripTime);
            }
        }
        catch { /* unreachable */ }

        if (times.Count == 0) return ("unreachable", "#EF4444");
        double avg = times.Average();
        string hex = avg <= 20 ? "#22C55E" : avg <= 60 ? "#F59E0B" : "#EF4444";
        string loss = times.Count < 4 ? $" · {4 - times.Count} lost" : "";
        return ($"{avg:0} ms ({times.Min()}–{times.Max()}){loss}", hex);
    }

    // ── live connections ────────────────────────────────────────────────────

    public static async Task<(List<ConnRow> rows, string summary)> GetConnectionsAsync()
    {
        var r = await CommandRunner.ExecAsync("netstat -ano -p TCP");
        var rows = new List<ConnRow>();
        int established = 0, listening = 0, other = 0;

        var procNames = new Dictionary<int, string>();
        foreach (var line in (r.Output ?? "").Split('\n'))
        {
            var parts = line.Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length < 5 || parts[0] != "TCP") continue;
            string state = parts[3];
            if (state == "ESTABLISHED") established++;
            else if (state == "LISTENING") listening++;
            else other++;

            if (!int.TryParse(parts[4], out int pid)) continue;
            if (!procNames.TryGetValue(pid, out string? pname))
            {
                try { pname = Process.GetProcessById(pid).ProcessName; }
                catch { pname = "pid " + pid; }
                procNames[pid] = pname;
            }

            // show active traffic first; skip the loopback chatter unless it's all there is
            if (rows.Count < 120)
            {
                rows.Add(new ConnRow
                {
                    Proc = pname,
                    Local = parts[1],
                    Remote = parts[2],
                    State = state,
                    Hex = state == "ESTABLISHED" ? "#22C55E" : state == "LISTENING" ? "#38BDF8" : "#9a93a8",
                });
            }
        }

        var ordered = rows
            .OrderBy(c => c.State switch { "ESTABLISHED" => 0, "LISTENING" => 1, _ => 2 })
            .ThenBy(c => c.Proc, StringComparer.OrdinalIgnoreCase)
            .ToList();
        return (ordered, $"{established} established · {listening} listening · {other} other");
    }

    // ── DNS quick switch ────────────────────────────────────────────────────

    public static Task<CommandResult> SetDnsAsync(string primary, string secondary) =>
        CommandRunner.ExecAsync(
            "PS:try { Get-NetAdapter -Physical | Where-Object Status -eq 'Up' | ForEach-Object { " +
            $"Set-DnsClientServerAddress -InterfaceIndex $_.ifIndex -ServerAddresses '{primary}','{secondary}' }} }} catch {{}}; " +
            "Clear-DnsClientCache; exit 0");

    public static Task<CommandResult> ResetDnsAsync() =>
        CommandRunner.ExecAsync(
            "PS:try { Get-NetAdapter -Physical | ForEach-Object { " +
            "Set-DnsClientServerAddress -InterfaceIndex $_.ifIndex -ResetServerAddresses } } catch {}; " +
            "Clear-DnsClientCache; exit 0");
}
