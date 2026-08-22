using System.Text.Json;
using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Feature.LootTableDefinition;
using SWLOR.Game.Server.Feature.SpawnDefinition;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Tests.Feature;

public class DantooineHayQuestSourceTests
{
    private const string HayTable = "DANTOOINE_HAY";

    [Test]
    public void HayBundles_HaveAResourceSpawnAndLootSource()
    {
        var spawnTables = new DantooineResourceSpawnDefinition().BuildSpawnTables();
        spawnTables.Should().ContainKey(HayTable);
        spawnTables[HayTable].Spawns.Should().ContainSingle(spawn =>
            spawn.Type == ObjectType.Placeable &&
            spawn.Resref == "dan_hay" &&
            spawn.Weight == 50);

        var lootTables = new DantooineLootTableDefinition().BuildLootTables();
        lootTables.Should().ContainKey(HayTable);
        lootTables[HayTable].Should().ContainSingle(item =>
            item.Resref == "haybundle" &&
            item.Weight == 50);
    }

    [Test]
    public void HayResource_UsesTheRenderableHayBundleAndOriginalRuinFarmNodes()
    {
        var root = FindRepositoryRoot();
        using var blueprint = JsonDocument.Parse(File.ReadAllText(
            Path.Combine(root.FullName, "Module", "utp", "dan_hay.utp.json")));
        blueprint.RootElement.GetProperty("Appearance").GetProperty("value").GetInt32()
            .Should().Be(69, "appearance 69 is the artwork-backed Farm: Hay Bundle row");
        GetLocalString(blueprint.RootElement, "SCAVENGE_POINT_LOOT_TABLE_NAME")
            .Should().Be(HayTable);

        using var area = JsonDocument.Parse(File.ReadAllText(
            Path.Combine(root.FullName, "Module", "git", "dan_destroyfarm.git.json")));
        var hayWaypoints = area.RootElement
            .GetProperty("WaypointList")
            .GetProperty("value")
            .EnumerateArray()
            .Where(waypoint => GetString(waypoint, "Tag") == HayTable)
            .ToArray();

        hayWaypoints.Should().HaveCount(8);
        hayWaypoints.Should().OnlyContain(waypoint => GetString(waypoint, "TemplateResRef") == "dan_hay");

        File.ReadAllText(Path.Combine(root.FullName, "Module", "itp", "placeablepalcus.itp.json"))
            .Should().Contain("\"value\": \"dan_hay\"");
        File.ReadAllText(Path.Combine(root.FullName, "Module", "itp", "waypointpalcus.itp.json"))
            .Should().Contain("\"value\": \"dan_hay\"");
    }

    private static string GetLocalString(JsonElement blueprint, string name)
    {
        foreach (var local in blueprint.GetProperty("VarTable").GetProperty("value").EnumerateArray())
        {
            if (GetString(local, "Name") == name)
                return GetString(local, "Value");
        }

        return string.Empty;
    }

    private static string GetString(JsonElement element, string propertyName) =>
        element.GetProperty(propertyName).GetProperty("value").GetString() ?? string.Empty;

    private static DirectoryInfo FindRepositoryRoot()
    {
        var directory = new DirectoryInfo(TestContext.CurrentContext.TestDirectory);
        while (directory != null && !File.Exists(Path.Combine(directory.FullName, "SWLOR.Game.Server.sln")))
            directory = directory.Parent;

        return directory ?? throw new DirectoryNotFoundException("Could not locate repository root.");
    }
}
