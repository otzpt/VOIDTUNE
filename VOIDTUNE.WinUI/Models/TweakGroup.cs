using System.Collections.Generic;

namespace VOIDTUNE.WinUI.Models;

/// <summary>A category section on the Tweaks page (grouped ListView). Props must stay
/// get/set — x:Bind in the group header template generates setters in XamlTypeInfo.</summary>
public class TweakGroup : List<Tweak>
{
    public string Name { get; set; } = "";
    public string Glyph { get; set; } = "";
    public string Hex { get; set; } = "#A78BFA";
    public string BgHex { get; set; } = "#26A78BFA";
    public string CountLabel { get; set; } = "";
}
