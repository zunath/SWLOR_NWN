using System.Reflection;
using NUnit.Framework;
using SWLOR.Game.Server.CharacterizationTests.Definitions;
using SWLOR.Game.Server.CharacterizationTests.Support;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.LootService;
using GameRandom = SWLOR.Game.Server.Service.Random;

namespace SWLOR.Game.Server.CharacterizationTests.Service;

/// <summary>
/// Locks how loot is drawn: the weighted item pick of <see cref="LootTable"/> with and without a rare find bonus, the
/// quantity roll of <c>Loot.SpawnLoot</c> (a random number from 1 up to the item's largest quantity), the rare chance bonus
/// scaling and the loot table argument parser. Each draw is recorded as the item's position in its table and the quantity
/// rolled. <c>Loot.SpawnLoot</c> itself reads creature variables and creates items, which needs the engine (Tier B).
/// </summary>
[NonParallelizable]
public class LootDrawCharacterizationTests
{
    private const int RepresentativeSeed = 6001;
    private const int RepresentativeDraws = 100;
    private const int AllTablesSeed = 6002;
    private const int AllTablesRecordedDraws = 16;
    private const int AllTablesFingerprintDraws = 2000;
    private const int LevelSeed = 6003;
    private const int LevelDraws = 40;

    private static readonly string[] ItemGroupNames = LootTableDefinitionSource.ItemGroupNames;

    // One table for each behaviour worth reading draw by draw: a single item, only rare items, a mix of rare and common items
    // with gold, equal weights, a very large table with a scaled rare bonus, spawn actions, big gold amounts, and an empty table.
    private static readonly string[] RepresentativeTables =
    {
        "ABANDONED_STATION_ZOMBIE_RANCOR",
        "ANCHRANGE_CANYON_COMP",
        "ANCHRANGE_CANYON_RARES",
        "CZ220_CREDITS",
        "ESPIONAGE_LOCKBOX_1",
        "ESPIONAGE_LOCKBOX_5",
        "FROG_BOSS_RARES",
        "HYDROLASE_TYPE_1",
        "KORRIBAN_FORTRESS_GEAR",
        "KORRIBAN_SITH_APPRENTICE_RARES",
        "KORRIBAN_SITH_CRATE_1",
        "MUSIC_DISCS",
        "MUSIC_DISCS_BOSS",
        "QIONHIVE_BROODMOTHER_RECIPE",
    };

    private static readonly int[] RareBonuses = { -10, 0, 10, 20, 30, 100 };

    [Test]
    public void GetRandomItemWithRareBonus_Representative()
    {
        var tables = LootTableDefinitionSource.Load().ToDictionary(entry => entry.Id);

        var table = new SnapshotTable(
            "LootTable.GetRandomItemWithRareBonus(int rareBonusChance) followed by the quantity roll Random.Next(item.MaxQuantity) + 1 (seeded)",
            "The item drawn and the quantity rolled, in the order Loot.SpawnLoot does them: the weighted pick (a rare item's weight rises by the rare find bonus, " +
            "a bonus of zero or less changes nothing), then a quantity from 1 to the item's largest quantity. An empty table throws.",
            $"{RepresentativeTables.Length} tables x rare bonus {{{string.Join(",", RareBonuses)}}}. Each row reseeds the generator with {RepresentativeSeed} and records {RepresentativeDraws} draws " +
            "as \"index x quantity\", where index is the item's position in the table (see the LootTableItems files). An empty table fails on the first draw and is recorded once.",
            new[] { "table", "rareBonusChance" },
            new[] { "itemCount", "draws" },
            RepresentativeSeed)
        {
            AllowConstantOutputs = true
        };

        using var random = new SeededRandom(RepresentativeSeed);
        foreach (var id in RepresentativeTables)
        {
            var lootTable = tables[id].Table;
            foreach (var bonus in RareBonuses)
            {
                random.Reseed(RepresentativeSeed);
                var draws = new List<string>();
                for (var draw = 0; draw < RepresentativeDraws; draw++)
                {
                    var text = DrawText(lootTable, bonus);
                    draws.Add(text);

                    // A table that cannot be drawn from fails every time; the failure is recorded once.
                    if (text.StartsWith("throws", StringComparison.Ordinal))
                        break;
                }

                table.Add(id, bonus, lootTable.Count, draws);
            }
        }

        Snapshot.Verify("Loot", "GetRandomItemWithRareBonus.Representative", table);
    }

