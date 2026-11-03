using FluentAssertions;
using NUnit.Framework;
using System.Text.Json;

namespace SWLOR.Game.Server.Tests.Feature;

/// <summary>
/// Random spawns use the baked 2m walkmesh lattice, which only checks surface material.
/// Water cut off below steep banks along the map edges, an enclosed pond, the Deep Mountains
/// plateau overlooking the entrance, and a street-level walkmesh tile in the middle of the
/// open Veles Sewers Sewage Holding Area tank passed that check, so creatures spawned where
/// players could see or shoot them but never reach them (Outlaws floated over the tank).
/// No Spawn Zones keep those points out of the bake.
/// </summary>
public class ViscaraWildlifeSpawnZoneTests
{
    [TestCase("viscarawildlands", 170f, 0f, TestName = "Wildlands south edge water (west end)")]
    [TestCase("viscarawildlands", 242f, 0f, TestName = "Wildlands south edge water (middle)")]
    [TestCase("viscarawildlands", 318f, 66f, TestName = "Wildlands east edge water")]
    [TestCase("viscaralake", 24f, 94f, TestName = "Lake enclosed west pond")]
    [TestCase("viscaralake", 232f, 0f, TestName = "Lake south edge water")]
    [TestCase("viscaralake", 318f, 180f, TestName = "Lake east edge water")]
    [TestCase("viscaralake", 318f, 8f, TestName = "Lake south-east corner water")]
    [TestCase("viscaradeepmount", 310f, 200f, TestName = "Deep Mountains plateau beside the entrance")]
    [TestCase("viscaradeepmount", 250f, 150f, TestName = "Deep Mountains plateau lip above the entrance passage")]
    [TestCase("viscaradeepmount", 250f, 240f, TestName = "Deep Mountains basin enclosed by the plateau")]
    [TestCase("viscaradeepmount", 112f, 246f, TestName = "Deep Mountains isolated rock top")]
    [TestCase("veles_sewers", 140f, 50f, TestName = "Veles Sewers Holding Area tank (south-west corner)")]
    [TestCase("veles_sewers", 148f, 58f, TestName = "Veles Sewers Holding Area tank (north-east corner)")]
    public void UnreachableSpawnPoints_AreNoSpawnZones(string area, float x, float y)
    {
        LoadNoSpawnZones(area).Should().Contain(zone => IsInside(x, y, zone),
            $"({x}, {y}) in {area} cannot be reached on foot, so random spawns must not use it");
    }

    [TestCase("viscarawildlands", 162f, 2f, TestName = "Wildlands shallow water joined to the shore")]
    [TestCase("viscarawildlands", 150f, 60f, TestName = "Wildlands open ground")]
    [TestCase("viscaralake", 24f, 102f, TestName = "Lake shore beside the pond")]
    [TestCase("viscaralake", 32f, 94f, TestName = "Lake shore east of the pond")]
    [TestCase("viscaradeepmount", 300f, 146f, TestName = "Deep Mountains entrance passage")]
    [TestCase("viscaradeepmount", 170f, 200f, TestName = "Deep Mountains valley floor below the plateau")]
    [TestCase("veles_sewers", 144f, 70f, TestName = "Veles Sewers walkway north of the Holding Area")]
    [TestCase("veles_sewers", 158f, 60f, TestName = "Veles Sewers walkway east of the Holding Area")]
    public void ReachableSpawnPoints_StaySpawnable(string area, float x, float y)
    {
        LoadNoSpawnZones(area).Should().NotContain(zone => IsInside(x, y, zone),
            $"({x}, {y}) in {area} is reachable on foot and must keep receiving spawns");
    }

    private static (float X, float Y)[][] LoadNoSpawnZones(string area)
    {
        using var git = JsonDocument.Parse(File.ReadAllText(Path.Combine(FindRepositoryRoot(), "Module", "git", $"{area}.git.json")));
        return git.RootElement.GetProperty("TriggerList").GetProperty("value").EnumerateArray()
            .Where(t => t.GetProperty("Tag").GetProperty("value").GetString() == "anti_spawn_trigg")
            .Select(t => t.GetProperty("Geometry").GetProperty("value").EnumerateArray()
                .Select(p => (X: GetFloat(t, "XPosition") + GetFloat(p, "PointX"),
                              Y: GetFloat(t, "YPosition") + GetFloat(p, "PointY")))
                .ToArray())
            .ToArray();
    }

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

    private static float GetFloat(JsonElement element, string name) => element.GetProperty(name).GetProperty("value").GetSingle();

    private static string FindRepositoryRoot()
    {
        var directory = new DirectoryInfo(TestContext.CurrentContext.TestDirectory);
        while (directory != null && !File.Exists(Path.Combine(directory.FullName, "SWLOR.Game.Server.sln")))
            directory = directory.Parent;

        return directory?.FullName ?? throw new DirectoryNotFoundException("Could not locate repository root.");
    }
}
