using System.Collections.Generic;
using System.Linq;
using SWLOR.Game.Server.Service.FarmingService;
using SWLOR.Game.Server.Service.LootService;

namespace SWLOR.Game.Server.Service.CraftService.Minigame.Prospecting;

/// <summary>
/// The gems a node's loot table can hold. Gems are ordinary loot-table entries (a veldite vein can drop jade, a hyphae tree emerald), and
/// a gem pocket gives one of the table's own gem entries. A table with none, such as arkoxit's, has no gem pockets. A tree's table
/// also holds its woodlot sapling, a rare drop that is neither ore nor a gem: it can come up on an ordinary unit roll, but it never
/// fills an extra ore roll and its weight is left out of the gem share, so adding it changed neither.
/// </summary>
public static class ProspectingGems
{
    /// <summary>The resrefs of the gem items harvesting tables hold, one for each tier.</summary>
    public static readonly IReadOnlyList<string> Resrefs = new[] { "jade", "agate", "citrine", "ruby", "emerald" };

    public static bool IsGem(string resref) => Resrefs.Contains(resref);

    /// <summary>The table's gem entries.</summary>
    public static IReadOnlyList<LootTableItem> GemEntries(LootTable table) =>
        table == null ? Array.Empty<LootTableItem>() : table.Where(item => IsGem(item.Resref)).ToList();

    /// <summary>The table's entries that are neither gems nor woodlot saplings: what an extra ore roll draws from.</summary>
    public static IReadOnlyList<LootTableItem> OreEntries(LootTable table) =>
        table == null ? Array.Empty<LootTableItem>() : table.Where(item => !IsGem(item.Resref) && !IsSapling(item.Resref)).ToList();

    /// <summary>True for the woodlot sapling items a tree's table can drop.</summary>
    public static bool IsSapling(string resref) => WoodlotRules.IsSaplingResref(resref);

    /// <summary>The table's woodlot sapling entries, none for any table but a tree's.</summary>
    public static IReadOnlyList<LootTableItem> SaplingEntries(LootTable table) =>
        table == null ? Array.Empty<LootTableItem>() : table.Where(item => IsSapling(item.Resref)).ToList();

    /// <summary>True when the table has a gem entry, so its nodes can hold gem pockets.</summary>
    public static bool HasGems(LootTable table) => GemEntries(table).Count > 0;

    /// <summary>
    /// The share of a roll of the table that is a gem: the weight of its gem entries over its whole weight, a sapling's weight
    /// not counted. 0 for a table with none.
    /// </summary>
    public static double GemChance(LootTable table)
    {
        if (table == null)
            return 0;

        var total = table.Where(item => !IsSapling(item.Resref)).Sum(item => (double)item.Weight);
        return total <= 0 ? 0 : GemEntries(table).Sum(item => (double)item.Weight) / total;
    }
}
