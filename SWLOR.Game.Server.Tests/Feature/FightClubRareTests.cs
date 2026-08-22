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

public class FightClubRareTests
{
    private static readonly (string Resref, string RareLootTable)[] RareElites =
    {
        ("ironjaw", "FIGHTCLUB_IRONJAW_RARES"),
        ("quickdraw", "FIGHTCLUB_QUICKDRAW_RARES"),
        ("hexcaller", "FIGHTCLUB_HEXCALLER_RARES"),
    };
    private const string RareSpawnTable = "FIGHTCLUB_BACKROOMS_RARES";
    private static readonly (string Blueprint, RecipeType Recipe, string CraftedResref, string Component)[] RareRecipes =
    {
        ("bp_pitcestus", RecipeType.SalvagedPitCestus, "pit_cestus", "arena_token"),
        ("bp_duelvest", RecipeType.SalvagedDuelistVest, "duel_vest", "spent_charge"),
        ("bp_charmcowl", RecipeType.SalvagedCharmCowl, "charm_cowl", "hex_focus"),
    };

    [Test]
    public void FightClubRareElites_UseWeightedRareEntriesInDedicatedTable()
    {
        var spawns = new NarShaddaaSpawnDefinition().BuildSpawnTables()[RareSpawnTable].Spawns;
        spawns.Select(s => s.Resref).Should().BeEquivalentTo(RareElites.Select(r => r.Resref));
        spawns.Should().OnlyContain(s => s.Type == ObjectType.Creature && s.Weight == 1 && s.IsRare);
    }

    [Test]
    public void FightClubRareEliteLoot_IsUniqueRareGear()
    {
        var tables = new NarShaddaaLootTableDefinition().BuildLootTables();
        foreach (var (_, id) in RareElites)
        {
            tables.Should().ContainKey(id);
            tables[id].IsRare.Should().BeTrue();
            tables[id].Should().OnlyContain(item => item.IsRare && item.MaxQuantity == 1 && item.Weight == 1);
        }
    }

    [Test]
    public void FightClubRareSpawnTable_IsReferencedInBackroomsOnly()
    {
        var root = FindRepositoryRoot();
        Directory.GetFiles(Path.Combine(root.FullName, "Module", "git"), "*.git.json")
            .Where(file => File.ReadAllText(file).Contains($"\"value\": \"{RareSpawnTable}\""))
            .Select(Path.GetFileName).Should().BeEquivalentTo(new[] { "pw_sc_emfbackr.git.json" });
    }

    [Test]
    public void FightClubRareRecipes_AreRegisteredWithSalvageComponents()
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
    public void FightClubRareRecipeBlueprints_LearnRegisteredRecipes()
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
