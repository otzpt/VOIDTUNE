namespace VOIDTUNE.WinUI.Models;

/// <summary>A process row in the DevTools Core Affinity manager. Props must stay
/// get/set — x:Bind in a DataTemplate generates setters in XamlTypeInfo.</summary>
public class ProcRow
{
    public string Name { get; set; } = "";
    public int Pid { get; set; }
    public string Detail { get; set; } = "";   // RAM · CPU time · current affinity
    public string Hex { get; set; } = "#9a93a8";
}
