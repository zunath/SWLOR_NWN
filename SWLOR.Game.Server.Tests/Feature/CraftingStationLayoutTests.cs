using System.Globalization;
using System.Numerics;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Tests.Perks;
using SWLOR.NWN.Formats.TwoDA;

namespace SWLOR.Game.Server.Tests.Feature;

/// <summary>
/// Placed crafting stations keep clear of each other, of other placeables and of tile walls, and each use point stays on open floor.
/// The crafting revamp swapped small props (a pot on a table, a bench in a corner) for full-size station models, so areas laid out
/// for the old props had stations clipping into each other, into furniture and walls, or standing on tables. The check reads each
/// station's walkmesh footprint and use points (.pwk) and compiled model bounds, the other placeables' footprints and the tile
/// walkmeshes from the haks. Placeables and tiles whose walkmesh ships with the base game are not checked against.
/// </summary>
public class CraftingStationLayoutTests
{
    private static readonly string[] StationBlueprints =
    {
        "smithery_bench", "engineering_term", "fabrication_term", "cookpot", "espionage_bench", "cft_forge", "research_term",
        "lightsaber_bench"
    };

    // Footprints are the walkmesh, a little inside the visible model. A few centimetres against a wall is fine; more shows as clipping.
    private const float MaxFootprintOverlap = 0.05f;
    private const float MaxModelBoundsOverlap = 0.01f;
    private const float LevelTolerance = 0.6f;
    private const float StackHeight = 1.5f;
    private const float PropHeight = 0.3f;

    // The Veles foundry's walkmesh is one 5.5 by 7.5 m box over open floor around its crucible; the refineries stand beside the crucible.
    private static readonly HashSet<string> BoxWalkmeshModels = new(StringComparer.OrdinalIgnoreCase) { "zlc_fky_f86" };

    private sealed record Triangle(Vector3 A, Vector3 B, Vector3 C, bool Walkable);

