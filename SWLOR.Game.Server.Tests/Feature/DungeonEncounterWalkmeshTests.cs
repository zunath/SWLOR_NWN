using System.Globalization;
using System.Numerics;
using System.Text.Json;
using System.Text.RegularExpressions;
using FluentAssertions;
using NUnit.Framework;
using SWLOR.NWN.Formats.TwoDA;

namespace SWLOR.Game.Server.Tests.Feature;

public class DungeonEncounterWalkmeshTests
{
    [TestCase("pw_sc_smarena", "sw_t_office")]
    [TestCase("pw_sc_dantmedsub", "sw_t_garage")]
    public void RepairedEncounterPoints_StandOnNativeWalkableFloor(string area, string hakFolder)
    {
        var root = FindRoot();
        using var git = JsonDocument.Parse(File.ReadAllText(Path.Combine(root, "Module", "git", area + ".git.json")));
        var points = git.RootElement.GetProperty("WaypointList").GetProperty("value").EnumerateArray()
            .Concat(git.RootElement.GetProperty("Placeable List").GetProperty("value").EnumerateArray())
            .Where(p => area == "pw_sc_smarena"
                ? Text(p, "Tag").EndsWith("_MS_SPAWN") || Text(p, "Tag").EndsWith("_ms_call") || Text(p, "Tag") == "STUCK_WAYPOINT"
                : Text(p, "Tag").Equals("CAPSTONE_INFCONDUIT_WD_SPAWN") || Text(p, "Tag").Equals("infconduit_wd_call"))
            .ToArray();
        points.Should().HaveCount(area == "pw_sc_smarena" ? 7 : 2);
        var triangles = ReadWalkableFloor(root, area, hakFolder);
        foreach (var point in points)
        {
            var suffix = point.TryGetProperty("XPosition", out _) ? "Position" : "";
            var position = new Vector3(Number(point, "X" + suffix), Number(point, "Y" + suffix), Number(point, "Z" + suffix));
            // Check room for a body, not merely a point on a wall or floor edge.
            foreach (var offset in new[] { Vector3.Zero, new Vector3(.5f, 0, 0), new Vector3(-.5f, 0, 0), new Vector3(0, .5f, 0), new Vector3(0, -.5f, 0) })
                triangles.Any(t => Covers(t, position + offset)).Should().BeTrue(
                    $"{area}/{Text(point, "Tag")} must have native walkable floor at {position + offset}");
        }
    }

    [Test]
    public void FightClubArena_HasAContinuousCombatFloorThroughTheFormerPit()
    {
        var triangles = ReadWalkableFloor(FindRoot(), "pw_sc_smarena", "sw_t_office");
        // This was a non-walkable hole despite the visible metal floor placeables.
        for (var x = 18; x <= 42; x += 2)
        for (var y = 38; y <= 50; y += 2)
            triangles.Any(t => Covers(t, new Vector3(x, y, 0))).Should().BeTrue($"the combat floor must support walking at ({x}, {y})");
    }

    private static List<Vector3[]> ReadWalkableFloor(string root, string area, string hakFolder)
    {
        using var are = JsonDocument.Parse(File.ReadAllText(Path.Combine(root, "Module", "are", area + ".are.json")));
        var folder = Path.Combine(root, "SWLOR_Haks", hakFolder);
        var set = File.ReadAllText(Path.Combine(folder, Text(are.RootElement, "Tileset") + ".set"));
        var modelNames = Regex.Matches(set, @"(?ms)^\[TILE(\d+)\]\s*\r?\n(.*?)(?=^\[|\z)")
            .ToDictionary(m => int.Parse(m.Groups[1].Value), m => Regex.Match(m.Groups[2].Value, @"(?m)^Model=(\S+)").Groups[1].Value);
        var transition = float.Parse(Regex.Match(set, @"(?m)^Transition=(\S+)").Groups[1].Value, CultureInfo.InvariantCulture);
        var surfaces = TwoDAReader.Read(Path.Combine(root, "SWLOR_Haks", "sw_2da", "surfacemat.2da"));
        var cache = new Dictionary<string, List<Vector3[]>>();
        var triangles = new List<Vector3[]>();
        var width = (int)Number(are.RootElement, "Width");
        var tiles = are.RootElement.GetProperty("Tile_List").GetProperty("value").EnumerateArray().ToArray();
        for (var index = 0; index < tiles.Length; index++)
        {
            var tile = tiles[index];
            var model = modelNames[(int)Number(tile, "Tile_ID")];
            if (!cache.TryGetValue(model, out var local))
            {
                var file = Path.Combine(folder, model + ".wok");
                // Solid boundary models can come from the base game rather than the HAK.
                // Missing geometry never counts as walkable; an affected placement fails.
                local = File.Exists(file) ? ReadWok(file, surfaces) : new List<Vector3[]>();
                cache.Add(model, local);
            }
            var transform = Matrix4x4.CreateRotationZ(Number(tile, "Tile_Orientation") * MathF.PI / 2)
                * Matrix4x4.CreateTranslation(index % width * 10 + 5, index / width * 10 + 5, Number(tile, "Tile_Height") * transition);
            triangles.AddRange(local.Select(t => t.Select(v => Vector3.Transform(v, transform)).ToArray()));
        }
        triangles.Should().NotBeEmpty();
        return triangles;
    }

