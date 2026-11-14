using System;
using System.Collections.Generic;
using System.Linq;
using SWLOR.NWN.API.NWScript.Enum.Item;

namespace SWLOR.Game.Server.Feature.MigrationDefinition;

/// <summary>
/// Gives owned Sandstorm Gaderffii staffs their Gaderffii look back. Player migration 23 and server migration 28 moved
/// unedited copies to a plain staff (bottom part 53) while the Gaderffii was not an appearance editor part; the
/// Gaderffii is now editor parts 62 (model 6, color 2) in every role. As in <see cref="ItemAppearanceCatalogMigration"/>,
/// weapon parts are model * 10 + color, and a copy converts only while every part this revision changes still
/// holds its old value, so a staff the player restyled keeps its look.
/// </summary>
public static class GaderffiiAppearanceMigration
{
    public static IReadOnlyDictionary<string, (BaseItem BaseItem, int[] Previous, int[] Current)> Revisions { get; } =
        new Dictionary<string, (BaseItem, int[], int[])>(StringComparer.OrdinalIgnoreCase)
        {
            ["sandstorm_gaffi"] = (BaseItem.QuarterStaff, new[] { 53, 11, 11 }, new[] { 62, 62, 62 }),
        };

    /// <summary>The parts to save when <paramref name="parts"/> still hold the plain staff look, otherwise null.</summary>
    internal static int[] GetUpdatedParts(string resref, BaseItem baseItem, IReadOnlyList<int> parts)
    {
        if (resref == null || !Revisions.TryGetValue(resref, out var revision) || revision.BaseItem != baseItem ||
            parts.Count != revision.Previous.Length)
            return null;
        return Enumerable.Range(0, parts.Count)
            .All(index => revision.Current[index] == revision.Previous[index] || parts[index] == revision.Previous[index])
            ? revision.Current
            : null;
    }
}
