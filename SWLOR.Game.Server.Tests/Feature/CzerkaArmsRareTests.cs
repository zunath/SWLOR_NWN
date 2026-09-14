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

public class CzerkaArmsRareTests
{
    private static readonly (string Resref, string RareLootTable)[] RareElites =
    {
        ("overwatch", "CZERKA_OVERWATCH_RARES"),
        ("blastbreaker", "CZERKA_BLASTBREAKER_RARES"),
        ("suppressor", "CZERKA_SUPPRESSOR_RARES"),
    };
    private const string RareSpawnTable = "CZERKA_ARMS_TEST_RANGE_RARES";

    private static readonly (string Blueprint, RecipeType Recipe, string CraftedResref, string Component)[] RareRecipes =
    {
        ("bp_precoptic", RecipeType.SalvagedPrecisionOptic, "precision_optic", "targeting_mod"),
        ("bp_detonknuck", RecipeType.SalvagedDetoniteKnuckle, "detonite_knuck", "detonite_chg"),
        ("bp_jammermesh", RecipeType.SalvagedJammerMesh, "jammer_mesh", "signal_disr"),
    };

    [Test]
    public void CzerkaRareElites_UseWeightedRareEntriesInDedicatedTable()
    {
        var tables = new NarShaddaaSpawnDefinition().BuildSpawnTables();
        tables.Should().ContainKey(RareSpawnTable);
        var spawns = tables[RareSpawnTable].Spawns;
        spawns.Select(s => s.Resref).Should().BeEquivalentTo(RareElites.Select(r => r.Resref));
        foreach (var spawn in spawns)
        {
            spawn.Type.Should().Be(ObjectType.Creature);
            spawn.Weight.Should().Be(1);
            spawn.IsRare.Should().BeTrue();
        }
    }

    [Test]
    public void CzerkaRareEliteLoot_IsUniqueRareGear()
    {
        var tables = new NarShaddaaLootTableDefinition().BuildLootTables();
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
    public void CzerkaRareSpawnTable_IsReferencedInTestRangeOnly()
    {
        var root = FindRepositoryRoot();
        var files = Directory.GetFiles(Path.Combine(root.FullName, "Module", "git"), "*.git.json")
            .Where(file => File.ReadAllText(file).Contains($"\"value\": \"{RareSpawnTable}\""))
            .Select(Path.GetFileName)
            .ToArray();
        files.Should().BeEquivalentTo(new[] { "pw_ar_czarmrange.git.json" });
    }

    [Test]
    public void CzerkaRareRecipes_AreRegisteredWithSalvageComponents()
    {
        var recipes = new SalvagedFieldGearRecipes().BuildRecipes();
        foreach (var (_, recipe, crafted, component) in RareRecipes)
        {
            recipes.Should().ContainKey(recipe);
            var detail = recipes[recipe];
            detail.Skill.Should().Be(SkillType.Smithery);
            detail.Resref.Should().Be(crafted);
            detail.Level.Should().Be(50);
            detail.Components.Should().ContainKey(component);
        }
    }

    [Test]
    public void CzerkaRareRecipeBlueprints_LearnRegisteredRecipes()
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

    private static DirectoryInfo FindRepositoryRoot()
    {
        var directory = new DirectoryInfo(TestContext.CurrentContext.TestDirectory);
        while (directory != null && !File.Exists(Path.Combine(directory.FullName, "SWLOR.Game.Server.sln")))
            directory = directory.Parent;
        return directory ?? throw new DirectoryNotFoundException("Could not locate repository root.");
    }
}
