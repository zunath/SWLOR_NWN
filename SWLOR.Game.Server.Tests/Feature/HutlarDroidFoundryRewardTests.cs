using System.Text.Json;
using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Feature.LootTableDefinition;
using SWLOR.Game.Server.Feature.RecipeDefinition.SmitheryRecipeDefinition;
using SWLOR.Game.Server.Feature.SpawnDefinition;
using SWLOR.Game.Server.Service.CraftService;
using SWLOR.Game.Server.Service.SkillService;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Tests.Feature;

public class HutlarDroidFoundryRewardTests
{
    private const int ItemPropertyRequiresSkill = 131;

    // Floor spawn table -> named rare -> loot key -> unique gear -> blueprint -> recipe -> crafted output -> salvage component.
    private static readonly (string SpawnTable, string Rare, string LootKey, string[] Gear, string Blueprint,
        RecipeType Recipe, string Crafted, string Component)[] NamedRares =
    {
        ("HUTLAR_FOUNDRY_INTAKE", "fdy_sorter", "SORTER", new[] { "grapple_bracer", "conveyor_treads" },
            "bp_sortharness", RecipeType.SalvagedSortingHarness, "sorting_harness", "sorting_actuator"),
        ("HUTLAR_FOUNDRY_ASSEMBLY", "fdy_foreman", "FOREMAN", new[] { "line_visor", "riveted_mantle" },
            "bp_assemgaunt", RecipeType.SalvagedAssemblyGauntlet, "assembly_gaunt", "torque_regulator"),
        ("HUTLAR_FOUNDRY_CORE", "fdy_inspector", "INSPECTOR", new[] { "quality_signet", "lattice_plate" },
            "bp_nullcollar", RecipeType.SalvagedNullFieldCollar, "nullfield_collar", "faultscan_optic"),
    };

    private static readonly (string Resref, string Difficulty, bool RollsGear)[] FloorDroids =
    {
        ("fdy_hauler", "Normal", false), ("fdy_sentry", "Normal", false), ("fdy_welder", "Normal", false), ("fdy_smelter", "Tough", false),
        ("fdy_linedef", "Normal", true), ("fdy_riveter", "Normal", true), ("fdy_reclaim", "Normal", true), ("fdy_forgewd", "Tough", true),
        ("fdy_wardroid", "Normal", true), ("fdy_siege", "Normal", true), ("fdy_hunter", "Normal", true), ("fdy_juggernt", "Tough", true),
    };

    private static string Root
    {
        get
        {
            var directory = new DirectoryInfo(TestContext.CurrentContext.TestDirectory);
            while (directory != null && !Directory.Exists(Path.Combine(directory.FullName, "Module")))
                directory = directory.Parent;
            return directory?.FullName ?? throw new DirectoryNotFoundException("Module directory not found.");
        }
    }

    private static JsonElement Load(string type, string name)
    {
        using var document = JsonDocument.Parse(File.ReadAllText(Path.Combine(Root, "Module", type, $"{name}.{type}.json")));
        return document.RootElement.Clone();
    }

    private static JsonElement Value(JsonElement o, string key) => o.GetProperty(key).GetProperty("value");
    private static string Text(JsonElement o, string key) => Value(o, key).GetString()!;

    private static string[] LootLocals(JsonElement creature) => Value(creature, "VarTable").EnumerateArray()
        .Where(v => Text(v, "Name").StartsWith("LOOT_TABLE_"))
        .OrderBy(v => Text(v, "Name"))
        .Select(v => Value(v, "Value").GetString()!)
        .ToArray();

    [Test]
    public void NamedRares_SpawnAsWeightedRareEntriesInTheirFloorTable()
    {
        var tables = new HutlarSpawnDefinition().BuildSpawnTables();
        foreach (var rare in NamedRares)
        {
            var rareSpawns = tables[rare.SpawnTable].Spawns.Where(s => s.IsRare).ToList();
            rareSpawns.Should().ContainSingle().Which.Resref.Should().Be(rare.Rare);
            rareSpawns[0].Type.Should().Be(ObjectType.Creature);
            rareSpawns[0].Weight.Should().Be(1, "rare elites stay on the normal weighted frequency model");
        }
    }

