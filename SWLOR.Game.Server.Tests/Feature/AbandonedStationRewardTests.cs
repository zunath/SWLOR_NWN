using System.Reflection;
using System.Text.Json;
using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Feature.LootTableDefinition;
using SWLOR.Game.Server.Feature.RecipeDefinition.SmitheryRecipeDefinition;
using SWLOR.Game.Server.Feature.SpawnDefinition;
using SWLOR.Game.Server.Service.CraftService;
using SWLOR.Game.Server.Service.KeyItemService;
using SWLOR.Game.Server.Service.SkillService;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Tests.Feature;

public class AbandonedStationRewardTests
{
    private const int ItemPropertyRequiresSkill = 131;

    // Floor spawn table -> named rare -> loot key -> unique gear -> blueprint -> recipe -> crafted output -> component.
    private static readonly (string SpawnTable, string Rare, string LootKey, string[] Gear, string Blueprint,
        RecipeType Recipe, string Crafted, string Component)[] NamedRares =
    {
        ("ABANDONED_STATION_MAIN", "abs_zchief", "CHIEF", new[] { "riotvisor_helm", "lockdown_bracer" },
            "bp_breachbelt", RecipeType.SalvagedBreachSealBelt, "breachseal_belt", "override_spike"),
        ("ABANDONED_STATION_RESTRICTED", "abs_subzero", "SUBZERO", new[] { "quarantine_mask", "isolation_shroud" },
            "bp_serumneck", RecipeType.SalvagedSerumCellNecklace, "serumcell_neck", "plague_culture"),
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
    public void StationMap_DropsFromEnemiesOnEveryFloorAndUnlocksAllFloors()
    {
        var mapType = KeyItemType.AbandonedStationMap;
        var mapResref = $"map_{(int)mapType}";
        var map = Load("uti", mapResref);
        var metadata = typeof(KeyItemType).GetField(nameof(KeyItemType.AbandonedStationMap))!
            .GetCustomAttribute<KeyItemAttribute>()!;
        metadata.Category.Should().Be(KeyItemCategoryType.Maps);
        metadata.IsActive.Should().BeTrue();
        Text(map, "Tag").Should().Be("KEY_ITEM");
        Value(map, "LocalizedName").GetProperty("0").GetString().Should().Be(metadata.Name);
        Value(map, "DescIdentified").GetProperty("0").GetString().Should().Be(metadata.Description);
        Value(map, "VarTable").EnumerateArray().Single(v => Text(v, "Name") == "KEY_ITEM_ID")
            .GetProperty("Value").GetProperty("value").GetInt32().Should().Be((int)mapType);

        var rares = new AbandonedStationLootTableDefinition().BuildLootTables()["STATION_RARES"];
        var drop = rares.Should().ContainSingle(item => item.Resref == mapResref).Subject;
        rares.IsRare.Should().BeTrue();
        drop.Weight.Should().BeGreaterThan(0);
        drop.MaxQuantity.Should().Be(1);
        drop.IsRare.Should().BeTrue();

        var spawns = new AbandonedStationSpawnDefinition().BuildSpawnTables();
        foreach (var (area, table) in new[]
        {
            ("zomb_abanstation", "ABANDONED_STATION_MAIN"),
            ("zomb_abanstatio2", "ABANDONED_STATION_RESTRICTED"),
            ("zomb_abanstatio3", "ABANDONED_STATION_CHAMBERS")
        })
        {
            Value(Load("git", area), "VarTable").EnumerateArray().Single(v => Text(v, "Name") == "MAP_KEY_ITEM_ID")
                .GetProperty("Value").GetProperty("value").GetInt32().Should().Be((int)mapType);
            spawns[table].Spawns.Should().Contain(spawn => LootLocals(Load("utc", spawn.Resref))
                .Any(loot => loot.StartsWith("STATION_RARES,") &&
                             int.Parse(loot.Split(',')[1]) > 0 && int.Parse(loot.Split(',')[2]) > 0),
                $"enemies on {area} must be able to drop the map");
        }
    }

    [Test]
    public void NamedRares_SpawnAsWeightedRareEntriesInTheirFloorTable()
    {
        var tables = new AbandonedStationSpawnDefinition().BuildSpawnTables();
        foreach (var rare in NamedRares)
        {
            var rareSpawns = tables[rare.SpawnTable].Spawns.Where(s => s.IsRare).ToList();
            rareSpawns.Should().ContainSingle().Which.Resref.Should().Be(rare.Rare);
            rareSpawns[0].Type.Should().Be(ObjectType.Creature);
            rareSpawns[0].Weight.Should().Be(1, "rare elites stay on the normal weighted frequency model");
        }
    }

    [Test]
    public void NamedRares_DropUniqueGearABlueprintAndAGuaranteedComponent()
    {
        var loot = new AbandonedStationLootTableDefinition().BuildLootTables();
        foreach (var rare in NamedRares)
        {
            var rares = loot[$"STATION_{rare.LootKey}_RARES"];
            rares.IsRare.Should().BeTrue();
            rares.Select(i => i.Resref).Should().BeEquivalentTo(rare.Gear.Append(rare.Blueprint));
            rares.Should().OnlyContain(i => i.IsRare && i.MaxQuantity == 1 && i.Weight == 1);
            loot[$"STATION_{rare.LootKey}_COMP"].Select(i => i.Resref).Should().Equal(rare.Component);

            LootLocals(Load("utc", rare.Rare)).Should().Equal(
                $"STATION_{rare.LootKey}_RARES,100,1",
                $"STATION_{rare.LootKey}_RARES,10,1",
                $"STATION_{rare.LootKey}_COMP,100,1",
                "MUSIC_DISCS,1,1");

            foreach (var gear in rare.Gear.Append(rare.Crafted))
            {
                var requirement = Value(Load("uti", gear), "PropertiesList").EnumerateArray()
                    .Single(p => Value(p, "PropertyName").GetInt32() == ItemPropertyRequiresSkill);
                Value(requirement, "Subtype").GetInt32().Should().Be((int)SkillType.Armor, gear);
                Value(requirement, "CostValue").GetInt32().Should().Be(25, $"{gear} is tier-3 named-rare gear");
            }
        }
    }

    [Test]
    public void NamedRareBlueprints_UnlockRecipesThatNeedTheirComponent()
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

        NamedRares.Select(r => recipes[r.Recipe].Level).Should().OnlyHaveUniqueItems("recipe levels spread across the station's reward band");
    }
}
