using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using VOIDTUNE.WinUI.Services;

namespace VOIDTUNE.WinUI.Tests;

/// <summary>
/// Detects tweaks silently fighting each other: two different tweaks writing DIFFERENT data to
/// the same registry value means whichever applies last wins and the other tweak's toggle state
/// silently lies. With 175+ hand-written tweaks this class of bug is inevitable without a
/// machine check — it can't be caught by eyeballing the catalog.
/// </summary>
public class TweakConflictTests
{
    // reg add "KEY" /v NAME /t TYPE /d DATA  — tolerate reordering of /t and /d
    private static readonly Regex RegAdd = new(
        @"reg\s+add\s+""(?<key>[^""]+)""\s+/v\s+(?<name>\S+)\s+(?:/t\s+\S+\s+)?/d\s+(?<data>\S+)",
        RegexOptions.IgnoreCase);

    [Fact]
    public void No_two_tweaks_write_different_data_to_the_same_registry_value()
    {
        // (key+value) -> list of (tweakId, data) from every APPLY command in the catalog.
        // Reverts are expected to overlap (many tweaks restore the same Windows default).
        var writes = new Dictionary<string, List<(string Id, string Data)>>(StringComparer.OrdinalIgnoreCase);

        foreach (var t in TweakCatalog.All)
        {
            // Restore tools exist to overwrite other tweaks' values — that's their job, not a conflict.
            if (t.Category == "Restore") continue;

            string cmd = t.ApplyCmd.StartsWith("PS:") ? t.ApplyCmd[3..] : t.ApplyCmd;
            foreach (Match m in RegAdd.Matches(cmd))
            {
                string slot = $"{m.Groups["key"].Value}::{m.Groups["name"].Value}";
                if (!writes.TryGetValue(slot, out var list)) writes[slot] = list = new();
                list.Add((t.Id, m.Groups["data"].Value.TrimEnd(';')));
            }
        }

        var conflicts = writes
            .Where(kv => kv.Value.Select(w => w.Data).Distinct(StringComparer.OrdinalIgnoreCase).Count() > 1)
            .Select(kv => $"{kv.Key} <- {string.Join(", ", kv.Value.Select(w => $"{w.Id}={w.Data}"))}")
            .ToList();

        Assert.True(conflicts.Count == 0,
            "Tweaks writing conflicting data to the same registry value (last-applied silently wins):\n  " +
            string.Join("\n  ", conflicts));
    }
}
