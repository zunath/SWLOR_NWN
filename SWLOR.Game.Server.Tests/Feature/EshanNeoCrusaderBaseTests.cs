using System.Text.Json;
using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.Feature.SpawnDefinition;

namespace SWLOR.Game.Server.Tests.Feature;

public class EshanNeoCrusaderBaseTests
{
    private const string AreaResref = "esh_nc_redoubt";
    private const string SpawnTableId = "ESHAN_NEOCRUSADER_BASE";

    [Test]
    public void ModuleIdentifier_PreservesRawVoidBytes()
    {
        var modulePath = Path.Combine(FindRepositoryRoot().FullName, "Module", "ifo", "module.ifo.json");
        var bytes = File.ReadAllBytes(modulePath);
        var modId = FindByteSequence(bytes, "\"Mod_ID\""u8);
        var value = FindByteSequence(bytes, "\"value\": \""u8, modId);

        bytes.AsSpan(value + 10, 4).ToArray().Should().Equal(0xff, 0xff, 0xff, 0xff);
        bytes.AsSpan(value + 10, 8)
            .SequenceEqual(new byte[] { 0xc3, 0xbf, 0xc3, 0xbf, 0xc3, 0xbf, 0xc3, 0xbf })
            .Should().BeFalse();
    }

    [Test]
    public void RuinedGarrison_IsRegisteredAndConfiguredAsAnEshanCombatArea()
    {
        using var module = Load("ifo", "module.ifo.json");
        var areas = List(module.RootElement, "Mod_Area_list")
            .Select(area => Text(area, "Area_Name"));
        areas.Should().ContainSingle(area => area == AreaResref);

        using var area = Load("are", $"{AreaResref}.are.json");
        Text(area.RootElement, "Name").Should().Be("Eshan - Ruined Garrison");
        Text(area.RootElement, "Tag").Should().Be("EshanRuinedGarrison");
        Text(area.RootElement, "Comments").Should().Be("Map Creator: scorchys");

        using var instances = Load("git", $"{AreaResref}.git.json");
        LocalText(instances.RootElement, "CREATURE_SPAWN_TABLE_ID").Should().Be(SpawnTableId);
        LocalInt(instances.RootElement, "CREATURE_SPAWN_COUNT").Should().Be(18);
        LocalInt(instances.RootElement, "PLANET_TYPE_ID").Should().Be((int)PlanetType.Eshan);
    }

    [Test]
    public void RuinedGarrison_HasTwoWayTravelWithTheBattlegrounds()
    {
        using var battlegrounds = Load("git", "pw_sc_eshbattle.git.json");
        using var redoubt = Load("git", $"{AreaResref}.git.json");

        List(battlegrounds.RootElement, "TriggerList")
            .Should().ContainSingle(trigger => Text(trigger, "LinkedTo") == "WP_esh_nc_base");
        var redoubtArrival = List(redoubt.RootElement, "WaypointList")
            .Single(waypoint => Text(waypoint, "Tag") == "WP_esh_nc_base");
        Text(redoubtArrival, "MapNote").Should().Be("Pass to the Eshan Battlegrounds");
        Number(redoubtArrival, "MapNoteEnabled").Should().Be(1);

        List(redoubt.RootElement, "TriggerList")
            .Should().ContainSingle(trigger => Text(trigger, "LinkedTo") == "WP_esh_battle_base");
        List(battlegrounds.RootElement, "WaypointList")
            .Should().ContainSingle(waypoint => Text(waypoint, "Tag") == "WP_esh_battle_base");
    }

    [Test]
    public void RuinedGarrison_DoesNotRetainCopiedViscaraTeleporters()
    {
        using var redoubt = Load("git", $"{AreaResref}.git.json");
        var placeables = List(redoubt.RootElement, "Placeable List");

        placeables.Should().NotContain(placeable => Text(placeable, "Tag") == "RepBaseExtEntrance");
        placeables.Should().NotContain(placeable => Text(placeable, "Tag") == "RepBaseExtJuniorMessEntrance");
        placeables.SelectMany(LocalVariables)
            .Should().NotContain(variable =>
                Text(variable, "Name") == "DESTINATION" &&
                Text(variable, "Value").StartsWith("WP_V_"));
    }

    [Test]
    public void RuinedGarrison_SpawnsTheNeoCrusaderGarrison()
    {
        var spawns = new EshanSpawnDefinition().BuildSpawnTables()[SpawnTableId].Spawns;

        spawns.Select(spawn => spawn.Resref).Should().BeEquivalentTo(
            "esh_nc_vanguard",
            "esh_nc_heavy",
            "esh_nc_medic",
            "esh_nc_captain");
    }

    [Test]
    public void RuinedGarrison_GicCommentsAlignWithPlacedInstances()
    {
        using var git = Load("git", $"{AreaResref}.git.json");
        using var gic = Load("gic", $"{AreaResref}.gic.json");
        foreach (var listName in new[]
                 {
                     "Creature List", "Door List", "Encounter List", "List", "Placeable List",
                     "SoundList", "StoreList", "TriggerList", "WaypointList",
                 })
        {
            List(gic.RootElement, listName).Length.Should().Be(List(git.RootElement, listName).Length,
                $"{listName} comments must remain aligned with placed instances");
        }
    }

    private static int FindByteSequence(byte[] source, ReadOnlySpan<byte> sequence, int start = 0)
    {
        var index = source.AsSpan(start).IndexOf(sequence);
        return index >= 0
            ? start + index
            : throw new InvalidDataException("Expected byte sequence was not found in module.ifo.json.");
    }

    private static JsonDocument Load(string directory, string fileName)
    {
        return JsonDocument.Parse(File.ReadAllText(Path.Combine(
            FindRepositoryRoot().FullName, "Module", directory, fileName)));
    }

    private static JsonElement[] List(JsonElement root, string name)
    {
        return root.GetProperty(name).GetProperty("value").EnumerateArray().ToArray();
    }

    private static string Text(JsonElement element, string name)
    {
        var value = element.GetProperty(name).GetProperty("value");
        return value.ValueKind == JsonValueKind.Object
            ? value.GetProperty("0").GetString() ?? string.Empty
            : value.GetString() ?? string.Empty;
    }

    private static int Number(JsonElement element, string name)
    {
        return element.GetProperty(name).GetProperty("value").GetInt32();
    }

    private static JsonElement[] LocalVariables(JsonElement instance)
    {
        return instance.TryGetProperty("VarTable", out var variables)
            ? variables.GetProperty("value").EnumerateArray().ToArray()
            : [];
    }

    private static JsonElement Local(JsonElement area, string name)
    {
        return List(area, "VarTable").Single(variable => Text(variable, "Name") == name);
    }

    private static string LocalText(JsonElement area, string name)
    {
        return Local(area, name).GetProperty("Value").GetProperty("value").GetString() ?? string.Empty;
    }

    private static int LocalInt(JsonElement area, string name)
    {
        return Local(area, name).GetProperty("Value").GetProperty("value").GetInt32();
    }

    private static DirectoryInfo FindRepositoryRoot()
    {
        var directory = new DirectoryInfo(TestContext.CurrentContext.TestDirectory);
        while (directory != null && !File.Exists(Path.Combine(directory.FullName, "SWLOR.Game.Server.sln")))
            directory = directory.Parent;

        return directory ?? throw new DirectoryNotFoundException("Could not locate repository root.");
    }
}
