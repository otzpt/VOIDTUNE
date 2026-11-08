using System.Text.RegularExpressions;

namespace VOIDTUNE.WinUI.Services;

/// <summary>What the CPU's marketing name says about its generation and tuning constraints.</summary>
public sealed record CpuProfile(string Generation, bool IsDualCcdX3D, bool IsRaptorLakeDesktop)
{
    /// <summary>
    /// Dual-CCD X3D parts depend on Balanced-plan core parking to keep games on the cache die.
    /// Raptor Lake desktop chips are exposed to Vmin shift, so they avoid pinned high clocks.
    /// </summary>
    public bool PrefersBalancedPlan => IsDualCcdX3D || IsRaptorLakeDesktop;

    private static readonly Regex IntelCore = new(@"\bi([3579])-(\d{4,5})([A-Z]*)", RegexOptions.Compiled);
    private static readonly Regex IntelUltra = new(@"Ultra\s+[3579]\s+(\d)\d\d", RegexOptions.Compiled);
    private static readonly Regex Ryzen = new(@"Ryzen\s+(?:AI\s+)?([3579])\s+(\d)\d{3}([A-Z0-9]*)", RegexOptions.Compiled);

    public static CpuProfile Parse(string name)
    {
        var core = IntelCore.Match(name);
        if (core.Success)
        {
            string digits = core.Groups[2].Value;
            int gen = int.Parse(digits.Length == 5 ? digits[..2] : digits[..1]);
            string suffix = core.Groups[3].Value;
            // Intel scoped Vmin shift to desktop parts of 65 W and up.
            bool raptorDesktop = gen is 13 or 14 && core.Groups[1].Value != "3"
                && !suffix.StartsWith('H') && !suffix.StartsWith('P') && !suffix.StartsWith('U') && !suffix.StartsWith('T');
            return new CpuProfile($"Intel {gen}th gen", false, raptorDesktop);
        }

        var ultra = IntelUltra.Match(name);
        if (ultra.Success) return new CpuProfile($"Core Ultra series {ultra.Groups[1].Value}", false, false);

        var ryzen = Ryzen.Match(name);
        if (ryzen.Success)
        {
            string suffix = ryzen.Groups[3].Value;
            bool mobile = suffix.Length > 0 && "HUG".Contains(suffix[0]);
            string gen = (ryzen.Groups[2].Value, mobile) switch
            {
                ("5", false) => "Zen 3",
                ("7", false) => "Zen 4",
                ("9", false) => "Zen 5",
                _ => "Ryzen",
            };
            bool dualCcdX3D = ryzen.Groups[1].Value == "9" && suffix.Contains("X3D");
            return new CpuProfile(gen, dualCcdX3D, false);
        }

        return new CpuProfile("", false, false);
    }
}
