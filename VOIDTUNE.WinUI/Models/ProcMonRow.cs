namespace VOIDTUNE.WinUI.Models;

/// <summary>A live process row in the DevTools Process Monitor. Props stay get/set —
/// x:Bind in a DataTemplate generates setters in XamlTypeInfo (init-only breaks the build).</summary>
public class ProcMonRow
{
    public string Name { get; set; } = "";
    public int Pid { get; set; }
    public string Cpu { get; set; } = "";     // "12%"
    public string Ram { get; set; } = "";     // "340 MB"
    public double Bar { get; set; }            // 0–100 relative impact (bar width)
    public string Hex { get; set; } = "#A78BFA";
    public bool Killable { get; set; } = true; // false for self / critical system processes
}
