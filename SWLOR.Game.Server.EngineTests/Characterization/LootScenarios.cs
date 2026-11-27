using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using SWLOR.Game.Server.EngineTests.Characterization.Support;
using SWLOR.Game.Server.EngineTests.Framework;

namespace SWLOR.Game.Server.EngineTests.Characterization;

/// <summary>
/// Output locking for loot: spawns loot from real loot tables the way a dying creature does (the creature's
/// <c>LOOT_TABLE_n</c> variables, then <c>Loot.SpawnLoot</c>), with the shared Random reseeded before every draw, and
/// records the items that arrive: blueprint, stack size and item properties, plus the gold.
/// </summary>
public static class LootScenarios
{
    private const int Seed = 20261028;
    private const int Draws = 8;
    private const string CreditFinderVariable = "CREDITFINDER_LEVEL";
    private const string RareBonusVariable = "RARE_BONUS_CHANCE";

    /// <param name="Label">Names the setup in the snapshot.</param>
    /// <param name="Tables">The creature's loot table entries: <c>table,chance,attempts</c> in order.</param>
    private sealed record Setup(string Label, string[] Tables, int CreditFinderLevel, int RareBonusChance);

    private static readonly Setup[] Setups =
    {
        new("Mandalorian warrior drops", new[] { "VISCARA_MANDALORIAN_WARRIOR,100,2" }, 0, 0),
        new("Mandalorian warrior drops with Credit Finder 3", new[] { "VISCARA_MANDALORIAN_WARRIOR,100,2" }, 3, 0),
        new("Mandalorian warrior drops and rares", new[]
        {
            "VISCARA_MANDALORIAN_WARRIOR,100,2", "VISCARA_MANDALORIAN_WARRIOR_TAGS,50,1",
            "VISCARA_MANDALORIAN_WARRIOR_RARES,20,1"
        }, 0, 0),
        new("Mandalorian warrior rares with rare bonus 30", new[]
        {
            "VISCARA_MANDALORIAN_WARRIOR,100,2", "VISCARA_MANDALORIAN_WARRIOR_RARES,20,1"
        }, 0, 30),
        new("Mandalorian leader drops", new[] { "VISCARA_MANDALORIAN_LEADER,60,3" }, 0, 0),
        new("Sand demon drops and rare ruby", new[]
        {
            "TATOOINE_SAND_DEMON,100,3", "TATOOINE_SAND_DEMON_RARES,10,1"
        }, 0, 0),
        new("Sand demon drops and rare ruby with rare bonus 20", new[]
        {
            "TATOOINE_SAND_DEMON,100,3", "TATOOINE_SAND_DEMON_RARES,10,1"
        }, 0, 20),
        new("Kath hound drops and rares", new[]
        {
            "VISCARA_KATH_HOUND,100,2", "VISCARA_KATH_HOUND_RARES,15,1"
        }, 0, 0)
    };

    [EngineTest("Characterization: loot spawned from loot tables", Category = "Characterization", TimeoutSeconds = 300f)]
    public static async Task TableDraws(EngineTestContext ctx)
    {
        using var clock = new FrozenClock();
        using var arena = await ScenarioArena.CreateAsync(ctx);
        var scenario = arena.Context;

        var table = new SnapshotTable(
            "Loot.SpawnLoot",
            "Spawns the loot a creature with the listed loot table entries would drop, once per draw with the shared " +
            "Random reseeded for that draw, and records the items and gold that arrived.",
            $"Source and receiver are one {ScenarioActors.Humanoid}; its LOOT_TABLE_n variables hold the listed " +
            $"'table,chance,attempts' entries. Draw n reseeds Random with {Seed} + n for n = 0..{Draws - 1}. " +
            "Items are written as 'blueprint xStackSize [Type/Subtype/Cost/Param, ...]'.",
            new[] { "setup", "creditFinder", "rareBonus", "draw" },
            new[] { "items", "gold" },
            Seed);

        foreach (var setup in Setups)
        {
            foreach (var entry in setup.Tables)
                scenario.Assert(Loot.LootTableExists(entry.Split(',')[0]), $"Loot table {entry.Split(',')[0]} is registered");

            var creature = await ScenarioActors.SpawnQuietAsync(scenario, ScenarioActors.Humanoid, 0f, 0f);
            await scenario.ExecuteInCreatureContextAsync(creature, () =>
            {
                for (var index = 0; index < setup.Tables.Length; index++)
                    SetLocalString(creature, "LOOT_TABLE_" + (index + 1), setup.Tables[index]);
                SetLocalInt(creature, CreditFinderVariable, setup.CreditFinderLevel);
                SetLocalInt(creature, RareBonusVariable, setup.RareBonusChance);
                Clear(creature);
            });
            await scenario.WaitFrameAsync();
            scenario.AssertEqual(0, GetGold(creature), "The receiver starts without gold");
            scenario.AssertEqual(0, ItemDescription.DescribeInventory(creature).Count, "The receiver starts without items");

            for (var draw = 0; draw < Draws; draw++)
            {
                await scenario.ExecuteInCreatureContextAsync(creature, () =>
                {
                    scenario.SeedRandom(Seed + draw);
                    Loot.SpawnLoot(creature, creature, "LOOT_TABLE_");
                });
                await scenario.WaitFrameAsync();
                await scenario.WaitFrameAsync();

                table.Add(setup.Label, setup.CreditFinderLevel, setup.RareBonusChance, draw,
                    ItemDescription.DescribeInventory(creature), GetGold(creature));

                await scenario.ExecuteInCreatureContextAsync(creature, () => Clear(creature));
                await scenario.WaitFrameAsync();
            }

            Retire(creature);
        }

        ScenarioSnapshot.Verify("Loot", "TableDraws", table);
    }

    private static void Clear(uint creature)
    {
        for (var item = GetFirstItemInInventory(creature); GetIsObjectValid(item); item = GetNextItemInInventory(creature))
            DestroyObject(item);

        var gold = GetGold(creature);
        if (gold > 0)
            TakeGoldFromCreature(gold, creature, true);
    }

    private static void Retire(uint creature)
    {
        AssignCommand(creature, () =>
        {
            SetIsDestroyable(true, false, false);
            DestroyObject(creature);
        });
    }
}
