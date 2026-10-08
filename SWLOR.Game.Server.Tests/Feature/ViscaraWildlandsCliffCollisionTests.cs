using FluentAssertions;
using NUnit.Framework;
using System.Text.Json;

namespace SWLOR.Game.Server.Tests.Feature;

/// <summary>
/// The "Landscape: Cliff - End 2" placeable (dag_tnocliff2) is a hollow mesa: its walkmesh is only a ring
/// around the rock walls. Where the ring crosses the area edge, players could walk into the hollow and
/// random spawns could land inside the rock. Each instance in Viscara - Wildlands must have its hollow
/// filled with unwalkable floor and its footprint covered by a No Spawn Zone.
/// </summary>
public class ViscaraWildlandsCliffCollisionTests
{
    private const int CliffEndAppearance = 3994;
    private const int UnwalkableFloorAppearance = 5699;
    private static readonly float[] HollowCentersAlongCliff = { 10f, 19f };

    [Test]
    public void CliffEndHollows_AreFilledWithUnwalkableFloor()
    {
        using var git = LoadAreaGit();
        var placeables = git.RootElement.GetProperty("Placeable List").GetProperty("value").EnumerateArray().ToArray();
        var floors = placeables
            .Where(p => GetInt(p, "Appearance") == UnwalkableFloorAppearance)
            .Select(p => (X: GetFloat(p, "X"), Y: GetFloat(p, "Y")))
            .ToArray();
        var cliffs = placeables.Where(p => GetInt(p, "Appearance") == CliffEndAppearance).ToArray();

        cliffs.Should().NotBeEmpty();
        foreach (var cliff in cliffs)
        {
            foreach (var along in HollowCentersAlongCliff)
            {
                var (x, y) = ToWorld(cliff, along, 0f);
                floors.Should().Contain(f => Distance(f.X, f.Y, Clamp(x), Clamp(y)) < 1f,
                    $"the cliff at ({GetFloat(cliff, "X"):0.0}, {GetFloat(cliff, "Y"):0.0}) must have its hollow filled");
            }
        }
    }

    [Test]
    public void CliffEndFootprints_AreNoSpawnZones()
    {
        using var git = LoadAreaGit();
        var zones = git.RootElement.GetProperty("TriggerList").GetProperty("value").EnumerateArray()
            .Where(t => t.GetProperty("Tag").GetProperty("value").GetString() == "anti_spawn_trigg")
            .Select(t => t.GetProperty("Geometry").GetProperty("value").EnumerateArray()
                .Select(p => (X: GetFloat(t, "XPosition") + GetFloat(p, "PointX"),
                              Y: GetFloat(t, "YPosition") + GetFloat(p, "PointY")))
                .ToArray())
            .ToArray();
        var cliffs = git.RootElement.GetProperty("Placeable List").GetProperty("value").EnumerateArray()
            .Where(p => GetInt(p, "Appearance") == CliffEndAppearance);

        foreach (var cliff in cliffs)
        {
            foreach (var along in HollowCentersAlongCliff)
            {
                var (x, y) = ToWorld(cliff, along, 0f);
                zones.Should().Contain(zone => IsInside(Clamp(x), Clamp(y), zone),
                    $"spawns must not be placed inside the cliff at ({GetFloat(cliff, "X"):0.0}, {GetFloat(cliff, "Y"):0.0})");
            }
        }
    }

    private static (float X, float Y) ToWorld(JsonElement placeable, float localX, float localY)
    {
        var bearing = GetFloat(placeable, "Bearing");
        var cos = MathF.Cos(bearing);
        var sin = MathF.Sin(bearing);
        return (GetFloat(placeable, "X") + localX * cos - localY * sin,
                GetFloat(placeable, "Y") + localX * sin + localY * cos);
    }

    private static float Clamp(float value) => Math.Clamp(value, 0.05f, 319.95f);

    private static float Distance(float ax, float ay, float bx, float by) => MathF.Sqrt((ax - bx) * (ax - bx) + (ay - by) * (ay - by));

    private static bool IsInside(float x, float y, (float X, float Y)[] polygon)
    {
        var inside = false;
        for (int i = 0, j = polygon.Length - 1; i < polygon.Length; j = i++)
        {
            var (ix, iy) = polygon[i];
            var (jx, jy) = polygon[j];
            if ((iy > y) != (jy > y) && x < (jx - ix) * (y - iy) / (jy - iy) + ix)
                inside = !inside;
        }

        return inside;
    }

    private static int GetInt(JsonElement element, string name) => element.GetProperty(name).GetProperty("value").GetInt32();

    private static float GetFloat(JsonElement element, string name) => element.GetProperty(name).GetProperty("value").GetSingle();

    private static JsonDocument LoadAreaGit()
    {
        var directory = new DirectoryInfo(TestContext.CurrentContext.TestDirectory);
        while (directory != null && !File.Exists(Path.Combine(directory.FullName, "SWLOR.Game.Server.sln")))
            directory = directory.Parent;

        if (directory == null)
            throw new DirectoryNotFoundException("Could not locate repository root.");

        return JsonDocument.Parse(File.ReadAllText(Path.Combine(directory.FullName, "Module", "git", "viscarawildlands.git.json")));
    }
}
