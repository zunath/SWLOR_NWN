using System.Text.Json;
using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Feature;
using SWLOR.Game.Server.Feature.SpawnDefinition;
using SWLOR.Game.Server.Service.AIService;

namespace SWLOR.Game.Server.Tests.Feature;

public class DantooineRuinFarmBoundaryTests
{
    [Test]
    public void Wildlife_RoamsWithinSafeBoundsAndCanReturnAfterCombat()
    {
        var spawns = new DantooineSpawnDefinition().BuildSpawnTables()["DANTOOINE_RUIN_FARM"].Spawns;
        spawns.Select(spawn => spawn.Resref).Should().BeEquivalentTo("pthune", "gizka", "thune");
        spawns.Should().OnlyContain(spawn => spawn.AIFlags == AIFlag.ReturnHome);
        spawns.Should().OnlyContain(spawn => spawn.OnSpawnActions.Count == 1 &&
            spawn.OnSpawnActions[0].Method.DeclaringType == typeof(DantooineRuinFarmWildlife) &&
            spawn.OnSpawnActions[0].Method.Name == nameof(DantooineRuinFarmWildlife.AttachHeartbeat),
            "these wildlife entries use their area-specific roaming heartbeat");
        new DantooineSpawnDefinition().BuildSpawnTables()
            .Where(table => table.Key != "DANTOOINE_RUIN_FARM")
            .SelectMany(table => table.Value.Spawns).Should().OnlyContain(spawn =>
                spawn.OnSpawnActions.All(action => action.Method.DeclaringType != typeof(DantooineRuinFarmWildlife)),
                "wildlife elsewhere must not receive this heartbeat");
    }

    [TestCase(0f, 0f, 5f, true)]
    [TestCase(124f, 150f, 5f, true)]
    [TestCase(124.1f, 70f, 5f, false)]
    [TestCase(-0.1f, 70f, 5f, false)]
    [TestCase(117f, -0.1f, 5f, false)]
    [TestCase(117f, 150.1f, 5f, false)]
    [TestCase(117f, 70f, float.NaN, false)]
    public void RoamingBounds_ExcludeUnsafePositions(float x, float y, float z, bool expected)
    {
        DantooineRuinFarmWildlife.IsSafePosition(new System.Numerics.Vector3(x, y, z)).Should().Be(expected);
    }

    [Test]
    public void EastBoundary_AddsNoCollisionOrMovementScripts()
    {
        using var git = LoadArea();
        var placeables = git.RootElement.GetProperty("Placeable List").GetProperty("value").EnumerateArray().ToArray();
        placeables.Should().HaveCount(27, "the area retains its original placed scenery");
        placeables.Should().NotContain(p => Number(p, "Appearance") == 5690 ||
            p.GetProperty("Tag").GetProperty("value").GetString()!.StartsWith("dan_east_edge_"),
            "an NPC wandering fix must not add invisible player barriers");

        var zone = LoadSpawnZone(git);
        Number(zone, "Type").Should().Be(0);
        Number(zone, "TrapFlag").Should().Be(0);
        foreach (var name in new[] { "ScriptOnEnter", "ScriptOnExit", "ScriptHeartbeat", "ScriptUserDefine", "OnClick" })
            zone.GetProperty(name).GetProperty("value").GetString().Should().BeEmpty(
                "the spawn exclusion must not move players or intercept their movement");
    }

    [Test]
    public void RandomSpawns_AvoidTheEastEdgeButKeepTheInterior()
    {
        using var git = LoadArea();
        var zone = LoadSpawnZone(git);
        var vertices = zone.GetProperty("Geometry").GetProperty("value").EnumerateArray()
            .Select(v => (X: Number(zone, "XPosition") + Number(v, "PointX"),
                          Y: Number(zone, "YPosition") + Number(v, "PointY"))).ToArray();

        for (var y = 0f; y < 150; y += 2)
        {
            vertices.Min(v => v.X).Should().BeLessThan(124.5f);
            vertices.Max(v => v.X).Should().BeGreaterThan(130f);
            vertices.Min(v => v.Y).Should().BeLessThan(y);
            vertices.Max(v => v.Y).Should().BeGreaterThan(y);
        }
        vertices.Min(v => v.X).Should().BeGreaterThan(122f,
            "reachable interior ground must still receive wildlife and resource spawns");
    }

    private static JsonElement LoadSpawnZone(JsonDocument git) => git.RootElement
        .GetProperty("TriggerList").GetProperty("value").EnumerateArray()
        .Single(t => t.GetProperty("TemplateResRef").GetProperty("value").GetString() == "anti_spawn_trigg");

    private static float Number(JsonElement element, string name) => element.GetProperty(name).GetProperty("value").GetSingle();

    private static JsonDocument LoadArea()
    {
        var root = new DirectoryInfo(TestContext.CurrentContext.TestDirectory);
        while (root != null && !File.Exists(Path.Combine(root.FullName, "SWLOR.Game.Server.sln")))
            root = root.Parent;
        return JsonDocument.Parse(File.ReadAllText(Path.Combine(
            root?.FullName ?? throw new DirectoryNotFoundException("Could not locate repository root."),
            "Module", "git", "dan_destroyfarm.git.json")));
    }
}
