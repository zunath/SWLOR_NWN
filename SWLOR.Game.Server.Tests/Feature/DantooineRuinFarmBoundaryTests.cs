using System.Text.Json;
using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Feature.SpawnDefinition;
using SWLOR.Game.Server.Service.AIService;

namespace SWLOR.Game.Server.Tests.Feature;

public class DantooineRuinFarmBoundaryTests
{
    [Test]
    public void Wildlife_ReturnsHomeAfterWandering()
    {
        var spawns = new DantooineSpawnDefinition().BuildSpawnTables()["DANTOOINE_RUIN_FARM"].Spawns;
        spawns.Should().NotBeEmpty().And.OnlyContain(spawn =>
            spawn.AIFlags.HasFlag(AIFlag.ReturnHome) && spawn.AIFlags.HasFlag(AIFlag.RandomWalk));
    }

    [Test]
    public void EastBoundary_IsBlockedAndExcludedFromRandomSpawns()
    {
        using var git = LoadArea();
        var floors = git.RootElement.GetProperty("Placeable List").GetProperty("value").EnumerateArray()
            .Where(p => p.GetProperty("Tag").GetProperty("value").GetString()!.StartsWith("dan_east_edge_"))
            .ToArray();
        floors.Should().OnlyContain(p => Number(p, "Appearance") == 5690 &&
            Number(p, "AnimationState") == 4 && Number(p, "Static") == 1 && Number(p, "Useable") == 0);

        var zone = git.RootElement.GetProperty("TriggerList").GetProperty("value").EnumerateArray()
            .Single(t => t.GetProperty("TemplateResRef").GetProperty("value").GetString() == "anti_spawn_trigg");
        var vertices = zone.GetProperty("Geometry").GetProperty("value").EnumerateArray()
            .Select(v => (X: Number(zone, "XPosition") + Number(v, "PointX"),
                          Y: Number(zone, "YPosition") + Number(v, "PointY"))).ToArray();

        for (var y = 0.5f; y < 150; y += 1)
        {
            if (y < 110)
                floors.Should().Contain(p => Covers(p, 129.5f, y), $"the open east edge at y={y} needs collision");
            vertices.Min(v => v.X).Should().BeLessThan(124.5f);
            vertices.Max(v => v.X).Should().BeGreaterThan(130f);
            vertices.Min(v => v.Y).Should().BeLessThan(y);
            vertices.Max(v => v.Y).Should().BeGreaterThan(y);
        }

        foreach (var (x, y) in new[] { (125.26f, 5.93f), (117.69f, 72.95f), (122f, 70f) })
            floors.Should().NotContain(p => Covers(p, x, y), "the entrance and public eastern approach must remain open");
        vertices.Min(v => v.X).Should().BeGreaterThan(122f, "reachable ground must still receive spawns");
    }

    private static bool Covers(JsonElement floor, float x, float y)
    {
        var angle = Number(floor, "Bearing");
        var dx = x - Number(floor, "X");
        var dy = y - Number(floor, "Y");
        return MathF.Abs(dx * MathF.Cos(angle) + dy * MathF.Sin(angle)) <= 5.001f &&
               MathF.Abs(-dx * MathF.Sin(angle) + dy * MathF.Cos(angle)) <= 1.001f;
    }

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