    [Test]
    public void GetRandomItem_ByTreasureHunterLevel()
    {
        var tables = LootTableDefinitionSource.Load().ToDictionary(entry => entry.Id);

        var table = new SnapshotTable(
            "LootTable.GetRandomItem(int treasureHunterLevel) / LootTable.GetRandomItem() (seeded)",
            "The weighted item pick with a rare find bonus of 10 per Treasure Hunter level; GetRandomItem() uses level 0. Each draw is the item's position in the table.",
            $"Tables with rare items {{ESPIONAGE_LOCKBOX_1, FROG_BOSS_RARES, KORRIBAN_SITH_CRATE_1}} x level {{0,1,2,3}} plus the no-argument overload. " +
            $"Each row reseeds the generator with {LevelSeed} and records {LevelDraws} draws.",
            new[] { "table", "treasureHunterLevel" },
            new[] { "draws" },
            LevelSeed);

        using var random = new SeededRandom(LevelSeed);
        foreach (var id in new[] { "ESPIONAGE_LOCKBOX_1", "FROG_BOSS_RARES", "KORRIBAN_SITH_CRATE_1" })
        {
            var lootTable = tables[id].Table;
            foreach (var level in new int?[] { 0, 1, 2, 3, null })
            {
                random.Reseed(LevelSeed);
                var draws = new List<int>();
                for (var draw = 0; draw < LevelDraws; draw++)
                {
                    var item = level == null ? lootTable.GetRandomItem() : lootTable.GetRandomItem(level.Value);
                    draws.Add(lootTable.IndexOf(item));
                }

                table.Add(id, level, draws);
            }
        }

        Snapshot.Verify("Loot", "GetRandomItem", table);
    }

    [TestCaseSource(nameof(ItemGroupNames))]
    public void GetRandomItemWithRareBonus_AllTables(string groupName)
    {
        var tables = LootTableDefinitionSource.Load()
            .Where(entry => entry.Table.Count > 0 && LootTableDefinitionSource.ItemGroupOf(entry.Id) == groupName)
            .ToArray();

        var table = new SnapshotTable(
            $"LootTable.GetRandomItemWithRareBonus + quantity roll over tables {groupName} (seeded)",
            "For every non-empty table whose id starts with these letters: the first draws as the item's position in the table, and a fingerprint of a longer run " +
            "of item and quantity pairs. Tables that hold rare items are drawn at a rare find bonus of 0 and 30, the others at 0.",
            $"{tables.Length} tables. Each row reseeds the generator with {AllTablesSeed}; the first {AllTablesRecordedDraws} item positions are listed, and the fingerprint is the first 12 hex digits " +
            $"of the SHA-256 of {AllTablesFingerprintDraws} draws written as \"index:quantity\" joined by spaces.",
            new[] { "table", "rareBonusChance" },
            new[] { "itemCount", "firstDraws", "fingerprint" },
            AllTablesSeed)
        {
            AllowConstantOutputs = true
        };

        using var random = new SeededRandom(AllTablesSeed);
        foreach (var entry in tables)
        {
            var bonuses = entry.Table.Any(item => item.IsRare) ? new[] { 0, 30 } : new[] { 0 };
            foreach (var bonus in bonuses)
            {
                random.Reseed(AllTablesSeed);
                var first = new List<int>();
                var run = new List<string>();
                for (var draw = 0; draw < AllTablesFingerprintDraws; draw++)
                {
                    var item = entry.Table.GetRandomItemWithRareBonus(bonus);
                    var quantity = GameRandom.Next(item.MaxQuantity) + 1;
                    var index = entry.Table.IndexOf(item);
                    if (draw < AllTablesRecordedDraws)
                        first.Add(index);

                    run.Add($"{index}:{quantity}");
                }

                table.Add(entry.Id, bonus, entry.Table.Count, first, DefinitionCell.Fingerprint(string.Join(' ', run)));
            }
        }

        Snapshot.Verify("Loot", "GetRandomItemWithRareBonus.AllTables." + groupName, table);
    }

