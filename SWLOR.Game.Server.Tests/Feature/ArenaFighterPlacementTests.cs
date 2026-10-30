using System.Text.Json;
using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Feature.SpawnDefinition;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Tests.Feature;

public class ArenaFighterPlacementTests
{
    private const string SpawnTag = "NAR_ARENA_FIGHTERS";
    private const string ShippingDistrict = "pw_ar_nardocks";

    [Test]
    public void ArenaFighterSpawns_AreOnlyInTheShippingDistrict()
    {
        var locations = new List<string>();
        foreach (var path in Directory.EnumerateFiles(Path.Combine(FindRoot(), "Module", "git"), "*.git.json"))
        {
            using var document = JsonDocument.Parse(File.ReadAllText(path));
            if (!document.RootElement.TryGetProperty("WaypointList", out var list)) continue;
            foreach (var waypoint in list.GetProperty("value").EnumerateArray().Where(w => Text(w, "Tag") == SpawnTag))
                locations.Add(Path.GetFileName(path));
        }

        locations.Should().BeEquivalentTo(Enumerable.Repeat(ShippingDistrict + ".git.json", 8),
            "the three relocated spawn points and five additional fighters belong in the Shipping District");
    }

    [Test]
    public void RelocatedWaypoints_UseTheExistingArenaFighterSpawnTable()
    {
        using var git = Read("git", ShippingDistrict);
        var points = List(git.RootElement, "WaypointList").Where(w => Text(w, "Tag") == SpawnTag).ToArray();
        points.Should().HaveCount(8);
        points.Should().OnlyContain(w => Text(w, "TemplateResRef") == "nar_arenafight");
        points.Should().OnlyContain(w => Math.Abs(Number(w, "ZPosition")) < .01);
        points.Select(w => (Number(w, "XPosition"), Number(w, "YPosition")))
            .Should().OnlyHaveUniqueItems();

        var spawn = new NarShaddaaSpawnDefinition().BuildSpawnTables()[SpawnTag].Spawns.Should().ContainSingle().Subject;
        spawn.Type.Should().Be(ObjectType.Creature);
        spawn.Resref.Should().Be("nar_arenafight");
    }

    [TestCase("pw_ar_nsficlub")]
    [TestCase(ShippingDistrict)]
    public void WaypointComments_RemainAlignedWithPlacedWaypoints(string area)
    {
        using var git = Read("git", area);
        using var gic = Read("gic", area);
        var waypoints = List(git.RootElement, "WaypointList").ToArray();
        var comments = List(gic.RootElement, "WaypointList").ToArray();
        comments.Should().HaveCount(waypoints.Length);
        for (var index = 0; index < waypoints.Length; index++)
            if (Text(waypoints[index], "Tag") == SpawnTag)
                Text(comments[index], "Comment").Should().Contain("Shipping District").And.Contain(SpawnTag);
    }

    [Test]
    public void Fighters_KeepClearOfQuestGiversAndTravelArrivals()
    {
        using var git = Read("git", ShippingDistrict);
        var waypoints = List(git.RootElement, "WaypointList").ToArray();
        var fighters = waypoints.Where(w => Text(w, "Tag") == SpawnTag).ToArray();
        fighters.Should().HaveCount(8);
        foreach (var fighter in fighters)
        {
            foreach (var creature in List(git.RootElement, "Creature List"))
                Distance(fighter, creature).Should().BeGreaterThan(50, "quest givers must remain outside the relocated encounter");
            foreach (var arrival in waypoints.Where(w => Text(w, "Tag") is not (SpawnTag or "NAR_THIEVES")))
                Distance(fighter, arrival).Should().BeGreaterThan(30, "players need room at travel and recovery arrivals");
        }
    }

    private static double Distance(JsonElement left, JsonElement right) => Math.Sqrt(
        Math.Pow(Number(left, "XPosition") - Number(right, "XPosition"), 2) +
        Math.Pow(Number(left, "YPosition") - Number(right, "YPosition"), 2));
    private static IEnumerable<JsonElement> List(JsonElement root, string field) => root.GetProperty(field).GetProperty("value").EnumerateArray();
    private static string Text(JsonElement element, string field) => element.GetProperty(field).GetProperty("value").GetString()!;
    private static double Number(JsonElement element, string field) => element.GetProperty(field).GetProperty("value").GetDouble();
    private static JsonDocument Read(string kind, string area) => JsonDocument.Parse(File.ReadAllText(
        Path.Combine(FindRoot(), "Module", kind, area + "." + kind + ".json")));
    private static string FindRoot()
    {
        var root = new DirectoryInfo(TestContext.CurrentContext.TestDirectory);
        while (root != null && !File.Exists(Path.Combine(root.FullName, "SWLOR.Game.Server.sln"))) root = root.Parent;
        return root?.FullName ?? throw new DirectoryNotFoundException("Repository root not found.");
    }
}