    [Test]
    public void PlacedCraftingStations_DoNotClipAndKeepTheirUsePointsClear()
    {
        var root = PassivePerkIconTests.FindRepositoryRoot().FullName;
        var haks = Path.Combine(root, "SWLOR_Haks");
        var appearances = TwoDAReader.Read(Path.Combine(haks, "sw_2da", "placeables.2da"));
        var surfaces = TwoDAReader.Read(Path.Combine(haks, "sw_2da", "surfacemat.2da"));
        var placeableWalkmeshes = IndexFiles(haks, "sw_plc*", "*.pwk");
        var tileWalkmeshes = IndexFiles(haks, "sw_t_*", "*.wok");
        var tilesets = IndexFiles(haks, "sw_t_*", "*.set");
        var stationModels = JsonDocument.Parse(File.ReadAllText(Path.Combine(root, "tools", "StationModels.json"))).RootElement
            .GetProperty("stations").EnumerateArray().Select(s => s.GetProperty("model").GetString()!).ToHashSet(StringComparer.OrdinalIgnoreCase);
        placeableWalkmeshes.Should().ContainKeys(stationModels, "the SWLOR_Haks submodule must be checked out with sw_plc");
        tileWalkmeshes.Should().HaveCountGreaterThan(1000, "the SWLOR_Haks tileset walkmeshes must be checked out");

        var meshes = new Dictionary<string, (List<Triangle> Triangles, List<Vector2> UsePoints)>(StringComparer.OrdinalIgnoreCase);
        (List<Triangle> Triangles, List<Vector2> UsePoints) Mesh(string path)
        {
            if (!meshes.TryGetValue(path, out var mesh))
                meshes[path] = mesh = ReadWalkmesh(path, surfaces);
            return mesh;
        }

        var failures = new List<string>();
        var stations = 0;
        foreach (var path in Directory.GetFiles(Path.Combine(root, "Module", "git"), "*.git.json").OrderBy(p => p))
        {
            var text = File.ReadAllText(path, Encoding.Latin1);
            if (!StationBlueprints.Any(resref => text.Contains($"\"{resref}\"", StringComparison.Ordinal)))
                continue;

            var area = Path.GetFileName(path).Split('.')[0];
            using var git = JsonDocument.Parse(text);
            var placed = git.RootElement.GetProperty("Placeable List").GetProperty("value").EnumerateArray().Select((p, index) =>
            {
                var appearance = (int)Number(p, "Appearance");
                var model = appearance < appearances.RowCount ? appearances.GetValue(appearance, "ModelName") ?? string.Empty : string.Empty;
                var transform = Matrix4x4.CreateRotationZ(Number(p, "Bearing")) * Matrix4x4.CreateTranslation(Number(p, "X"), Number(p, "Y"), Number(p, "Z"));
                var mesh = placeableWalkmeshes.TryGetValue(model, out var file) ? Mesh(file) : (new List<Triangle>(), new List<Vector2>());
                return new
                {
                    Index = index,
                    ResRef = Text(p, "TemplateResRef"),
                    Model = model,
                    Position = new Vector3(Number(p, "X"), Number(p, "Y"), Number(p, "Z")),
                    Footprint = mesh.Item1.Where(t => !t.Walkable).Select(t => Transform(t, transform)).ToList(),
                    UsePoints = mesh.Item2.Select(u => Vector2.Transform(u, Matrix3x2.CreateRotation(Number(p, "Bearing")) * Matrix3x2.CreateTranslation(Number(p, "X"), Number(p, "Y")))).ToList(),
                    Bounds = stationModels.Contains(model) ? ModelBounds(Path.Combine(haks, "sw_plc", model + ".mdl"), transform) : new List<Triangle>()
                };
            }).ToList();
            var subjects = placed.Where(p => StationBlueprints.Contains(p.ResRef) && stationModels.Contains(p.Model)).ToList();
            if (subjects.Count == 0)
                continue;

            var (tiles, unknownTiles, width) = ReadTiles(root, area, tilesets, tileWalkmeshes, surfaces, Mesh);
            foreach (var station in subjects)
            {
                stations++;
                var label = $"{area} Placeable List[{station.Index}] {station.ResRef} at ({station.Position.X:F2}, {station.Position.Y:F2}, {station.Position.Z:F2})";
                // Props set on a station's work surface or hung on a wall stand above its base and are not floor obstacles.
                var obstacles = placed.Where(o => o.Index != station.Index && o.Position.Z - station.Position.Z <= PropHeight &&
                    station.Position.Z - o.Position.Z <= StackHeight && !BoxWalkmeshModels.Contains(o.Model)).ToList();
                foreach (var other in obstacles)
                {
                    if (other.Bounds.Count > 0 && other.Index > station.Index && Overlap(station.Bounds, other.Bounds) > MaxModelBoundsOverlap)
                        failures.Add($"{label}: its model overlaps the {other.ResRef} at Placeable List[{other.Index}]");
                    else if (other.Bounds.Count == 0 && Overlap(station.Footprint, other.Footprint) > MaxFootprintOverlap)
                        failures.Add($"{label}: its footprint overlaps the {other.ResRef} ({other.Model}) at Placeable List[{other.Index}]");
                }

                var walls = tiles.Where(t => !t.Walkable && Centroid(t).Z > station.Position.Z - 0.4f && Centroid(t).Z < station.Position.Z + 1.8f).ToList();
                if (Overlap(station.Footprint, walls) > MaxFootprintOverlap)
                    failures.Add($"{label}: its footprint overlaps the tile's walls");

                foreach (var use in station.UsePoints)
                {
                    var tile = (int)MathF.Floor(use.Y / 10) * width + (int)MathF.Floor(use.X / 10);
                    if (tiles.Count > 0 && !unknownTiles.Contains(tile) &&
                        !tiles.Any(t => t.Walkable && HeightAt(t, use) is { } z && MathF.Abs(z - station.Position.Z) < LevelTolerance))
                        failures.Add($"{label}: its use point ({use.X:F2}, {use.Y:F2}) is not on open floor at the station's height");

                    foreach (var other in obstacles)
                    {
                        if ((other.Bounds.Count > 0 ? other.Bounds : other.Footprint).Any(t => Contains(t, use)))
                            failures.Add($"{label}: its use point ({use.X:F2}, {use.Y:F2}) is inside the {other.ResRef} at Placeable List[{other.Index}]");
                    }
                }
            }
        }

        stations.Should().BeGreaterThan(50);
        failures.Should().BeEmpty(string.Join(Environment.NewLine, failures));
    }

    private static Dictionary<string, string> IndexFiles(string haks, string folders, string pattern)
    {
        var files = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (var folder in Directory.GetDirectories(haks, folders).OrderBy(f => f, StringComparer.Ordinal))
        {
            foreach (var file in Directory.EnumerateFiles(folder, pattern, new EnumerationOptions { MatchCasing = MatchCasing.CaseInsensitive }))
                files.TryAdd(Path.GetFileNameWithoutExtension(file), file);
        }

        return files;
    }

