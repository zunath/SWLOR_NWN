using System;
using System.IO;
using System.Linq;
using System.Text.Json;
using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Feature.BeastDefinition.TamableBeastDefinition;
using SWLOR.Game.Server.Feature.SpawnDefinition;
using SWLOR.Game.Server.Service.AIService;
using SWLOR.Game.Server.Service.BeastMasteryService;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Tests.Feature;

public class ShatterveinEncounterTests
{
    [Test]
    public void Warden_HasOneFixedSpawnInsideCrystalDen_AndCannotEnterWolfTable()
    {
        var tables = new EshanSpawnDefinition().BuildSpawnTables();
        var wolves = tables["ESHAN_SHATTERVEIN_WOLVES"].Spawns;
        wolves.Should().ContainSingle().Which.Resref.Should().Be("esh_veinwolf");
        var boss = tables["ESHAN_SHATTERVEIN_WARDEN"].Spawns.Should().ContainSingle().Which;
        boss.Resref.Should().Be("esh_veinboss");
        boss.AIFlags.HasFlag(AIFlag.ReturnHome).Should().BeTrue();
        boss.AIFlags.HasFlag(AIFlag.RandomWalk).Should().BeFalse();

        using var git = Read("git", "pw_sc_esvein");
        var points = Value(git.RootElement, "WaypointList").EnumerateArray().ToArray();
        var den = points.Single(point => Value(point, "Tag").GetString() == "ESHAN_SHATTERVEIN_WARDEN");
        // The marked den lies inside the southeastern crystal walls, away from the western pools.
        Value(den, "XPosition").GetDouble().Should().BeInRange(127, 133);
        Value(den, "YPosition").GetDouble().Should().BeInRange(56, 61);
        points.Count(point => Value(point, "Tag").GetString() == "ESHAN_SHATTERVEIN_WOLVES").Should().Be(7);
        Value(git.RootElement, "VarTable").EnumerateArray().Should().NotContain(variable =>
            Value(variable, "Name").GetString() == "CREATURE_SPAWN_TABLE_ID");
        using var gic = Read("gic", "pw_sc_esvein");
        Value(gic.RootElement, "WaypointList").GetArrayLength().Should().Be(points.Length);
    }

    [Test]
    public void Wolf_TamesIntoFenrisForceBeast_WithCompleteProgression()
    {
        using var wolf = Read("utc", "esh_veinwolf");
        var creature = wolf.RootElement;
        Value(creature, "Appearance_Type").GetInt32().Should().Be((int)AppearanceType.DogWolfFenrisCCCPLUSHHYENAofDOOM);
        var beastType = Value(creature, "VarTable").EnumerateArray().Single(variable => Value(variable, "Name").GetString() == "BEAST_TYPE");
        Value(beastType, "Value").GetInt32().Should().Be((int)BeastType.ShatterveinWolf);
        var beast = new ShatterveinWolfBeastDefinition().Build()[BeastType.ShatterveinWolf];
        beast.Role.Should().Be(BeastRoleType.Force);
        beast.Appearance.Should().Be(AppearanceType.DogWolfFenrisCCCPLUSHHYENAofDOOM);
        beast.Levels.Keys.Should().BeEquivalentTo(Enumerable.Range(1, 50));
        beast.PossibleMutations.Should().BeEmpty();
    }

    [TestCase("esh_veinwolf", 6356, new[] { 1861, 1862 })]
    [TestCase("esh_veinboss", 6473, new[] { 1861, 1862, 1863, 1864, 1765, 1844 })]
    public void Encounters_UseLevel45ForcePackages_WithoutUnrelatedQuestCredit(string resref, int appearance, int[] abilities)
    {
        using var utc = Read("utc", resref);
        var creature = utc.RootElement;
        Value(creature, "Appearance_Type").GetInt32().Should().Be(appearance);
        Value(creature, "FeatList").EnumerateArray().Select(feat => Value(feat, "Feat").GetInt32())
            .Should().BeEquivalentTo(abilities.Append(289));
        var variables = Value(creature, "VarTable").EnumerateArray().ToArray();
        variables.Should().NotContain(variable => Value(variable, "Name").GetString() == "QUEST_NPC_GROUP_ID");
        if (resref == "esh_veinboss")
            variables.Should().NotContain(variable => Value(variable, "Name").GetString() == "BEAST_TYPE");
        using var skin = Read("uti", resref + "_sk");
        var level = Value(skin.RootElement, "PropertiesList").EnumerateArray().Single(prop => Value(prop, "PropertyName").GetInt32() == 99);
        Value(level, "CostValue").GetInt32().Should().Be(45);
    }

    private static JsonElement Value(JsonElement element, string field) => element.GetProperty(field).GetProperty("value");

    private static JsonDocument Read(string kind, string resref)
    {
        var root = new DirectoryInfo(TestContext.CurrentContext.TestDirectory);
        while (root != null && !Directory.Exists(Path.Combine(root.FullName, "Module"))) root = root.Parent;
        if (root == null) throw new DirectoryNotFoundException("Repository root not found.");
        return JsonDocument.Parse(File.ReadAllText(Path.Combine(root.FullName, "Module", kind, resref + "." + kind + ".json")));
    }
}