    private static List<Vector3[]> ReadWok(string file, TwoDAFile surfaces)
    {
        var result = new List<Vector3[]>();
        foreach (Match node in Regex.Matches(File.ReadAllText(file), @"(?is)node\s+aabb\s+\S+\s+(.*?)endnode"))
        {
            var lines = node.Groups[1].Value.Split('\n').Select(l => l.Split((char[])null, StringSplitOptions.RemoveEmptyEntries)).Where(l => l.Length > 0).ToArray();
            var position = lines.Single(l => l[0] == "position").Skip(1).Select(Parse).ToArray();
            var orientation = lines.Single(l => l[0] == "orientation").Skip(1).Select(Parse).ToArray();
            var axis = new Vector3(orientation[0], orientation[1], orientation[2]);
            var rotation = Math.Abs(orientation[3]) < .00001f ? Quaternion.Identity : Quaternion.CreateFromAxisAngle(Vector3.Normalize(axis), orientation[3]);
            var transform = Matrix4x4.CreateFromQuaternion(rotation) * Matrix4x4.CreateTranslation(position[0], position[1], position[2]);
            var vi = Array.FindIndex(lines, l => l[0] == "verts");
            var vertices = lines.Skip(vi + 1).Take(int.Parse(lines[vi][1])).Select(l => Vector3.Transform(new Vector3(Parse(l[0]), Parse(l[1]), Parse(l[2])), transform)).ToArray();
            var fi = Array.FindIndex(lines, l => l[0] == "faces");
            foreach (var face in lines.Skip(fi + 1).Take(int.Parse(lines[fi][1])))
                if (surfaces.GetValue(int.Parse(face[^1]), "Walk") == "1")
                    result.Add(face.Take(3).Select(i => vertices[int.Parse(i)]).ToArray());
        }
        return result;
    }

    private static bool Covers(Vector3[] triangle, Vector3 point)
    {
        var a = triangle[0]; var u = triangle[1] - a; var v = triangle[2] - a; var d = point - a;
        var determinant = u.X * v.Y - u.Y * v.X;
        if (Math.Abs(determinant) < .00001f) return false;
        var s = (d.X * v.Y - d.Y * v.X) / determinant;
        var t = (u.X * d.Y - u.Y * d.X) / determinant;
        return s >= -.00001f && t >= -.00001f && s + t <= 1.00001f && Math.Abs(a.Z + s * u.Z + t * v.Z - point.Z) <= .05f;
    }

    private static string Text(JsonElement o, string name) => o.GetProperty(name).GetProperty("value").GetString()!;
    private static float Number(JsonElement o, string name) => o.GetProperty(name).GetProperty("value").GetSingle();
    private static float Parse(string value) => float.Parse(value, CultureInfo.InvariantCulture);
    private static string FindRoot()
    {
        var root = new DirectoryInfo(TestContext.CurrentContext.TestDirectory);
        while (root != null && !File.Exists(Path.Combine(root.FullName, "SWLOR.Game.Server.sln"))) root = root.Parent;
        return root?.FullName ?? throw new DirectoryNotFoundException("Repository root not found.");
    }
}