    [Test]
    public void NamedRares_DropUniqueGearABlueprintAndAGuaranteedSalvageComponent()
    {
        var loot = new HutlarLootTableDefinition().BuildLootTables();
        foreach (var rare in NamedRares)
        {
            var rares = loot[$"HUTLAR_FOUNDRY_{rare.LootKey}_RARES"];
            rares.IsRare.Should().BeTrue();
            rares.Select(i => i.Resref).Should().BeEquivalentTo(rare.Gear.Append(rare.Blueprint));
            rares.Should().OnlyContain(i => i.IsRare && i.MaxQuantity == 1 && i.Weight == 1);
            loot[$"HUTLAR_FOUNDRY_{rare.LootKey}_COMP"].Select(i => i.Resref).Should().Equal(rare.Component);

            LootLocals(Load("utc", rare.Rare)).Should().Equal(
                $"HUTLAR_FOUNDRY_{rare.LootKey}_RARES,100,1",
                $"HUTLAR_FOUNDRY_{rare.LootKey}_RARES,10,1",
                $"HUTLAR_FOUNDRY_{rare.LootKey}_COMP,100,1",
                "MUSIC_DISCS,1,1");

            foreach (var gear in rare.Gear.Append(rare.Crafted))
            {
                var requirement = Value(Load("uti", gear), "PropertiesList").EnumerateArray()
                    .Single(p => Value(p, "PropertyName").GetInt32() == ItemPropertyRequiresSkill);
                Value(requirement, "Subtype").GetInt32().Should().Be((int)SkillType.Armor, gear);
                Value(requirement, "CostValue").GetInt32().Should().Be(45, gear);
            }
        }
    }

    [Test]
    public void NamedRareBlueprints_UnlockSalvageRecipesThatNeedTheirComponent()
    {
        var recipes = new SalvagedFieldGearRecipes().BuildRecipes();
        foreach (var rare in NamedRares)
        {
            var blueprint = Load("uti", rare.Blueprint);
            Text(blueprint, "Tag").Should().Be("RECIPE");
            Value(blueprint, "VarTable").EnumerateArray().Single(v => Text(v, "Name") == "RECIPES")
                .GetProperty("Value").GetProperty("value").GetString().Should().Be(((int)rare.Recipe).ToString());

            var recipe = recipes[rare.Recipe];
            recipe.Skill.Should().Be(SkillType.Smithery);
            recipe.Resref.Should().Be(rare.Crafted);
            recipe.Components.Should().ContainKey(rare.Component);
            File.Exists(Path.Combine(Root, "Module", "uti", $"{rare.Crafted}.uti.json")).Should().BeTrue();
        }

        NamedRares.Select(r => recipes[r.Recipe].Level).Should().OnlyHaveUniqueItems("recipe levels spread across the foundry's reward band");
    }

    [Test]
    public void FloorDroids_RollTierFiveGearOnTheAssemblyLineAndCore()
    {
        var loot = new HutlarLootTableDefinition().BuildLootTables();
        loot["HUTLAR_FOUNDRY_INTAKE"].Select(i => i.Resref).Should().Contain("elec_high", "level 41-45 enemies drop mostly tier 5 materials");
        loot["HUTLAR_FOUNDRY_GEAR"].Should().NotBeEmpty();
        loot["HUTLAR_FOUNDRY_GEAR"].Select(i => i.Resref)
            .Should().OnlyContain(resref => File.Exists(Path.Combine(Root, "Module", "uti", $"{resref}.uti.json")));

        foreach (var (resref, difficulty, rollsGear) in FloorDroids)
        {
            var gearRolls = LootLocals(Load("utc", resref)).Where(l => l.StartsWith("HUTLAR_FOUNDRY_GEAR,")).ToArray();
            if (rollsGear)
                gearRolls.Should().Equal(difficulty == "Tough" ? "HUTLAR_FOUNDRY_GEAR,20,1" : "HUTLAR_FOUNDRY_GEAR,10,1");
            else
                gearRolls.Should().BeEmpty($"{resref} is on the Intake floor");
        }
    }

    [Test]
    public void Overseer_IsAPremiumBossWithThePremiumGearRecipePool()
    {
        var loot = new HutlarLootTableDefinition().BuildLootTables();
        var recipes = loot["HUTLAR_FOUNDRY_OVERSEER_RECIPES"];
        var premiumGearRecipes = loot["QIONHIVE_BROODMOTHER_RECIPE"]
            .Select(i => i.Resref)
            .Where(resref => !resref.EndsWith("sushi"));
        recipes.Select(i => i.Resref).Should().BeEquivalentTo(premiumGearRecipes);
        foreach (var item in recipes)
            Text(Load("uti", item.Resref), "Tag").Should().Be("RECIPE", item.Resref);

        var overseerTable = new HutlarSpawnDefinition().BuildSpawnTables()["HUTLAR_FOUNDRY_OVERSEER"];
        overseerTable.RespawnDelayMinutes.Should().Be(61, "the Overseer uses the premium world-boss window, rolled per respawn");
        overseerTable.RespawnDelayMaximumMinutes.Should().Be(160);

        LootLocals(Load("utc", "fdy_overseer")).Should().Contain(new[]
        {
            "HUTLAR_FOUNDRY_OVERSEER_RECIPES,100,1",
            "HUTLAR_FOUNDRY_GEAR,100,2",
        });
    }
}
