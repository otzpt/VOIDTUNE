using System.Collections.Generic;
using System.Linq;
using VOIDTUNE.WinUI.Models;
using VOIDTUNE.WinUI.Services;

namespace VOIDTUNE.WinUI.Tests;

/// <summary>
/// Structural guardrails for the tweak catalog. These exist because the catalog is hand-edited
/// C# data (167+ entries) with no compiler-enforced invariants beyond "it's a valid object
/// initializer" — a copy-paste can silently duplicate a tweak (this caught a real one: "p6" was
/// an exact duplicate of "deb3", both toggling AllowCortana) or drop a revert command.
/// </summary>
public class TweakCatalogIntegrityTests
{
    /// <summary>
    /// IDs that legitimately have no RevertCmd because they ARE one-shot actions (a cache flush,
    /// a TRIM pass, a page-file recalculation) or restore/reset actions whose entire purpose is
    /// to be the "undo" for other tweaks (Category "Restore"). Any new tweak with an empty
    /// RevertCmd must either get a real revert or be added here deliberately.
    /// </summary>
    private static readonly HashSet<string> OneShotIds = new()
    {
        "ram3",  // Clear Temp & DNS
        "ram7",  // Optimal Page File
        "stor3", // TRIM All SSDs
        "pow3",  // Ultimate Performance — undone via rst2 "Restore Power"
        "rst1", "rst2", "rst3", "rst4", "rst5", "rst6", // Restore category
    };

    private static IReadOnlyList<Tweak> All => TweakCatalog.All;

    [Fact]
    public void Catalog_is_not_empty()
    {
        Assert.True(All.Count > 0, "TweakCatalog.All returned zero tweaks.");
    }

    [Fact]
    public void Every_tweak_has_a_unique_id()
    {
        var dupes = All.GroupBy(t => t.Id)
                        .Where(g => g.Count() > 1)
                        .Select(g => g.Key)
                        .ToList();

        Assert.True(dupes.Count == 0, $"Duplicate tweak IDs found: {string.Join(", ", dupes)}");
    }

    [Fact]
    public void Every_tweak_has_required_fields_populated()
    {
        var bad = All.Where(t =>
            string.IsNullOrWhiteSpace(t.Id) ||
            string.IsNullOrWhiteSpace(t.Category) ||
            string.IsNullOrWhiteSpace(t.Name) ||
            string.IsNullOrWhiteSpace(t.Description) ||
            string.IsNullOrWhiteSpace(t.ApplyCmd))
            .Select(t => t.Id)
            .ToList();

        Assert.True(bad.Count == 0, $"Tweaks missing required fields: {string.Join(", ", bad)}");
    }

    [Fact]
    public void Every_tweak_has_a_revert_unless_explicitly_one_shot()
    {
        var missing = All.Where(t => string.IsNullOrWhiteSpace(t.RevertCmd) && !OneShotIds.Contains(t.Id))
                          .Select(t => t.Id)
                          .ToList();

        Assert.True(missing.Count == 0,
            $"Tweaks with no RevertCmd and not in the one-shot allowlist: {string.Join(", ", missing)}. " +
            "Add a real revert, or add the ID to OneShotIds if it's genuinely one-way.");
    }

    [Fact]
    public void No_two_tweaks_apply_and_revert_the_same_way()
    {
        // Catches accidental duplicates like "p6"/"deb3" (both toggled AllowCortana identically)
        // where the same effect was added twice under different IDs/names.
        var dupes = All.Where(t => !string.IsNullOrWhiteSpace(t.RevertCmd))
                        .GroupBy(t => (t.ApplyCmd, t.RevertCmd))
                        .Where(g => g.Count() > 1)
                        .Select(g => string.Join(" == ", g.Select(t => t.Id)))
                        .ToList();

        Assert.True(dupes.Count == 0, $"Tweaks with byte-identical Apply+Revert commands: {string.Join("; ", dupes)}");
    }

    [Fact]
    public void OneShotIds_allowlist_has_no_stale_entries()
    {
        // If a tweak in the allowlist gets removed from the catalog or given a real revert,
        // the allowlist should shrink with it — otherwise it silently stops meaning anything.
        var catalogIds = All.Select(t => t.Id).ToHashSet();
        var stale = OneShotIds.Where(id => !catalogIds.Contains(id)).ToList();

        Assert.True(stale.Count == 0, $"OneShotIds references IDs no longer in the catalog: {string.Join(", ", stale)}");
    }

    [Fact]
    public void MinBuild_is_never_negative()
    {
        var bad = All.Where(t => t.MinBuild < 0).Select(t => t.Id).ToList();
        Assert.True(bad.Count == 0, $"Tweaks with negative MinBuild: {string.Join(", ", bad)}");
    }
}
