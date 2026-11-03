using System.Numerics;
using System.Text.Json;
using System.Text.RegularExpressions;
using FluentAssertions;
using Nwn.Formats.NativeModels;
using NUnit.Framework;
using SWLOR.NWN.Formats.TwoDA;

namespace SWLOR.Game.Server.Tests.Feature;

/// <summary>
/// The client draws a static placeable only while the tile its origin sits on is visible. A udp2 void
/// filler tile (VisibilityNode T) is never visible, and a tile behind a closed door is hidden, so a static
/// wall anchored there renders as black void from the room it belongs to. A player standing on floor laid
/// over a void filler tile sees the whole area culled.
/// </summary>
public class RepublicBaseTileVisibilityTests
{
    private const float Ceiling = 4f;

    [TestCase("visredrepbase")]
    [TestCase("visrephanger")]
    public void StaticPlaceables_AreAnchoredInTheRoomTheyAreSeenFrom(string area)
    {
        var map = TileMap.Load(Root(), area);
        using var git = JsonDocument.Parse(File.ReadAllText(Path.Combine(Root(), "Module", "git", $"{area}.git.json")));
        var problems = new List<string>();
        foreach (var placeable in git.RootElement.GetProperty("Placeable List").GetProperty("value").EnumerateArray())
        {
            if (Number(placeable, "Static") != 1) continue;
            var vertices = VisibleVertices(placeable).ToList();
            if (vertices.Count == 0) continue;
            var origin = map.Region(Number(placeable, "X"), Number(placeable, "Y"));
            var regions = vertices.Select(v => map.Region(v.X, v.Y)).ToList();
            var elsewhere = regions.Count(region => region != null && region != origin) / (float)regions.Count;
            if (origin == null || !regions.Contains(origin) || elsewhere > .2f)
                problems.Add($"{Text(placeable, "Tag")} at ({Number(placeable, "X")}, {Number(placeable, "Y")})");
        }

        problems.Should().BeEmpty("a static placeable must sit on a visible tile of the room it is seen from, or be dynamic");
    }

    [Test]
    public void HeadquartersFloors_AreNotLaidOverVoidFillerTiles()
    {
        var map = TileMap.Load(Root(), "visredrepbase");
        using var git = JsonDocument.Parse(File.ReadAllText(Path.Combine(Root(), "Module", "git", "visredrepbase.git.json")));
        var floors = git.RootElement.GetProperty("Placeable List").GetProperty("value").EnumerateArray()
            .Where(p => Appearances.Value.GetValue((int)Number(p, "Appearance"), "Label") is { } label
                && label.Contains("Floor") && !label.Contains("Unwalkable"))
            .Select(p => (Transform: Placement(p), Bounds: ModelBounds(Model(p))))
            .Where(f => f.Bounds != null)
            .ToList();
        var problems = new List<string>();
        for (var x = 0; x < map.Width; x++)
        for (var y = 0; y < map.Height; y++)
        {
            if (!map.IsVoid(x, y)) continue;
            var covered = 0;
            for (var i = 0; i < 10; i++)
            for (var j = 0; j < 10; j++)
            {
                var point = new Vector3(x * 10 + .5f + i, y * 10 + .5f + j, 0);
                if (floors.Any(f => Matrix4x4.Invert(f.Transform, out var inverse)
                        && Vector3.Transform(point, inverse) is var local
                        && local.X >= f.Bounds!.Value.Min.X && local.X <= f.Bounds.Value.Max.X
                        && local.Y >= f.Bounds.Value.Min.Y && local.Y <= f.Bounds.Value.Max.Y))
                    covered++;
            }

            if (covered >= 70) problems.Add($"tile ({x}, {y}) is {covered}% covered by floor");
        }

        problems.Should().BeEmpty("rooms need real floor tiles under them, not void filler");
    }

    private static readonly Lazy<TwoDAFile> Appearances = new(() =>
        TwoDAReader.Read(Path.Combine(Root(), "SWLOR_Haks", "sw_2da", "placeables.2da")));

