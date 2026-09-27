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

public class TuskenCavesTests
{
    private const string EliteSpawnTable = "TATOOINE_TUSKEN_ELITE";
    private const string WarlordSpawnTable = "TATOOINE_TUSKEN_WARLORD";
    private const string WarlordAreaFile = "tat_tuskcavebot.git.json";
    private const int ItemPropertyRequiresSkill = 131;
    private const int ArmorSkillSubtype = 6;
    private const int StaffSkillSubtype = 44;

    private static readonly string[] CaveAreaFiles =
    {
        "tat_tuskcavetunn.git.json",
        "tat_tuskcavemain.git.json",
        "tat_tuskcavebot.git.json",
    };

    private static readonly (string Resref, string RareLootTable, string ComponentLootTable, string Component, string[] UniqueDrops)[] RareElites =
    {
        ("tuskchampion", "TATOOINE_TUSKEN_CHAMPION_RARES", "TATOOINE_TUSKEN_CHAMPION_COMP", "sun_bantha_hide",
            new[] { "gaffi_bracer", "hide_warbelt", "bp_hidecuirass" }),
        ("tusklongeye", "TATOOINE_TUSKEN_LONGEYE_RARES", "TATOOINE_TUSKEN_LONGEYE_COMP", "cycler_scope",
            new[] { "dunescout_boots", "dunescout_glove", "bp_scopecap" }),
        ("tuskwarcaller", "TATOOINE_TUSKEN_WARCALLER_RARES", "TATOOINE_TUSKEN_WARCALLER_COMP", "bone_fetish",
            new[] { "ritual_beads", "sandcall_wraps", "bp_windmantle" }),
    };

    private static readonly (string Blueprint, RecipeType Recipe, string Crafted, string Component, int Level)[] Recipes =
    {
        ("bp_hidecuirass", RecipeType.SunCuredHideCuirass, "hide_cuirass", "sun_bantha_hide", 42),
        ("bp_scopecap", RecipeType.ScopewrightCap, "scopewright_cap", "cycler_scope", 43),
        ("bp_windmantle", RecipeType.WindcallerMantle, "windcall_mantle", "bone_fetish", 44),
        ("bp_warbandhelm", RecipeType.WarbandHelm, "warband_helm", "bone_totem", 45),
    };

    private static readonly (string Resref, int Skill, int RequiredLevel)[] UniqueGear =
    {
        ("gaffi_bracer", ArmorSkillSubtype, 40),
        ("hide_warbelt", ArmorSkillSubtype, 40),
        ("dunescout_boots", ArmorSkillSubtype, 40),
        ("dunescout_glove", ArmorSkillSubtype, 40),
        ("ritual_beads", ArmorSkillSubtype, 40),
        ("sandcall_wraps", ArmorSkillSubtype, 40),
        ("warband_legs", ArmorSkillSubtype, 42),
        ("sandstorm_gaffi", StaffSkillSubtype, 42),
        ("hide_cuirass", ArmorSkillSubtype, 40),
        ("scopewright_cap", ArmorSkillSubtype, 40),
        ("windcall_mantle", ArmorSkillSubtype, 40),
        ("warband_helm", ArmorSkillSubtype, 42),
    };

    [Test]
    public void TuskenCaveRareElites_UseWeightedRareEntriesInEliteSpawnTable()
    {
        var spawns = new TatooineSpawnDefinition().BuildSpawnTables()[EliteSpawnTable].Spawns;

        spawns.Where(s => !s.IsRare).Select(s => (s.Resref, s.Weight))
            .Should()
            .BeEquivalentTo(new[] { ("tusken_elite1", 50), ("tusken_elite2", 50) }, "the normal Tusken Elite weights stay unchanged");

        var rares = spawns.Where(s => s.IsRare).ToArray();
        rares.Select(s => s.Resref).Should().BeEquivalentTo(RareElites.Select(r => r.Resref));
        rares.Should().OnlyContain(s => s.Type == ObjectType.Creature && s.Weight == 1);
    }

    [Test]
    public void TuskenWarlord_UsesDedicatedSpawnTableWithWorldBossRespawn()
    {
        var tables = new TatooineSpawnDefinition().BuildSpawnTables();
        var table = tables[WarlordSpawnTable];

        table.Spawns.Should().ContainSingle();
        var spawn = table.Spawns.Single();
        spawn.Resref.Should().Be("tuskwarlord");
        spawn.Type.Should().Be(ObjectType.Creature);
        spawn.IsRare.Should().BeFalse();
        table.RespawnDelayMinutes.Should().BeGreaterThanOrEqualTo(60);
        table.RespawnDelayMaximumMinutes.Should().BeGreaterThanOrEqualTo(table.RespawnDelayMinutes);

        tables.Where(t => t.Value.Spawns.Any(s => s.Resref == "tuskwarlord"))
            .Select(t => t.Key)
            .Should()
            .BeEquivalentTo(new[] { WarlordSpawnTable }, "the Warlord should only spawn from its dedicated table");
    }