    [Test]
    public void QuantityRoll()
    {
        var table = new SnapshotTable(
            "Random.Next(item.MaxQuantity) + 1 (the quantity roll in Loot.SpawnLoot) (seeded)",
            "The quantity of a drop: a random number from 1 to the item's largest quantity. A largest quantity of 1 always gives 1; " +
            "zero gives 1; a negative largest quantity makes the roll throw.",
            $"Largest quantity {{-1,0,1,2,3,5,10,25,100,15000}}. Each row reseeds the generator with {RepresentativeSeed} and records {LevelDraws} rolls.",
            new[] { "maxQuantity" },
            new[] { "rolls" },
            RepresentativeSeed);

        using var random = new SeededRandom(RepresentativeSeed);
        foreach (var maxQuantity in new[] { -1, 0, 1, 2, 3, 5, 10, 25, 100, 15000 })
        {
            random.Reseed(RepresentativeSeed);
            object rolls;
            try
            {
                var values = new List<int>();
                for (var draw = 0; draw < LevelDraws; draw++)
                    values.Add(GameRandom.Next(maxQuantity) + 1);

                rolls = values;
            }
            catch (Exception exception)
            {
                rolls = Outcome.Describe(exception);
            }

            table.Add(maxQuantity, rolls);
        }

        Snapshot.Verify("Loot", "QuantityRoll", table);
    }

    [Test]
    public void GetRareChanceBonus()
    {
        var table = new SnapshotTable(
            "LootTable.GetRareChanceBonus(int rareBonusChance)",
            "Converts a creature's rare find bonus into the percentage points added to a table's drop chance: unscaled when the table has no maximum, " +
            "otherwise scaled so that the full bonus of 30 adds the table's maximum (integer division, so small bonuses can round down to zero).",
            "Table maximum {null,0,10,15,19,30,50} x rare find bonus {-30,-1,0,1,2,10,15,20,29,30,31,60}.",
            new[] { "maxRareChanceBonus", "rareBonusChance" },
            new[] { "bonus" });

        foreach (var maximum in new int?[] { null, 0, 10, 15, 19, 30, 50 })
        {
            foreach (var bonus in new[] { -30, -1, 0, 1, 2, 10, 15, 20, 29, 30, 31, 60 })
                table.Add(maximum, bonus, new LootTable { MaxRareChanceBonus = maximum }.GetRareChanceBonus(bonus));
        }

        Snapshot.Verify("Loot", "GetRareChanceBonus", table);
    }

    [Test]
    public void ParseLootTableArguments()
    {
        var method = typeof(Loot).GetMethod("ParseLootTableArguments", BindingFlags.Static | BindingFlags.NonPublic)!;

        var table = new SnapshotTable(
            "Loot.ParseLootTableArguments(string) (private, read by reflection)",
            "Splits the \"table, chance, attempts\" text a creature carries into its loot table name, the percent chance (default 100, at most 100) and the number of attempts " +
            "(default 1, at least 1). Only well-formed numbers are listed; malformed numbers are logged by the game.",
            "Hand-written argument texts.",
            new[] { "arguments" },
            new[] { "tableName", "chance", "attempts" })
        {
            AllowConstantOutputs = true
        };

        foreach (var arguments in new[]
                 {
                     "CZ220_CREDITS", "  CZ220_CREDITS  ", "CZ220_CREDITS,50", "CZ220_CREDITS, 50", "CZ220_CREDITS,50,3", " CZ220_CREDITS , 25 , 4 ",
                     "CZ220_CREDITS,100", "CZ220_CREDITS,101", "CZ220_CREDITS,1000,2", "CZ220_CREDITS,0", "CZ220_CREDITS,-5,2", "CZ220_CREDITS,50,0",
                     "CZ220_CREDITS,50,-3", "CZ220_CREDITS,50,10,99"
                 })
        {
            var (tableName, chance, attempts) = ((string, int, int))method.Invoke(null, new object[] { arguments })!;
            table.Add(arguments, tableName, chance, attempts);
        }

        Snapshot.Verify("Loot", "ParseLootTableArguments", table);
    }

    private static string DrawText(LootTable lootTable, int rareBonus)
    {
        try
        {
            var item = lootTable.GetRandomItemWithRareBonus(rareBonus);
            var quantity = GameRandom.Next(item.MaxQuantity) + 1;
            return $"{lootTable.IndexOf(item)} x {quantity}";
        }
        catch (Exception exception)
        {
            return Outcome.Describe(exception);
        }
    }
}