    private static readonly Lazy<Dictionary<string, string>> Models = new(() =>
        Directory.EnumerateDirectories(Path.Combine(Root(), "SWLOR_Haks"), "sw_plc*")
            .SelectMany(folder => Directory.EnumerateFiles(folder, "*.mdl"))
            .GroupBy(Path.GetFileNameWithoutExtension, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(group => group.Key, group => group.First(), StringComparer.OrdinalIgnoreCase));

    private static readonly Dictionary<string, Vector3[]?> ModelCache = new(StringComparer.OrdinalIgnoreCase);

    private static Vector3[]? Model(JsonElement placeable)
    {
        var name = Appearances.Value.GetValue((int)Number(placeable, "Appearance"), "ModelName");
        if (string.IsNullOrEmpty(name) || name == "****" || !Models.Value.TryGetValue(name, out var path)) return null;
        if (ModelCache.TryGetValue(name, out var cached)) return cached;
        var model = new MdlReader().Parse(File.ReadAllBytes(path));
        var vertices = model.GetMeshNodes().OfType<MdlTrimeshNode>()
            .Where(node => node.Render && !node.IsWalkmesh)
            .SelectMany(node => node.Vertices.Select(vertex => ToModelSpace(node, vertex)))
            .ToArray();
        return ModelCache[name] = vertices;
    }

    private static Vector3 ToModelSpace(MdlNode node, Vector3 vertex)
    {
        for (var current = node; current != null; current = current.Parent)
            vertex = Vector3.Transform(vertex, current.Orientation) + current.Position;
        return vertex;
    }

    private static (Vector3 Min, Vector3 Max)? ModelBounds(Vector3[]? vertices) =>
        vertices is { Length: > 0 } ? (vertices.Aggregate(Vector3.Min), vertices.Aggregate(Vector3.Max)) : null;

    private static Matrix4x4 Placement(JsonElement placeable) =>
        Matrix4x4.CreateRotationZ(Number(placeable, "Bearing")) *
        Matrix4x4.CreateTranslation(Number(placeable, "X"), Number(placeable, "Y"), Number(placeable, "Z"));

    // Geometry hidden under the floor or above the tile ceiling is never seen.
    private static IEnumerable<Vector3> VisibleVertices(JsonElement placeable)
    {
        var vertices = Model(placeable);
        if (vertices == null) yield break;
        var placement = Placement(placeable);
        foreach (var vertex in vertices)
        {
            var world = Vector3.Transform(vertex, placement);
            if (world.Z >= -.05f && world.Z <= Ceiling) yield return world;
        }
    }

    private sealed class TileMap
    {
        private static readonly string[] Corners = ["TopLeft", "BottomLeft", "BottomRight", "TopRight"];
        private static readonly string[] Edges = ["Top", "Left", "Bottom", "Right"];
        private readonly Dictionary<(int, int), int> _regions = new();
        private readonly HashSet<(int, int)> _void = new();

        public int Width { get; private init; }
        public int Height { get; private init; }

        public bool IsVoid(int x, int y) => _void.Contains((x, y));

        public int? Region(float x, float y) =>
            _regions.TryGetValue(((int)MathF.Floor(x / 10), (int)MathF.Floor(y / 10)), out var region) ? region : null;

        public static TileMap Load(string root, string area)
        {
            using var are = JsonDocument.Parse(File.ReadAllText(Path.Combine(root, "Module", "are", $"{area}.are.json")));
            var tileset = are.RootElement.GetProperty("Tileset").GetProperty("value").GetString()!;
            var set = Directory.EnumerateFiles(Path.Combine(root, "SWLOR_Haks"), $"{tileset}.set", SearchOption.AllDirectories).Single();
            var tiles = ReadSet(set);
            var map = new TileMap
            {
                Width = are.RootElement.GetProperty("Width").GetProperty("value").GetInt32(),
                Height = are.RootElement.GetProperty("Height").GetProperty("value").GetInt32(),
            };
            var info = new Dictionary<(int, int), (Dictionary<string, string> Tile, Dictionary<string, string> Corner, Dictionary<string, string> Edge)>();
            var list = are.RootElement.GetProperty("Tile_List").GetProperty("value").EnumerateArray().ToArray();
            for (var y = 0; y < map.Height; y++)
            for (var x = 0; x < map.Width; x++)
            {
                var entry = list[y * map.Width + x];
                var tile = tiles[entry.GetProperty("Tile_ID").GetProperty("value").GetInt32()];
                var turns = entry.GetProperty("Tile_Orientation").GetProperty("value").GetInt32();
                var corner = new Dictionary<string, string>();
                var edge = new Dictionary<string, string>();
                for (var i = 0; i < 4; i++)
                {
                    corner[Corners[(i + turns) % 4]] = tile.GetValueOrDefault(Corners[i], "");
                    edge[Edges[(i + turns) % 4]] = tile.GetValueOrDefault(Edges[i], "");
                }
                info[(x, y)] = (tile, corner, edge);
                if (tile.GetValueOrDefault("VisibilityNode") == "T") map._void.Add((x, y));
            }

            bool Open((int X, int Y) at, string side)
            {
                var (tile, corner, edge) = info[at];
                if (tile.GetValueOrDefault("VisibilityNode") == "T" || edge[side].StartsWith("Door")) return false;
                var pair = side switch
                {
                    "Top" => ("TopLeft", "TopRight"),
                    "Bottom" => ("BottomLeft", "BottomRight"),
                    "Left" => ("TopLeft", "BottomLeft"),
                    _ => ("TopRight", "BottomRight"),
                };
                return !(corner[pair.Item1] == "Wall" && corner[pair.Item2] == "Wall" && edge[side].Length == 0);
            }

            var next = 0;
            foreach (var start in info.Keys)
            {
                if (map._regions.ContainsKey(start) || map._void.Contains(start)) continue;
                var pending = new Stack<(int X, int Y)>([start]);
                map._regions[start] = next;
                while (pending.TryPop(out var at))
                {
                    foreach (var (side, dx, dy, opposite) in new[] { ("Top", 0, 1, "Bottom"), ("Bottom", 0, -1, "Top"), ("Left", -1, 0, "Right"), ("Right", 1, 0, "Left") })
                    {
                        var neighbour = (at.X + dx, at.Y + dy);
                        if (!info.ContainsKey(neighbour) || map._regions.ContainsKey(neighbour)) continue;
                        if (!Open(at, side) || !Open(neighbour, opposite)) continue;
                        map._regions[neighbour] = next;
                        pending.Push(neighbour);
                    }
                }
                next++;
            }

            return map;
        }

        private static Dictionary<int, Dictionary<string, string>> ReadSet(string path)
        {
            var tiles = new Dictionary<int, Dictionary<string, string>>();
            Dictionary<string, string>? current = null;
            foreach (var line in File.ReadLines(path).Select(l => l.Trim()))
            {
                var header = Regex.Match(line, @"^\[TILE(\d+)\]$");
                if (header.Success) tiles[int.Parse(header.Groups[1].Value)] = current = new Dictionary<string, string>();
                else if (line.StartsWith('[')) current = null;
                else if (current != null && line.IndexOf('=') is > 0 and var split)
                    current[line[..split]] = line[(split + 1)..];
            }
            return tiles;
        }
    }

    private static float Number(JsonElement element, string name) => element.GetProperty(name).GetProperty("value").GetSingle();
    private static string Text(JsonElement element, string name) => element.GetProperty(name).GetProperty("value").GetString() ?? "";

    private static string Root()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory != null && !Directory.Exists(Path.Combine(directory.FullName, "Module"))) directory = directory.Parent;
        return directory?.FullName ?? throw new DirectoryNotFoundException("Module directory not found.");
    }
}
