namespace VOIDTUNE.WinUI.Models;

/// <summary>One changed registry value in the DevTools Reg Diff tab. Props must stay
/// get/set — x:Bind in a DataTemplate generates setters in XamlTypeInfo.</summary>
public class DiffRow
{
    public string Change { get; set; } = "";      // ADDED / REMOVED / CHANGED
    public string Hex { get; set; } = "#9a93a8";
    public string Key { get; set; } = "";
    public string Name { get; set; } = "";
    public string Detail { get; set; } = "";      // "old → new" / value
}
