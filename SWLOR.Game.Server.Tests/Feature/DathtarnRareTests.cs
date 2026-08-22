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

public class DathtarnRareTests
{
    private static readonly (string Resref, string RareLootTable)[] RareElites =
    {
        ("tarnapexmaw", "DATHTARN_APEX_RARES"),
        ("quillstalker", "DATHTARN_QUILL_RARES"),
        ("rhydelalpha", "DATHTARN_RHYDEL_RARES"),
    };
    private const string RareSpawnTable = "DATHOMIR_TARN_JUNGLE_PRESERVE_RARES";
    private static readonly (string Blueprint, RecipeType Recipe, string CraftedResref, string Component)[] RareRecipes =
    {
        ("bp_tarnapexmaw", RecipeType.SalvagedApexGauntlets, "tarnapexmawcr", "tarnapexmawcm"),
        ("bp_quillstalker", RecipeType.SalvagedQuillVisor, "quillstalkercr", "quillstalkercm"),
        ("bp_rhydelalpha", RecipeType.SalvagedRhydelCuirass, "rhydelalphacr", "rhydelalphacm"),
    };

    [Test]
    public void DathtarnRareTests_UseWeightedRareEntriesInDedicatedTable()
    {
        var spawns = new DathomirSpawnDefinition().BuildSpawnTables()[RareSpawnTable].Spawns;
        spawns.Select(s => s.Resref).Should().BeEquivalentTo(RareElites.Select(r => r.Resref));
        spawns.Should().OnlyContain(s => s.Type == ObjectType.Creature && s.Weight == 1 && s.IsRare);
    }

    [Test]
    public void DathtarnRareTests_LootIsUniqueRareGear()
    {
        var tables = new DathomirLootTableDefinition().BuildLootTables();
        foreach (var (_, id) in RareElites)
        {
            tables.Should().ContainKey(id);
            tables[id].IsRare.Should().BeTrue();
            tables[id].Should().OnlyContain(item => item.IsRare && item.MaxQuantity == 1 && item.Weight == 1);
        }
    }

    [Test]
    public void DathtarnRareTests_RecipesAreRegisteredWithSalvageComponents()
    {
        var recipes = new SalvagedFieldGearRecipes().BuildRecipes();
        foreach (var (_, recipe, crafted, component) in RareRecipes)
        {
            recipes.Should().ContainKey(recipe);
            recipes[recipe].Skill.Should().Be(SkillType.Smithery);
            recipes[recipe].Resref.Should().Be(crafted);
            recipes[recipe].Level.Should().Be(50);
            recipes[recipe].Components.Should().ContainKey(component);
        }
    }

    [Test]
    public void DathtarnRareTests_BlueprintsLearnRegisteredRecipes()
    {
        var root = FindRepositoryRoot();
        foreach (var (blueprint, recipe, _, _) in RareRecipes)
        {
            using var uti = JsonDocument.Parse(File.ReadAllText(Path.Combine(root.FullName, "Module", "uti", $"{blueprint}.uti.json")));
            uti.RootElement.GetProperty("Tag").GetProperty("value").GetString().Should().Be("RECIPE");
            GetLocalString(uti.RootElement, "RECIPES").Should().Be(((int)recipe).ToString());
            uti.RootElement.GetProperty("LocalizedName").GetProperty("value").GetProperty("0").GetString().Should().StartWith("Blueprint: ");
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
        var d = new DirectoryInfo(TestContext.CurrentContext.TestDirectory);
        while (d != null && !File.Exists(Path.Combine(d.FullName, "SWLOR.Game.Server.sln"))) d = d.Parent;
        return d ?? throw new DirectoryNotFoundException("Could not locate repository root.");
    }
}
