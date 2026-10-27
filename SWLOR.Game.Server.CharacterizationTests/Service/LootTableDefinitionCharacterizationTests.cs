using System.Reflection;
using NUnit.Framework;
using SWLOR.Game.Server.CharacterizationTests.Definitions;
using SWLOR.Game.Server.CharacterizationTests.Support;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.LootService;

namespace SWLOR.Game.Server.CharacterizationTests.Service;

/// <summary>
/// Locks the built loot table definitions: every table's flags and totals, and every item of every table (item, weight,
/// largest quantity, rare flag, whether a spawn action is set). The items are split over several snapshot files by the first
/// letter of the table id so that each file stays reviewable. A spawn action is code; the snapshot only records that one is set.
/// </summary>
public class LootTableDefinitionCharacterizationTests
{
    private static readonly string[] TableProperties =
    {
        nameof(LootTable.IsRare),
        nameof(LootTable.MaxRareChanceBonus),
    };

    private static readonly string[] ItemProperties =
    {
        nameof(LootTableItem.IsRare),
        nameof(LootTableItem.MaxQuantity),
        nameof(LootTableItem.OnSpawn),
        nameof(LootTableItem.Resref),
        nameof(LootTableItem.Weight),
    };

    private static readonly string[] ItemGroupNames = LootTableDefinitionSource.ItemGroupNames;

    [Test]
    public void PublicConstants()
    {
        var table = new SnapshotTable(
            "LootTable and Loot (public const fields)",
            "The rare find bonus a full Treasure Hunter perk grants, and the variable name the corpse placeable uses.",
            "All public const fields of LootTable and Loot, sorted by class then name.",
            new[] { "class", "constant" },
            new[] { "type", "value" })
        {
            AllowConstantOutputs = true
        };

        foreach (var type in new[] { typeof(Loot), typeof(LootTable) }.OrderBy(type => type.Name, StringComparer.Ordinal))
        {
            var constants = type
                .GetFields(BindingFlags.Public | BindingFlags.Static)
                .Where(field => field.IsLiteral && !field.IsInitOnly)
                .OrderBy(field => field.Name, StringComparer.Ordinal);
            foreach (var field in constants)
                table.Add(type.Name, field.Name, field.FieldType.Name, field.GetRawConstantValue());
        }

        Snapshot.Verify("Loot", "PublicConstants", table);
    }

    [Test]
    public void LootTables()
    {
        var tables = LootTableDefinitionSource.Load();

        var table = new SnapshotTable(
            "Loot tables (every ILootTableDefinition table)",
            "One row per loot table: whether it is a rare table, the scaled rare chance bonus, and the totals of its items. " +
            "The items themselves are in the LootTableItems files.",
            $"{tables.Count} tables sorted by id (ordinal). totalWeight is the sum of the item weights; rareWeight the sum of the weights of the rare items.",
            new[] { "table" },
            new[] { "isRare", "maxRareChanceBonus", "itemCount", "totalWeight", "rareItemCount", "rareWeight", "goldItemCount", "spawnActionCount", "largestMaxQuantity" })
        {
            AllowConstantOutputs = true
        };

        foreach (var entry in tables)
        {
            var items = entry.Table;
            table.Add(
                entry.Id,
                entry.Table.IsRare,
                entry.Table.MaxRareChanceBonus,
                items.Count,
                items.Sum(item => item.Weight),
                items.Count(item => item.IsRare),
                items.Where(item => item.IsRare).Sum(item => item.Weight),
                items.Count(item => item.Resref == "nw_it_gold001"),
                items.Count(item => item.OnSpawn != null),
                items.Count == 0 ? 0 : items.Max(item => item.MaxQuantity));
        }

        Snapshot.Verify("Loot", "LootTables", table);
    }

    [TestCaseSource(nameof(ItemGroupNames))]
    public void LootTableItems(string groupName)
    {
        var tables = LootTableDefinitionSource.Load().Where(entry => LootTableDefinitionSource.ItemGroupOf(entry.Id) == groupName).ToArray();

        var table = new SnapshotTable(
            $"Loot table items: tables {groupName} (every item of every ILootTableDefinition table whose id starts with these letters)",
            "Every item of every table in the group in the order the table lists it: the item, its weight, the largest quantity a drop can have, " +
            "whether the Treasure Hunter perk raises its weight, and whether a spawn action is set. The chance of an item is its weight divided by the total weight " +
            "of the table (rare items get the creature's rare find bonus added to their weight first).",
            $"{tables.Length} tables, {tables.Sum(entry => entry.Table.Count)} items. Tables are sorted by id (ordinal); index is the item's position in the table. " +
            "nw_it_gold001 is gold, where the quantity is an amount of credits.",
            new[] { "table", "index" },
            new[] { "resref", "weight", "maxQuantity", "isRare", "hasSpawnAction" })
        {
            AllowConstantOutputs = true
        };

        foreach (var entry in tables)
        {
            for (var index = 0; index < entry.Table.Count; index++)
            {
                var item = entry.Table[index];
                table.Add(entry.Id, index, item.Resref, item.Weight, item.MaxQuantity, item.IsRare, item.OnSpawn != null);
            }
        }

        Snapshot.Verify("Loot", "LootTableItems." + groupName, table);
    }

    [Test]
    public void EveryTable_BelongsToAnItemGroup()
    {
        // The item snapshots are split by the first letter of the table id; every id must land in one of the named files.
        var groups = LootTableDefinitionSource.Load().Select(entry => LootTableDefinitionSource.ItemGroupOf(entry.Id)).Distinct();
        Assert.That(groups, Is.SubsetOf(ItemGroupNames));
        Assert.That(LootTableDefinitionSource.Load().Count, Is.GreaterThan(0));
    }

    [Test]
    public void LootTableDefinitionSource_FindsEveryDefinition()
    {
        // Every definition class must build at least one table, otherwise it would be a silent gap in the snapshots.
        var built = LootTableDefinitionSource.Load().Select(entry => entry.Definition).Distinct().Count();
        Assert.That(built, Is.EqualTo(LootTableDefinitionSource.DefinitionTypes().Count));
    }

    [Test]
    public void EveryLootTableProperty_IsLockedOrDeliberatelyLeftOut()
    {
        Assert.That(PropertyNames(typeof(LootTable)), Is.EquivalentTo(TableProperties),
            "LootTable gained or lost a property. Lock new plain data in LootTables and add it to TableProperties.");
        Assert.That(PropertyNames(typeof(LootTableItem)), Is.EquivalentTo(ItemProperties),
            "LootTableItem gained or lost a property. Lock new plain data in LootTableItems and add it to ItemProperties " +
            "(or list new code as a has-flag like hasSpawnAction).");
    }

    private static string[] PropertyNames(Type type)
    {
        return type
            .GetProperties(BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly)
            .Select(property => property.Name)
            .OrderBy(name => name, StringComparer.Ordinal)
            .ToArray();
    }
}