    private static (List<Triangle> Tiles, HashSet<int> Unknown, int Width) ReadTiles(string root, string area, Dictionary<string, string> tilesets,
        Dictionary<string, string> walkmeshes, TwoDAFile surfaces, Func<string, (List<Triangle> Triangles, List<Vector2> UsePoints)> mesh)
    {
        using var are = JsonDocument.Parse(File.ReadAllText(Path.Combine(root, "Module", "are", area + ".are.json"), Encoding.Latin1));
        var width = (int)Number(are.RootElement, "Width");
        var tiles = new List<Triangle>();
        var unknown = new HashSet<int>();
        var list = are.RootElement.GetProperty("Tile_List").GetProperty("value").EnumerateArray().ToArray();
        if (!tilesets.TryGetValue(Text(are.RootElement, "Tileset"), out var setFile))
        {
            unknown.UnionWith(Enumerable.Range(0, list.Length));
            return (tiles, unknown, width);
        }

        var set = File.ReadAllText(setFile, Encoding.Latin1).Replace("\r", string.Empty);
        var models = Regex.Matches(set, @"(?ms)^\[TILE(\d+)\]\s*\n(.*?)(?=^\[|\z)")
            .ToDictionary(m => int.Parse(m.Groups[1].Value), m => Regex.Match(m.Groups[2].Value, @"(?m)^Model=(\S+)").Groups[1].Value);
        var transition = float.Parse(Regex.Match(set, @"(?m)^Transition=(\S+)").Groups[1].Value, CultureInfo.InvariantCulture);
        for (var index = 0; index < list.Length; index++)
        {
            var tile = list[index];
            if (!walkmeshes.TryGetValue(models[(int)Number(tile, "Tile_ID")], out var file))
            {
                unknown.Add(index);
                continue;
            }

            var transform = Matrix4x4.CreateRotationZ(Number(tile, "Tile_Orientation") * MathF.PI / 2)
                * Matrix4x4.CreateTranslation(index % width * 10 + 5, index / width * 10 + 5, Number(tile, "Tile_Height") * transition);
            tiles.AddRange(mesh(file).Triangles.Select(t => Transform(t, transform)));
        }

        return (tiles, unknown, width);
    }

    // ASCII walkmesh: each mesh node's faces with their surface walkability, and the engine-readable use nodes.
    private static (List<Triangle> Triangles, List<Vector2> UsePoints) ReadWalkmesh(string file, TwoDAFile surfaces)
    {
        var triangles = new List<Triangle>();
        var uses = new List<Vector2>();
        foreach (Match node in Regex.Matches(File.ReadAllText(file, Encoding.Latin1), @"(?ims)^\s*node\s+(\S+)\s+(\S+)(.*?)^\s*endnode"))
        {
            var lines = node.Groups[3].Value.Split('\n').Select(l => l.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries)).Where(l => l.Length > 0).ToArray();
            var position = lines.FirstOrDefault(l => l[0].Equals("position", StringComparison.OrdinalIgnoreCase))?.Skip(1).Select(Parse).ToArray() ?? new float[3];
            if (node.Groups[1].Value.Equals("dummy", StringComparison.OrdinalIgnoreCase))
            {
                if (Regex.IsMatch(node.Groups[2].Value, @"^.{4}pwk_(use|dp_use_)0[12]$") && (position[0] != 0 || position[1] != 0))
                    uses.Add(new Vector2(position[0], position[1]));
                continue;
            }

            var vi = Array.FindIndex(lines, l => l[0].Equals("verts", StringComparison.OrdinalIgnoreCase));
            var fi = Array.FindIndex(lines, l => l[0].Equals("faces", StringComparison.OrdinalIgnoreCase));
            if (vi < 0 || fi < 0)
                continue;

            var orientation = lines.FirstOrDefault(l => l[0].Equals("orientation", StringComparison.OrdinalIgnoreCase))?.Skip(1).Select(Parse).ToArray() ?? new float[] { 0, 0, 1, 0 };
            var axis = new Vector3(orientation[0], orientation[1], orientation[2]);
            var rotation = MathF.Abs(orientation[3]) < 1e-5f || axis.LengthSquared() < 1e-9f ? Quaternion.Identity : Quaternion.CreateFromAxisAngle(Vector3.Normalize(axis), orientation[3]);
            var transform = Matrix4x4.CreateFromQuaternion(rotation) * Matrix4x4.CreateTranslation(position[0], position[1], position[2]);
            var vertices = lines.Skip(vi + 1).Take(int.Parse(lines[vi][1])).Select(l => Vector3.Transform(new Vector3(Parse(l[0]), Parse(l[1]), Parse(l[2])), transform)).ToArray();
            foreach (var face in lines.Skip(fi + 1).Take(int.Parse(lines[fi][1])))
                triangles.Add(new Triangle(vertices[int.Parse(face[0])], vertices[int.Parse(face[1])], vertices[int.Parse(face[2])],
                    surfaces.GetValue(int.Parse(face[^1]), "Walk") == "1"));
        }

