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
using SWLOR.NWN.API.NWScript.Enum.Item;

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

    [Test]
    public void CrewGear_DropsFromTheCrewThatWoreIt()
    {
        var loot = new AbandonedStationLootTableDefinition().BuildLootTables();
        loot["STATION_SECURITY_GEAR"].Select(i => i.Resref).Should().BeEquivalentTo(ContainmentArmor.Concat(ContainmentWeapons.Select(w => w.Gear)));
        loot["STATION_CREW_GEAR"].Select(i => i.Resref).Should().BeEquivalentTo(GantryArmor);
        loot["STATION_RESEARCH_GEAR"].Select(i => i.Resref).Should().BeEquivalentTo(BiolabArmor);
        loot["STATION_GEAR"].Select(i => i.Resref).Should().BeEquivalentTo(
            loot["STATION_SECURITY_GEAR"].Concat(loot["STATION_CREW_GEAR"]).Concat(loot["STATION_RESEARCH_GEAR"]).Select(i => i.Resref),
            "rakghouls, test subjects and the rancor carry whatever their victims wore");
        foreach (var table in new[] { "STATION_SECURITY_GEAR", "STATION_CREW_GEAR", "STATION_RESEARCH_GEAR", "STATION_GEAR" })
            loot[table].Should().OnlyContain(i => i.Weight == 1 && i.MaxQuantity == 1, table);

        foreach (var (creature, table, chance) in GearDrops)
            LootLocals(Load("utc", creature)).Should().Contain($"{table},{chance},1", creature);
    }

    [Test]
    public void Bosses_DropLevelThirtyUniques()
    {
        var loot = new AbandonedStationLootTableDefinition().BuildLootTables();
        foreach (var (creature, table, uniques) in BossUniques)
        {
            loot[table].IsRare.Should().BeTrue();
            loot[table].Select(i => i.Resref).Should().BeEquivalentTo(uniques);
            loot[table].Should().OnlyContain(i => i.IsRare && i.MaxQuantity == 1 && i.Weight == 1);
            LootLocals(Load("utc", creature)).Should().Contain($"{table},40,1", creature);
        }
    }

    [Test]
    public void StationArmor_MatchesItsCraftedReferenceBudget()
    {
        var failures = new List<string>();
        foreach (var (gear, reference, level, minRatio, maxRatio, maxOffense) in ArmorBudgets)
        {
            var properties = Properties(gear);
            RequiredSkill(properties).Should().Be(((int)SkillType.Armor, level), gear);
            RequiredSkill(Properties(reference)).Level.Should().Be(level, $"{reference} is the level-{level} reference for {gear}");

            var ratio = Units(properties) / Units(Properties(reference));
            if (ratio < minRatio - 0.005 || ratio > maxRatio + 0.005)
                failures.Add($"{gear}: {ratio:0.000} of {reference}'s budget, expected {minRatio:0.00}-{maxRatio:0.00}");
            foreach (var type in new[] { ItemPropertyType.Attack, ItemPropertyType.ForceAttack, ItemPropertyType.Evasion })
                if (Amount(properties, type) > maxOffense)
                    failures.Add($"{gear}: {type} {Amount(properties, type)} exceeds {maxOffense}");
        }

        failures.Should().BeEmpty(string.Join(Environment.NewLine, failures));
    }

    [Test]
    public void StationWeapons_SitBetweenLevelTwentyFiveCraftedAndLevelThirtyLoot()
    {
        foreach (var (gear, crafted, levelThirtyLoot, level, attack) in ContainmentWeapons.Append(RancorCleaver))
        {
            var properties = Properties(gear);
            var weapon = Load("uti", gear);
            Value(weapon, "BaseItem").GetInt32().Should().Be(Value(Load("uti", levelThirtyLoot), "BaseItem").GetInt32(), gear);
            var skill = RequiredSkill(properties);
            skill.Should().Be((RequiredSkill(Properties(levelThirtyLoot)).Skill, level), gear);

            Amount(properties, ItemPropertyType.DMG).Should().BeInRange(
                Amount(Properties(crafted), ItemPropertyType.DMG), Amount(Properties(levelThirtyLoot), ItemPropertyType.DMG), gear);
            Amount(properties, ItemPropertyType.Attack).Should().Be(attack, gear);
            Amount(properties, ItemPropertyType.Delay).Should().Be(Amount(Properties(levelThirtyLoot), ItemPropertyType.Delay), gear);
        }
    }

    [Test]
    public void StationMap_DropsAsARareAndRevealsEveryStationArea()
    {
        var map = Load("uti", $"map_{(int)KeyItemType.ViscaraAbandonedStationMap}");
        Text(map, "Tag").Should().Be("KEY_ITEM");
        Value(map, "VarTable").EnumerateArray().Single(v => Text(v, "Name") == "KEY_ITEM_ID")
            .GetProperty("Value").GetProperty("value").GetInt32().Should().Be((int)KeyItemType.ViscaraAbandonedStationMap);
        Value(map, "DescIdentified").GetProperty("0").GetString().Should().NotBeNullOrWhiteSpace();

        new AbandonedStationLootTableDefinition().BuildLootTables()["STATION_RARES"]
            .Should().Contain(i => i.Resref == $"map_{(int)KeyItemType.ViscaraAbandonedStationMap}" && i.IsRare);

        foreach (var area in new[] { "zomb_abanstation", "zomb_abanstatio2", "zomb_abanstatio3" })
        {
            var locals = Value(Load("git", area), "VarTable").EnumerateArray().ToDictionary(v => Text(v, "Name"), v => Value(v, "Value").GetInt32());
            locals["MINI_MAP_DISABLED"].Should().Be(1, area);
            locals["MAP_KEY_ITEM_ID"].Should().Be((int)KeyItemType.ViscaraAbandonedStationMap, $"{area} hides its minimap until the player finds the map");
        }
    }

    private static readonly string[] ContainmentArmor = { "cnt_helmet", "cnt_armor", "cnt_bracer", "cnt_leggings", "cnt_shield" };
    private static readonly string[] GantryArmor = { "gantry_cap", "gantry_tunic", "gantry_gloves", "gantry_boots" };
    private static readonly string[] BiolabArmor = { "biolab_cap", "biolab_tunic", "biolab_gloves", "biolab_boots" };

    // Station weapon -> level-25 crafted weapon and level-30 loot weapon of the same base item, required level, Attack.
    private static readonly (string Gear, string Crafted, string LevelThirtyLoot, int Level, int Attack)[] ContainmentWeapons =
    {
        ("cnt_longsword", "prm_longsword", "raider_longsword", 25, 1),
        ("cnt_knife", "prm_knife", "raider_knife", 25, 1),
        ("cnt_gswd", "prm_greatsword", "raider_gswd", 25, 1),
        ("cnt_spear", "prm_spear", "raider_spear", 25, 1),
        ("cnt_twinblade", "prm_twinblade", "raider_twinblade", 25, 1),
        ("cnt_katar", "prm_katar", "raider_katar", 25, 1),
        ("cnt_staff", "prm_staff", "raider_staff", 25, 1),
        ("cnt_pistol", "prm_pistol", "raider_pistol", 25, 1),
        ("cnt_rifle", "prm_rifle", "raider_rifle", 25, 1),
        ("cnt_shuriken", "prm_shuriken", "raider_shuriken", 25, 1),
    };

    private static readonly (string Gear, string Crafted, string LevelThirtyLoot, int Level, int Attack) RancorCleaver =
        ("rancorbone_gswd", "raider_gswd", "raider_gswd", 30, 2);

    // Station creature -> crew gear table and percent chance per kill.
    private static readonly (string Creature, string Table, int Chance)[] GearDrops =
    {
        ("abs_zdockhand", "STATION_CREW_GEAR", 5),
        ("abs_ztech", "STATION_CREW_GEAR", 5),
        ("abs_zresearch", "STATION_RESEARCH_GEAR", 5),
        ("abs_zguard", "STATION_SECURITY_GEAR", 10),
        ("abs_rakghoul", "STATION_GEAR", 5),
        ("abs_zgenetic", "STATION_RESEARCH_GEAR", 10),
        ("abs_zsubject", "STATION_GEAR", 10),
        ("abs_rakstalk", "STATION_GEAR", 10),
        ("abs_rakbrute", "STATION_GEAR", 20),
        ("abs_rakalpha", "STATION_GEAR", 50),
        ("abs_zdirector", "STATION_RESEARCH_GEAR", 100),
        ("abs_zrancor", "STATION_GEAR", 100),
    };

    private static readonly (string Creature, string Table, string[] Uniques)[] BossUniques =
    {
        ("abs_zdirector", "STATION_DIRECTOR_RARES", new[] { "director_coat", "director_signet" }),
        ("abs_zrancor", "STATION_RANCOR_RARES", new[] { "rancorbone_gswd", "rancorhide_cloak" }),
    };

    // Station armor -> crafted piece of the same slot and line, required level, allowed budget ratio, and the
    // cap on each of Attack, Force Attack and Evasion. Floor gear matches level-25 crafted gear and trades part
    // of that budget for one signature stat; boss uniques and named rares sit above crafted gear of their level.
    private static readonly (string Gear, string Reference, int Level, double MinRatio, double MaxRatio, int MaxOffense)[] ArmorBudgets =
    {
        ("cnt_helmet", "prm_bul_helm", 25, 1, 1, 1),
        ("cnt_armor", "prm_bul_armor", 25, 1, 1, 1),
        ("cnt_bracer", "prm_bul_bracer", 25, 1, 1, 1),
        ("cnt_leggings", "prm_bul_legs", 25, 1, 1, 1),
        ("cnt_shield", "prm_bul_shield", 25, 1, 1, 1),
        ("gantry_cap", "prm_skm_cap", 25, 1, 1, 1),
        ("gantry_tunic", "prm_skm_tunic", 25, 1, 1, 1),
        ("gantry_gloves", "prm_skm_gloves", 25, 1, 1, 1),
        ("gantry_boots", "prm_skm_boots", 25, 1, 1, 1),
        ("biolab_cap", "prm_chn_cap", 25, 1, 1, 1),
        ("biolab_tunic", "prm_chn_tunic", 25, 1, 1, 1),
        ("biolab_gloves", "prm_chn_gloves", 25, 1, 1, 1),
        ("biolab_boots", "prm_chn_boots", 25, 1, 1, 1),
        ("director_coat", "gr_tunic", 30, 1.05, 1.25, 2),
        ("director_signet", "gr_ring", 30, 1.05, 1.25, 2),
        ("rancorhide_cloak", "ar_cloak", 30, 1.05, 1.25, 2),
        ("riotvisor_helm", "prm_bul_helm", 25, 1.05, 1.25, 2),
        ("lockdown_bracer", "prm_bul_bracer", 25, 1.05, 1.25, 2),
        ("quarantine_mask", "prm_chn_cap", 25, 1.05, 1.25, 2),
        ("isolation_shroud", "prm_chn_cloak", 25, 1.05, 1.25, 2),
    };

    // Budget units: Defense is the unit; Attack, Force Attack and Evasion cost double, matching LootStatRevisionTests.
    private static readonly Dictionary<(int Type, int Subtype), double> UnitWeights = new()
    {
        [((int)ItemPropertyType.Defense, 1)] = 1, [((int)ItemPropertyType.Defense, 2)] = 1,
        [((int)ItemPropertyType.HPBonus, 0)] = 1 / 8.0, [((int)ItemPropertyType.FP, 0)] = 5 / 6.0,
        [((int)ItemPropertyType.Stamina, 0)] = 5 / 6.0, [((int)ItemPropertyType.CombatReadiness, 0)] = 1,
        [((int)ItemPropertyType.Attack, 0)] = 2, [((int)ItemPropertyType.ForceAttack, 0)] = 2,
        [((int)ItemPropertyType.Evasion, 0)] = 2, [((int)ItemPropertyType.FPRegen, 0)] = 2.5,
        [((int)ItemPropertyType.STMRegen, 0)] = 2.5,
    };

    private static Dictionary<(int Type, int Subtype), int> Properties(string resref) =>
        Value(Load("uti", resref), "PropertiesList").EnumerateArray().ToDictionary(
            p => (Value(p, "PropertyName").GetInt32(), Value(p, "Subtype").GetInt32()),
            p => Value(p, "CostValue").GetInt32());

    private static int Amount(Dictionary<(int Type, int Subtype), int> properties, ItemPropertyType type) =>
        properties.Where(p => p.Key.Type == (int)type).Sum(p => p.Value);

    private static (int Skill, int Level) RequiredSkill(Dictionary<(int Type, int Subtype), int> properties) =>
        properties.Where(p => p.Key.Type == ItemPropertyRequiresSkill).Select(p => (p.Key.Subtype, p.Value)).Single();

    private static double Units(Dictionary<(int Type, int Subtype), int> properties) =>
        properties.Sum(p => UnitWeights.TryGetValue(p.Key, out var weight) ? weight * p.Value : 0);
}
