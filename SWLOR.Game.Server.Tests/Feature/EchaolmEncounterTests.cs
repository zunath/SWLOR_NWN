using System;
using System.IO;
using System.Linq;
using System.Text.Json;
using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Feature.BeastDefinition.TamableBeastDefinition;
using SWLOR.Game.Server.Feature.SpawnDefinition;
using SWLOR.Game.Server.Service.BeastMasteryService;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Tests.Feature;

public class EchaolmEncounterTests
{
    [Test]
    public void Shimmerdeep_SpawnsEchaolm_AndRetainsItsResources()
    {
        using var area = Read("git", "pw_sc_es_cavesh");
        var vars = Value(area.RootElement, "VarTable").EnumerateArray().ToDictionary(v => Value(v, "Name").GetString(), v => Value(v, "Value"));
        vars["RESOURCE_SPAWN_TABLE_ID"].GetString().Should().Be("ESHAN_SHIMMERDEEP_RESOURCES");
        vars["CREATURE_SPAWN_COUNT"].GetInt32().Should().Be(6);
        var table = new EshanSpawnDefinition().BuildSpawnTables()[vars["CREATURE_SPAWN_TABLE_ID"].GetString()!];
        table.Spawns.Should().ContainSingle().Which.Resref.Should().Be("esh_echaolm");
    }

    [Test]
    public void Echaolm_TamesAsWhiteWolfBruiser_WithEstablishedProgression()
    {
        using var utc = Read("utc", "esh_echaolm");
        var creature = utc.RootElement;
        Value(creature, "Appearance_Type").GetInt32().Should().Be((int)AppearanceType.DogWinterWolf);
        var tame = Value(creature, "VarTable").EnumerateArray().Single(v => Value(v, "Name").GetString() == "BEAST_TYPE");
        Value(tame, "Value").GetInt32().Should().Be((int)BeastType.Echaolm);
        Value(creature, "VarTable").EnumerateArray().Should().NotContain(v => Value(v, "Name").GetString() == "QUEST_NPC_GROUP_ID");
        Value(creature, "FeatList").EnumerateArray().Select(v => Value(v, "Feat").GetInt32())
            .Should().Contain(new[] { (int)FeatType.RakingClaws, (int)FeatType.RendingBite });
        var beast = new EchaolmBeastDefinition().Build()[BeastType.Echaolm];
        var baseline = new PlainsThuneBeastDefinition().Build()[BeastType.PlainsThune];
        beast.Name.Should().Be("Echa'olm");
        beast.Role.Should().Be(BeastRoleType.Bruiser);
        beast.Appearance.Should().Be(AppearanceType.DogWinterWolf);
        beast.PortraitId.Should().Be(320);
        beast.AccuracyStat.Should().Be(baseline.AccuracyStat);
        beast.DamageStat.Should().Be(baseline.DamageStat);
        beast.Levels.Keys.Should().BeEquivalentTo(Enumerable.Range(1, 50));
        beast.Levels.Should().BeEquivalentTo(baseline.Levels);
        beast.PossibleMutations.Should().BeEmpty();
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