    [Test]
    public void TuskenWarlord_WaypointCopiesAnEliteWaypointInTheDeepestCave()
    {
        var root = FindRepositoryRoot();
        foreach (var file in CaveAreaFiles.Where(f => f != WarlordAreaFile))
        {
            GetWaypoints(root, file).Should().NotContain(w => w.Tag == WarlordSpawnTable, $"{file} should not host the Warlord");
        }

        var waypoints = GetWaypoints(root, WarlordAreaFile);
        var warlord = waypoints.Where(w => w.Tag == WarlordSpawnTable).Should().ContainSingle().Subject;
        waypoints
            .Where(w => w.Tag == EliteSpawnTable)
            .Should()
            .Contain(w => w.X == warlord.X && w.Y == warlord.Y && w.Z == warlord.Z,
                "the Warlord waypoint must reuse the exact position of an existing Tusken Elite waypoint");

        var allGitFiles = Directory.GetFiles(Path.Combine(root.FullName, "Module", "git"), "*.git.json");
        allGitFiles
            .Where(path => File.ReadAllText(path).Contains($"\"value\": \"{WarlordSpawnTable}\"", StringComparison.Ordinal))
            .Select(Path.GetFileName)
            .Should()
            .BeEquivalentTo(new[] { WarlordAreaFile });
    }

    [Test]
    public void TuskenCaveRareElites_DropOneGuaranteedUniqueWithLowChanceSecondRoll()
    {
        var root = FindRepositoryRoot();
        var tables = new TatooineLootTableDefinition().BuildLootTables();

        foreach (var spec in RareElites)
        {
            var rareTable = tables[spec.RareLootTable];
            rareTable.IsRare.Should().BeTrue();
            rareTable.Select(i => i.Resref).Should().BeEquivalentTo(spec.UniqueDrops);
            rareTable.Should().OnlyContain(i => i.IsRare && i.Weight == 1 && i.MaxQuantity == 1);

            var componentTable = tables[spec.ComponentLootTable];
            componentTable.IsRare.Should().BeFalse();
            componentTable.Should().ContainSingle(i => i.Resref == spec.Component && i.MaxQuantity == 1);

            var locals = GetLootLocals(root, spec.Resref);
            locals.Should().Contain($"{spec.RareLootTable},100,1");
            locals.Should().Contain($"{spec.RareLootTable},10,1");
            locals.Should().Contain($"{spec.ComponentLootTable},100,1");
            locals.Where(l => l.StartsWith(spec.RareLootTable + ",", StringComparison.Ordinal))
                .Should()
                .HaveCount(2, "one guaranteed unique roll plus one low-chance bonus roll");
        }
    }

    [Test]
    public void TuskenWarlord_DropsBossMaterialsLockboxAndRareRewards()
    {
        var root = FindRepositoryRoot();
        var tables = new TatooineLootTableDefinition().BuildLootTables();

        tables["TATOOINE_TUSKEN_WARLORD_ELECTRONICS"].Should().ContainSingle(i => i.Resref == "elec_high" && i.MaxQuantity >= 2);
        tables["TATOOINE_TUSKEN_WARLORD_FIBERPLAST"].Should().ContainSingle(i => i.Resref == "fiberp_high");
        tables["TATOOINE_TUSKEN_WARLORD_LOCKBOX"].Should().ContainSingle(i => i.Resref == "lockbox_t5");
        tables["TATOOINE_TUSKEN_WARLORD_COMP"].Should().ContainSingle(i => i.Resref == "bone_totem");

        var rares = tables["TATOOINE_TUSKEN_WARLORD_RARES"];
        rares.IsRare.Should().BeTrue();
        rares.Should().OnlyContain(i => i.IsRare && i.Weight == 1 && i.MaxQuantity == 1);
        rares.Select(i => i.Resref).Should().Contain(new[] { "sandstorm_gaffi", "warband_legs" });
        rares.Select(i => i.Resref).Should().Contain(Recipes.Select(r => r.Blueprint), "the Warlord can drop any cave blueprint");

        var locals = GetLootLocals(root, "tuskwarlord");
        locals.Should().Contain("TATOOINE_TUSKEN_WARLORD_ELECTRONICS,100,3", "a guaranteed stack of several High Quality Electronics");
        locals.Should().Contain("TATOOINE_TUSKEN_WARLORD_FIBERPLAST,100,1");
        locals.Should().Contain("TATOOINE_TUSKEN_WARLORD_LOCKBOX,100,1");
        locals.Should().Contain("TATOOINE_TUSKEN_WARLORD_COMP,100,1");
        locals.Should().Contain(l => l.StartsWith("TATOOINE_TUSKEN_WARLORD_RARES,", StringComparison.Ordinal));
    }

    [Test]
    public void TuskenEliteLoot_KeepsElectronicsAndImprovesRareLockboxOdds()
    {
        var tables = new TatooineLootTableDefinition().BuildLootTables();

        tables[EliteSpawnTable].Single(i => i.Resref == "elec_high").Weight.Should().BeGreaterThanOrEqualTo(12);

        var rares = tables["TATOOINE_TUSKEN_ELITE_RARES"];
        var lockbox = rares.Single(i => i.Resref == "lockbox_t4");
        ((double)lockbox.Weight / rares.Sum(i => i.Weight)).Should().BeGreaterThan(0.1, "clearing the caves as a group should turn up Tier 4 lockboxes");
    }

