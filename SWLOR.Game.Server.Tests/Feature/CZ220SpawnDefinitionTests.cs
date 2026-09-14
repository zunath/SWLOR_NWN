using System.IO;
using System.Linq;
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

public class CZ220SpawnDefinitionTests
{
    // Blueprint drop -> recipe -> crafted output -> encounter salvage component.
    private static readonly (string Blueprint, RecipeType Recipe, string CraftedResref, string Component)[] RareRecipes =
    {
        ("bp_reactorpl", RecipeType.SalvagedReactorPlate, "reactor_plate", "reactor_core"),
        ("bp_pistongaunt", RecipeType.SalvagedPistonGauntlet, "piston_gaunt", "crusher_piston"),
        ("bp_siegeoptic", RecipeType.SalvagedSiegeOptics, "siege_optics", "targeting_lens"),
    };

    [Test]
    public void CZ220RareRecipes_AreRegisteredWithSalvageComponents()
    {
        var recipes = new SalvagedFieldGearRecipes().BuildRecipes();
        foreach (var (_, recipe, crafted, component) in RareRecipes)
        {
            recipes.Should().ContainKey(recipe);
            var detail = recipes[recipe];
            detail.Skill.Should().Be(SkillType.Smithery);
            detail.Resref.Should().Be(crafted);
            detail.Level.Should().Be(50);
            detail.Quantity.Should().Be(1);
            detail.Components.Should().ContainKey(component);
        }
    }

    [Test]
    public void CZ220RareRecipeBlueprints_LearnRegisteredRecipes()
    {
        var root = FindRepositoryRoot();
        foreach (var (blueprint, recipe, _, _) in RareRecipes)
        {
            using var uti = JsonDocument.Parse(File.ReadAllText(
                Path.Combine(root.FullName, "Module", "uti", $"{blueprint}.uti.json")));
            var json = uti.RootElement;
            json.GetProperty("Tag").GetProperty("value").GetString().Should().Be("RECIPE");
            GetLocalString(json, "RECIPES").Should().Be(((int)recipe).ToString());
            json.GetProperty("LocalizedName").GetProperty("value").GetProperty("0").GetString()
                .Should().StartWith("Blueprint: ");
        }
    }

    private static string GetLocalString(JsonElement json, string name)
    {
        foreach (var v in json.GetProperty("VarTable").GetProperty("value").EnumerateArray())
            if (v.GetProperty("Name").GetProperty("value").GetString() == name)
                return v.GetProperty("Value").GetProperty("value").GetString() ?? string.Empty;
        return string.Empty;
    }

    private static readonly (string Resref, string RareLootTable)[] RareElites =
    {
        ("bulwark", "CZ220_BULWARK_RARES"),
        ("slagborn", "CZ220_SLAGBORN_RARES"),
        ("demolisherzr9", "CZ220_DEMOLISHER_RARES"),
    };

    private const string RareSpawnTable = "CZ220_BREAKER_YARD_RARES";

    [Test]
    public void CZ220RareElites_UseWeightedRareEntriesInDedicatedTable()
    {
        var tables = new CZ220SpawnDefinition().BuildSpawnTables();
        tables.Should().ContainKey(RareSpawnTable);

        var spawns = tables[RareSpawnTable].Spawns;
        spawns.Select(s => s.Resref).Should().BeEquivalentTo(RareElites.Select(r => r.Resref));
        foreach (var spawn in spawns)
        {
            spawn.Type.Should().Be(ObjectType.Creature);
            spawn.Weight.Should().Be(1, "rare elites stay on the normal weighted frequency model");
            spawn.IsRare.Should().BeTrue();
        }
    }

    [Test]
    public void CZ220RareEliteLoot_IsUniqueRareGear()
    {
        var tables = new CZ220LootTableDefinition().BuildLootTables();
        foreach (var (_, tableId) in RareElites)
        {
            tables.Should().ContainKey(tableId);
            var table = tables[tableId];
            table.IsRare.Should().BeTrue();
            table.Should().NotBeEmpty();
            table.Should().OnlyContain(item => item.IsRare && item.MaxQuantity == 1 && item.Weight == 1);
        }
    }

    [Test]
    public void CZ220RareSpawnTable_IsReferencedInBreakerBayOnly()
    {
        var root = FindRepositoryRoot();
        var files = Directory.GetFiles(Path.Combine(root.FullName, "Module", "git"), "*.git.json")
            .Where(file => File.ReadAllText(file).Contains($"\"value\": \"{RareSpawnTable}\""))
            .Select(Path.GetFileName)
            .ToArray();
        files.Should().BeEquivalentTo(new[] { "cz220shipbreakin.git.json" });
    }

    private static DirectoryInfo FindRepositoryRoot()
    {
        var directory = new DirectoryInfo(TestContext.CurrentContext.TestDirectory);
        while (directory != null && !File.Exists(Path.Combine(directory.FullName, "SWLOR.Game.Server.sln")))
            directory = directory.Parent;
        return directory ?? throw new DirectoryNotFoundException("Could not locate repository root.");
    }
}
