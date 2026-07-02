namespace VOIDTUNE.WinUI.Models;

/// <summary>One fixed drive on the Dashboard storage card. Props must stay get/set —
/// x:Bind in a DataTemplate generates setters in XamlTypeInfo (init-only breaks the build).</summary>
public class DiskRow
{
    public string Letter { get; set; } = "";
    public string Detail { get; set; } = "";
    public double Pct { get; set; }
}
