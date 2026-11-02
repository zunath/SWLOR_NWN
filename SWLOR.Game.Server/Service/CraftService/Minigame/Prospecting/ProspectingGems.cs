using System.Collections.Generic;
using System.Linq;
using SWLOR.Game.Server.Service.LootService;

namespace SWLOR.Game.Server.Service.CraftService.Minigame.Prospecting;

/// <summary>
/// The gems a node's loot table can hold. Gems are ordinary loot-table entries (a veldite vein can drop jade, a hyphae tree emerald), and
/// a gem pocket gives one of the table's own gem entries. A table with none, such as arkoxit's, has no gem pockets.
/// </summary>
public static class ProspectingGems
{
    /// <summary>The resrefs of the gem items harvesting tables hold, one for each tier.</summary>
    public static readonly IReadOnlyList<string> Resrefs = new[] { "jade", "agate", "citrine", "ruby", "emerald" };

    public static bool IsGem(string resref) => Resrefs.Contains(resref);

    /// <summary>The table's gem entries.</summary>
    public static IReadOnlyList<LootTableItem> GemEntries(LootTable table) =>
        table == null ? Array.Empty<LootTableItem>() : table.Where(item => IsGem(item.Resref)).ToList();

    /// <summary>The table's entries that are not gems: what an extra ore roll draws from.</summary>
    public static IReadOnlyList<LootTableItem> OreEntries(LootTable table) =>
        table == null ? Array.Empty<LootTableItem>() : table.Where(item => !IsGem(item.Resref)).ToList();

    /// <summary>True when the table has a gem entry, so its nodes can hold gem pockets.</summary>
    public static bool HasGems(LootTable table) => GemEntries(table).Count > 0;

    /// <summary>The share of a roll of the table that is a gem: the weight of its gem entries over its whole weight. 0 for a table with none.</summary>
    public static double GemChance(LootTable table)
    {
        if (table == null)
            return 0;

        var total = table.Sum(item => (double)item.Weight);
        return total <= 0 ? 0 : GemEntries(table).Sum(item => (double)item.Weight) / total;
    }
}