    [Test]
    public void TuskenWarGearRecipes_AreUnlockedByDroppedBlueprintsAndUseCaveComponents()
    {
        var root = FindRepositoryRoot();
        var recipes = new TuskenWarGearRecipes().BuildRecipes();
        var tables = new TatooineLootTableDefinition().BuildLootTables();
        var droppedResrefs = tables.Values.SelectMany(t => t).Select(i => i.Resref).ToHashSet();

        recipes.Keys.Should().BeEquivalentTo(Recipes.Select(r => r.Recipe));
        foreach (var spec in Recipes)
        {
            var recipe = recipes[spec.Recipe];
            recipe.Skill.Should().Be(SkillType.Smithery);
            recipe.Resref.Should().Be(spec.Crafted);
            recipe.Level.Should().Be(spec.Level);
            recipe.Components.Should().ContainKey(spec.Component);
            recipe.Requirements.OfType<RecipeUnlockRequirement>().Should().ContainSingle();
            File.Exists(Path.Combine(root.FullName, "Module", "uti", $"{spec.Crafted}.uti.json")).Should().BeTrue();

            droppedResrefs.Should().Contain(spec.Blueprint);
            droppedResrefs.Should().Contain(spec.Component);

            using var blueprint = ReadJson(root, "uti", spec.Blueprint);
            blueprint.RootElement.GetProperty("Tag").GetProperty("value").GetString().Should().Be("RECIPE");
            blueprint.RootElement.GetProperty("TemplateResRef").GetProperty("value").GetString().Should().Be(spec.Blueprint);
            GetLocalString(blueprint.RootElement, "RECIPES").Should().Be(((int)spec.Recipe).ToString());
        }

        recipes.Values.Select(r => r.Level).Distinct().Should().HaveCount(Recipes.Length, "recipe levels should spread across the cave reward band");
    }

    [Test]
    public void TuskenCaveUniqueGear_UsesRequiredSkillConventions()
    {
        var root = FindRepositoryRoot();
        foreach (var (resref, skill, requiredLevel) in UniqueGear)
        {
            using var uti = ReadJson(root, "uti", resref);
            var requirement = uti.RootElement.GetProperty("PropertiesList").GetProperty("value").EnumerateArray()
                .Where(p => p.GetProperty("PropertyName").GetProperty("value").GetInt32() == ItemPropertyRequiresSkill)
                .Should()
                .ContainSingle($"{resref} should declare one required skill")
                .Subject;
            requirement.GetProperty("Subtype").GetProperty("value").GetInt32().Should().Be(skill, resref);
            requirement.GetProperty("CostValue").GetProperty("value").GetInt32().Should().Be(requiredLevel, resref);
        }
    }

    [Test]
    public void TuskenCaveRewards_AreListedInTheItemPalette()
    {
        var root = FindRepositoryRoot();
        var palette = File.ReadAllText(Path.Combine(root.FullName, "Module", "itp", "itempalcus.itp.json"));
        var rewards = UniqueGear.Select(g => g.Resref)
            .Concat(Recipes.Select(r => r.Blueprint))
            .Concat(Recipes.Select(r => r.Component));

        foreach (var resref in rewards)
        {
            palette.Should().Contain($"\"value\": \"{resref}\"", $"{resref} should be placeable from the custom item palette");
        }
    }

    private static IReadOnlyList<string> GetLootLocals(DirectoryInfo root, string creature)
    {
        using var utc = ReadJson(root, "utc", creature);
        return utc.RootElement.GetProperty("VarTable").GetProperty("value").EnumerateArray()
            .Where(v => v.GetProperty("Name").GetProperty("value").GetString()!.StartsWith("LOOT_TABLE_", StringComparison.Ordinal))
            .Select(v => v.GetProperty("Value").GetProperty("value").GetString() ?? string.Empty)
            .ToArray();
    }

    private static IReadOnlyList<(string Tag, double X, double Y, double Z)> GetWaypoints(DirectoryInfo root, string areaFile)
    {
        using var git = JsonDocument.Parse(File.ReadAllText(Path.Combine(root.FullName, "Module", "git", areaFile)));
        return git.RootElement.GetProperty("WaypointList").GetProperty("value").EnumerateArray()
            .Select(w => (
                w.GetProperty("Tag").GetProperty("value").GetString() ?? string.Empty,
                w.GetProperty("XPosition").GetProperty("value").GetDouble(),
                w.GetProperty("YPosition").GetProperty("value").GetDouble(),
                w.GetProperty("ZPosition").GetProperty("value").GetDouble()))
            .ToArray();
    }

    private static JsonDocument ReadJson(DirectoryInfo root, string folder, string resref)
    {
        return JsonDocument.Parse(File.ReadAllText(Path.Combine(root.FullName, "Module", folder, $"{resref}.{folder}.json")));
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