        return (triangles, uses);
    }

    // A compiled model's header carries its bounding box; the plan rectangle stands for the visible model.
    private static List<Triangle> ModelBounds(string file, Matrix4x4 transform)
    {
        var header = new byte[176];
        using (var stream = File.OpenRead(file))
            stream.ReadExactly(header);
        float At(int offset) => BitConverter.ToSingle(header, offset);
        var (x0, y0, x1, y1) = (At(148), At(152), At(160), At(164));
        var corners = new[] { new Vector3(x0, y0, 0), new Vector3(x1, y0, 0), new Vector3(x1, y1, 0), new Vector3(x0, y1, 0) }
            .Select(c => Vector3.Transform(c, transform)).ToArray();
        return new List<Triangle> { new(corners[0], corners[1], corners[2], false), new(corners[0], corners[2], corners[3], false) };
    }

    private static Triangle Transform(Triangle t, Matrix4x4 m) =>
        new(Vector3.Transform(t.A, m), Vector3.Transform(t.B, m), Vector3.Transform(t.C, m), t.Walkable);

    private static Vector3 Centroid(Triangle t) => (t.A + t.B + t.C) / 3;

    private static float Cross(Vector2 a, Vector2 b, Vector2 p) => (b.X - a.X) * (p.Y - a.Y) - (b.Y - a.Y) * (p.X - a.X);

    private static Vector2 Flat(Vector3 v) => new(v.X, v.Y);

    private static bool Contains(Triangle t, Vector2 p)
    {
        var (a, b, c) = (Flat(t.A), Flat(t.B), Flat(t.C));
        if (MathF.Abs(Cross(a, b, c)) < 1e-6f)
            return false;
        var (d1, d2, d3) = (Cross(a, b, p), Cross(b, c, p), Cross(c, a, p));
        return !((d1 < -1e-6f || d2 < -1e-6f || d3 < -1e-6f) && (d1 > 1e-6f || d2 > 1e-6f || d3 > 1e-6f));
    }

    private static float? HeightAt(Triangle t, Vector2 p)
    {
        if (!Contains(t, p))
            return null;
        var (a, b, c) = (Flat(t.A), Flat(t.B), Flat(t.C));
        var area = Cross(a, b, c);
        var u = Cross(b, c, p) / area;
        var v = Cross(c, a, p) / area;
        return u * t.A.Z + v * t.B.Z + (1 - u - v) * t.C.Z;
    }

    private static float Overlap(List<Triangle> first, List<Triangle> second)
    {
        if (first.Count == 0 || second.Count == 0)
            return 0;
        var (minA, maxA) = Box(first);
        var total = 0f;
        foreach (var t in second)
        {
            var (minB, maxB) = Box(new List<Triangle> { t });
            if (minB.X > maxA.X || maxB.X < minA.X || minB.Y > maxA.Y || maxB.Y < minA.Y)
                continue;
            foreach (var s in first)
                total += Overlap(s, t);
        }

        return total;
    }

    private static (Vector2 Min, Vector2 Max) Box(List<Triangle> triangles)
    {
        var points = triangles.SelectMany(t => new[] { Flat(t.A), Flat(t.B), Flat(t.C) }).ToArray();
        return (new Vector2(points.Min(p => p.X), points.Min(p => p.Y)), new Vector2(points.Max(p => p.X), points.Max(p => p.Y)));
    }

    // Plan-view intersection area of two triangles (Sutherland-Hodgman clip).
    private static float Overlap(Triangle first, Triangle second)
    {
        var polygon = CounterClockwise(first);
        var clip = CounterClockwise(second);
        for (var i = 0; i < 3 && polygon.Count >= 3; i++)
        {
            var (a, b) = (clip[i], clip[(i + 1) % 3]);
            var next = new List<Vector2>();
            for (var j = 0; j < polygon.Count; j++)
            {
                var (p, q) = (polygon[j], polygon[(j + 1) % polygon.Count]);
                var (sp, sq) = (Cross(a, b, p), Cross(a, b, q));
                if (sp >= 0)
                    next.Add(p);
                if (sp >= 0 != sq >= 0)
                    next.Add(p + (q - p) * (sp / (sp - sq)));
            }

            polygon = next;
        }

        if (polygon.Count < 3)
            return 0;
        var area = 0f;
        for (var i = 0; i < polygon.Count; i++)
            area += polygon[i].X * polygon[(i + 1) % polygon.Count].Y - polygon[(i + 1) % polygon.Count].X * polygon[i].Y;
        return MathF.Abs(area) / 2;
    }

    private static List<Vector2> CounterClockwise(Triangle t)
    {
        var points = new List<Vector2> { Flat(t.A), Flat(t.B), Flat(t.C) };
        if (Cross(points[0], points[1], points[2]) < 0)
            points.Reverse();
        return points;
    }

    private static string Text(JsonElement element, string field) => element.GetProperty(field).GetProperty("value").GetString() ?? string.Empty;

    private static float Number(JsonElement element, string field) => element.GetProperty(field).GetProperty("value").GetSingle();

    private static float Parse(string value) => float.Parse(value, CultureInfo.InvariantCulture);
}
