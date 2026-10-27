using NUnit.Framework;
using SWLOR.Game.Server.CharacterizationTests.Definitions;
using SWLOR.Game.Server.CharacterizationTests.Support;
using SWLOR.Game.Server.Service.CraftService;

namespace SWLOR.Game.Server.CharacterizationTests.Service;

/// <summary>
/// Locks the random bonuses a researched blueprint can roll: the weighted bonus tables of every enhancement type, crafting
/// flag and tier, and the seeded weighted pick over them. The class that holds them is internal, so it is reached by
/// reflection; it needs no engine.
/// </summary>
[NonParallelizable]
public class CraftBlueprintBonusCharacterizationTests
{
    private const int PickSeed = 5001;
    private const int RecordedPicks = 200;
    private const int HistogramPicks = 20000;

    [Test]
    public void BlueprintBonusTables()
    {
        var source = new BlueprintBonusSource();
        var tables = source.Tables();

        var table = new SnapshotTable(
            "BlueprintBonuses tables (internal class, read by reflection)",
            "Every bonus a blueprint can roll, with its weight, the enhancement it grants, the amount and the damage type, in table order. " +
            "The chance of a bonus is its weight divided by the total weight of its table.",
            $"{tables.Count} tables (enhancement type {{Weapon, Armor, Food}} x crafting item {{false, true}} x tier {{1, 2, 3}}), {tables.Sum(entry => entry.Bonuses.Count)} bonuses. " +
            "damageType is Invalid for bonuses that are not damage.",
            new[] { "enhancementType", "isCraftingItem", "tier", "index" },
            new[] { "weight", "subType", "amount", "damageType" })
        {
            AllowConstantOutputs = true
        };

        foreach (var entry in tables)
        {
            for (var index = 0; index < entry.Bonuses.Count; index++)
            {
                var bonus = entry.Bonuses[index];
                table.Add(entry.EnhancementType, entry.IsCraftingItem, entry.Tier, index, bonus.Weight, bonus.Type, bonus.Amount, bonus.DamageType);
            }
        }

        Snapshot.Verify("Craft", "BlueprintBonusTables", table);
    }

    [Test]
    public void PickBonus_Availability()
    {
        var source = new BlueprintBonusSource();
        var tables = source.Tables().ToDictionary(entry => (entry.EnhancementType, entry.IsCraftingItem, entry.Tier));

        var table = new SnapshotTable(
            "BlueprintBonuses.PickBonus(RecipeEnhancementType, int tier, bool isCraftingItem): which combinations have a table",
            "A combination without a table returns no bonus. Weapon, armor and food have tables for tiers 1 to 3, with and without the crafting flag; " +
            "every other enhancement type, and tiers outside 1..3, have none.",
            "Every RecipeEnhancementType member x crafting item {false, true} x tier 0..4.",
            new[] { "enhancementType", "isCraftingItem", "tier" },
            new[] { "hasTable", "bonusCount", "totalWeight" })
        {
            AllowConstantOutputs = true
        };

        foreach (var type in Enum.GetValues<RecipeEnhancementType>().OrderBy(type => DefinitionCell.Id(type)))
        {
            foreach (var isCrafting in new[] { false, true })
            {
                for (var tier = 0; tier <= 4; tier++)
                {
                    var found = tables.TryGetValue((type, isCrafting, tier), out var entry);
                    table.Add(type, isCrafting, tier, found, found ? entry.Bonuses.Count : 0, found ? entry.Bonuses.Sum(bonus => bonus.Weight) : 0);
                }
            }
        }

        Snapshot.Verify("Craft", "PickBonus.Availability", table);
    }

    [Test]
    public void PickBonus_SeededDraws()
    {
        var source = new BlueprintBonusSource();
        var tables = source.Tables();

        var table = new SnapshotTable(
            "BlueprintBonuses.PickBonus(RecipeEnhancementType, int tier, bool isCraftingItem) (seeded)",
            "The bonus index drawn on each call, for every table. The first draws are written out one by one, and the counts per index over " +
            "all draws show the weights at work. An index refers to the position in the table of BlueprintBonusTables.",
            $"The {tables.Count} tables of BlueprintBonusTables. Each row reseeds the generator with {PickSeed}, then draws {HistogramPicks}; the first {RecordedPicks} indexes are listed " +
            "and the counts per index are over all draws.",
            new[] { "enhancementType", "isCraftingItem", "tier" },
            new[] { "bonusCount", "firstDraws", "countsPerIndex" },
            PickSeed);

        using var random = new SeededRandom(PickSeed);
        foreach (var entry in tables)
        {
            random.Reseed(PickSeed);
            var first = new int[RecordedPicks];
            var counts = new int[entry.Bonuses.Count];
            for (var draw = 0; draw < HistogramPicks; draw++)
            {
                var index = source.PickIndex(entry.EnhancementType, entry.Tier, entry.IsCraftingItem)!.Value;
                if (draw < RecordedPicks)
                    first[draw] = index;

                counts[index]++;
            }

            table.Add(entry.EnhancementType, entry.IsCraftingItem, entry.Tier, entry.Bonuses.Count, first, counts);
        }

        Snapshot.Verify("Craft", "PickBonus.SeededDraws", table);
    }
}
