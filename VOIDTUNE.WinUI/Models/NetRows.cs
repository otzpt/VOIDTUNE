namespace VOIDTUNE.WinUI.Models;

// DevTools Network tab rows. Props must stay get/set — x:Bind in a DataTemplate
// generates setters in XamlTypeInfo (init-only breaks the build).

/// <summary>A network adapter summary.</summary>
public class NetAdapterRow
{
    public string Name { get; set; } = "";
    public string Detail { get; set; } = "";     // type · speed · status
    public string Addresses { get; set; } = "";  // IPv4 · gateway
    public string Dns { get; set; } = "";        // DNS servers
    public string StateHex { get; set; } = "#22C55E";
}

/// <summary>A latency test target result.</summary>
public class PingRow
{
    public string Target { get; set; } = "";
    public string Result { get; set; } = "—";
    public string Hex { get; set; } = "#9a93a8";
}

/// <summary>One TCP connection from the live connections view.</summary>
public class ConnRow
{
    public string Proc { get; set; } = "";
    public string Local { get; set; } = "";
    public string Remote { get; set; } = "";
    public string State { get; set; } = "";
    public string Hex { get; set; } = "#9a93a8";
}
