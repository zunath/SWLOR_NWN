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
    [Test]
    public void VelesEspionageWorkbench_HasAWalkableApproachWithinInteractionRange()
    {
        var root = FindRoot();
        using var git = JsonDocument.Parse(File.ReadAllText(Path.Combine(root, "Module", "git", "veles_shops.git.json")));
        var bench = git.RootElement.GetProperty("Placeable List").GetProperty("value").EnumerateArray()
            .Single(p => Text(p, "TemplateResRef") == "espionage_bench");
        var position = new Vector3(Number(bench, "X"), Number(bench, "Y"), Number(bench, "Z"));
        var triangles = ReadWalkableFloor(root, "veles_shops", "sw_t_modint2");
        var bodyOffsets = new[] { Vector3.Zero, new Vector3(.4f, 0, 0), new Vector3(-.4f, 0, 0), new Vector3(0, .4f, 0), new Vector3(0, -.4f, 0) };
        var approaches = new[] { new Vector3(1.5f, 0, 0), new Vector3(-1.5f, 0, 0), new Vector3(0, 1.5f, 0), new Vector3(0, -1.5f, 0) };
        approaches.Any(approach => bodyOffsets.All(offset => triangles.Any(t => Covers(t, position + approach + offset))))
            .Should().BeTrue("the workbench must have room for a player to approach on the native floor");
    }

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

    [Test]
    public void CzerkaWeaponsFacility_UsesTwentyFixedGeneralSpawnsOnClearFloor()
    {
        var root = FindRoot();
        using var git = JsonDocument.Parse(File.ReadAllText(Path.Combine(root, "Module", "git", "pw_ar_czarmrange.git.json")));
        var area = git.RootElement;
        area.GetProperty("VarTable").GetProperty("value").EnumerateArray()
            .Should().NotContain(v => Text(v, "Name") == "CREATURE_SPAWN_TABLE_ID",
                "random area positions can select floor enclosed by static scenery");
        var waypoints = area.GetProperty("WaypointList").GetProperty("value").EnumerateArray().ToArray();
        var generalSpawns = waypoints.Where(w => Text(w, "Tag") == "CAPSTONE_CZERKA_ARMS_TEST_RANGE").ToArray();
        generalSpawns.Should().HaveCount(20, "the 16 by 16 area previously received twenty random general spawns");
        generalSpawns.Select(w => (Number(w, "XPosition"), Number(w, "YPosition"))).Should().OnlyHaveUniqueItems();

        var spawnPoints = waypoints.Where(w => Text(w, "Tag") == "CAPSTONE_CZERKA_ARMS_TEST_RANGE"
            || Text(w, "Tag") == "CZERKA_ARMS_TEST_RANGE_RARES" || Text(w, "Tag").EndsWith("_WD_SPAWN")).ToArray();
        spawnPoints.Should().HaveCount(24);
        var floor = ReadWalkableFloor(root, "pw_ar_czarmrange", "sw_t_garage");
        var appearances = TwoDAReader.Read(Path.Combine(root, "SWLOR_Haks", "sw_2da", "placeables.2da"));
        var meshes = Directory.EnumerateFiles(Path.Combine(root, "SWLOR_Haks"), "*.pwk", SearchOption.AllDirectories)
            .ToDictionary(Path.GetFileNameWithoutExtension, StringComparer.OrdinalIgnoreCase);
        foreach (var point in spawnPoints)
        {
            var position = new Vector3(Number(point, "XPosition"), Number(point, "YPosition"), Number(point, "ZPosition"));
            var body = Enumerable.Range(0, 16).Select(i => position + new Vector3(
                .75f * MathF.Cos(i * MathF.PI / 8), .75f * MathF.Sin(i * MathF.PI / 8), 0)).Prepend(position).ToArray();
            foreach (var sample in body)
                floor.Any(t => Covers(t, sample)).Should().BeTrue($"{Text(point, "Tag")} needs room for a creature at {sample}");

            foreach (var placeable in area.GetProperty("Placeable List").GetProperty("value").EnumerateArray())
            {
                var model = appearances.GetValue((int)Number(placeable, "Appearance"), "ModelName");
                // Models without PWKs do not contribute native collision.
                if (!meshes.TryGetValue(model, out var mesh)) continue;
                var placement = Matrix4x4.CreateRotationZ(Number(placeable, "Bearing"))
                    * Matrix4x4.CreateTranslation(Number(placeable, "X"), Number(placeable, "Y"), Number(placeable, "Z"));
                foreach (var bounds in ReadCollisionBounds(mesh, placement))
                foreach (var sample in body)
                {
                    var local = Vector3.Transform(sample, bounds.Inverse);
                    var overlaps = local.X >= bounds.Min.X && local.X <= bounds.Max.X
                        && local.Y >= bounds.Min.Y && local.Y <= bounds.Max.Y
                        && local.Z + 1.8f > bounds.Min.Z && local.Z + .05f < bounds.Max.Z;
                    overlaps.Should().BeFalse($"{Text(point, "Tag")} at {position} must clear {model}'s collision volume");
                }
            }
        }
    }

    [Test]
    public void FightClubBackrooms_UsesTwentyFixedGeneralSpawnsOnClearFloor()
    {
        var root = FindRoot();
        using var git = JsonDocument.Parse(File.ReadAllText(Path.Combine(root, "Module", "git", "pw_sc_emfbackr.git.json")));
        var area = git.RootElement;
        area.GetProperty("VarTable").GetProperty("value").EnumerateArray()
            .Should().NotContain(v => Text(v, "Name") == "CREATURE_SPAWN_TABLE_ID",
                "random area positions can select inaccessible floor outside the backrooms");
        var spawns = area.GetProperty("WaypointList").GetProperty("value").EnumerateArray()
            .Where(w => Text(w, "Tag") == "CAPSTONE_SMUGGLERS_MOON_FIGHT_CLUB_BACKROOMS").ToArray();
        spawns.Should().HaveCount(20, "the 16 by 16 area previously received twenty random general spawns");
        spawns.Select(w => (Number(w, "XPosition"), Number(w, "YPosition"))).Should().OnlyHaveUniqueItems();

        var floor = ReadWalkableFloor(root, "pw_sc_emfbackr", "sw_t_office");
        var appearances = TwoDAReader.Read(Path.Combine(root, "SWLOR_Haks", "sw_2da", "placeables.2da"));
        var meshes = Directory.EnumerateFiles(Path.Combine(root, "SWLOR_Haks"), "*.pwk", SearchOption.AllDirectories)
            .ToDictionary(Path.GetFileNameWithoutExtension, StringComparer.OrdinalIgnoreCase);
        var scenery = area.GetProperty("Placeable List").GetProperty("value").EnumerateArray()
            .Select(p => (Model: appearances.GetValue((int)Number(p, "Appearance"), "ModelName"), Placeable: p))
            .Where(p => meshes.ContainsKey(p.Model))
            .SelectMany(p => ReadCollisionBounds(meshes[p.Model], Matrix4x4.CreateRotationZ(Number(p.Placeable, "Bearing"))
                * Matrix4x4.CreateTranslation(Number(p.Placeable, "X"), Number(p.Placeable, "Y"), Number(p.Placeable, "Z"))))
            .ToArray();

        foreach (var spawn in spawns)
        {
            var position = new Vector3(Number(spawn, "XPosition"), Number(spawn, "YPosition"), Number(spawn, "ZPosition"));
            var body = Enumerable.Range(0, 16).Select(i => position + new Vector3(
                .75f * MathF.Cos(i * MathF.PI / 8), .75f * MathF.Sin(i * MathF.PI / 8), 0)).Prepend(position);
            foreach (var sample in body)
            {
                floor.Any(t => Covers(t, sample)).Should().BeTrue($"the backroom spawn at {position} needs walkable floor at {sample}");
                scenery.Any(bounds => Inside(bounds, sample)).Should().BeFalse(
                    $"the backroom spawn at {position} must clear scenery at {sample}");
            }
        }
    }

    [Test]
    public void FightClubBackrooms_EverySpawnIsReachableOnFootFromTheEntrance()
    {
        var root = FindRoot();
        using var git = JsonDocument.Parse(File.ReadAllText(Path.Combine(root, "Module", "git", "pw_sc_emfbackr.git.json")));
        var area = git.RootElement;
        var placeables = area.GetProperty("Placeable List").GetProperty("value").EnumerateArray().ToArray();
        // Sections joined only by teleports left chasing enemies stuck behind walls next to the player.
        placeables.Where(p => Text(p, "TemplateResRef") == "tele_obj").Select(p => Local(p, "DESTINATION"))
            .Should().Equal(new[] { "SMUG_BACK_ENT" }, "the exit is the only teleport; every room connects on foot");

        const float cell = .25f;
        const int size = (int)(160 / cell);
        var floor = new bool[size, size];
        foreach (var t in ReadWalkableFloor(root, "pw_sc_emfbackr", "sw_t_office"))
            Rasterize(t, cell, size, (i, j, z) => { if (MathF.Abs(z) < .5f) floor[i, j] = true; });

        var blocked = new bool[size, size];
        var appearances = TwoDAReader.Read(Path.Combine(root, "SWLOR_Haks", "sw_2da", "placeables.2da"));
        var meshes = Directory.EnumerateFiles(Path.Combine(root, "SWLOR_Haks"), "*.pwk", SearchOption.AllDirectories)
            .ToDictionary(Path.GetFileNameWithoutExtension, StringComparer.OrdinalIgnoreCase);
        foreach (var placeable in placeables)
        {
            if (!meshes.TryGetValue(appearances.GetValue((int)Number(placeable, "Appearance"), "ModelName"), out var mesh)) continue;
            var placement = Matrix4x4.CreateRotationZ(Number(placeable, "Bearing"))
                * Matrix4x4.CreateTranslation(Number(placeable, "X"), Number(placeable, "Y"), Number(placeable, "Z"));
            // Slice collision at body height; bounding boxes would seal doorway panels, which are one mesh each.
            foreach (var t in ReadCollisionTriangles(mesh, placement))
            foreach (var height in new[] { .5f, 1f, 1.5f })
            {
                var crossings = new List<Vector3>();
                for (var e = 0; e < 3; e++)
                {
                    var a = t[e]; var b = t[(e + 1) % 3];
                    if (a.Z - height < 0 != b.Z - height < 0) crossings.Add(Vector3.Lerp(a, b, (height - a.Z) / (b.Z - a.Z)));
                }
                if (crossings.Count != 2) continue;
                var steps = (int)(Vector3.Distance(crossings[0], crossings[1]) / (cell / 4)) + 2;
                for (var s = 0; s < steps; s++)
                {
                    var p = Vector3.Lerp(crossings[0], crossings[1], s / (float)(steps - 1));
                    var (i, j) = ((int)(p.X / cell), (int)(p.Y / cell));
                    if (i >= 0 && j >= 0 && i < size && j < size) blocked[i, j] = true;
                }
            }
        }

        // A creature body needs half a metre of clearance from walls and the floor edge.
        const int radius = 2;
        var free = new bool[size, size];
        for (var i = 0; i < size; i++)
        for (var j = 0; j < size; j++)
        {
            free[i, j] = true;
            for (var di = -radius; di <= radius && free[i, j]; di++)
            for (var dj = -radius; dj <= radius && free[i, j]; dj++)
            {
                // Measure to the nearest edge of the neighbouring cell, not its centre.
                var dx = MathF.Max(0, MathF.Abs(di) - .5f);
                var dy = MathF.Max(0, MathF.Abs(dj) - .5f);
                if (dx * dx + dy * dy > radius * radius) continue;
                var (x, y) = (i + di, j + dj);
                free[i, j] = x >= 0 && y >= 0 && x < size && y < size && floor[x, y] && !blocked[x, y];
            }
        }

        var waypoints = area.GetProperty("WaypointList").GetProperty("value").EnumerateArray().ToArray();
        (int, int) CellOf(JsonElement w) => ((int)(Number(w, "XPosition") / cell), (int)(Number(w, "YPosition") / cell));
        var reached = new bool[size, size];
        var queue = new Queue<(int, int)>();
        var entrance = CellOf(waypoints.Single(w => Text(w, "Tag") == "SMUG_BACKROOMS"));
        free[entrance.Item1, entrance.Item2].Should().BeTrue("the entrance arrival point needs clear floor");
        reached[entrance.Item1, entrance.Item2] = true;
        queue.Enqueue(entrance);
        while (queue.Count > 0)
        {
            var (i, j) = queue.Dequeue();
            foreach (var (x, y) in new[] { (i + 1, j), (i - 1, j), (i, j + 1), (i, j - 1) })
                if (x >= 0 && y >= 0 && x < size && y < size && free[x, y] && !reached[x, y])
                {
                    reached[x, y] = true;
                    queue.Enqueue((x, y));
                }
        }

        var spawns = waypoints.Where(w => Text(w, "Tag") is "CAPSTONE_SMUGGLERS_MOON_FIGHT_CLUB_BACKROOMS" or "FIGHTCLUB_BACKROOMS_RARES"
            || Text(w, "Tag").EndsWith("_WD_SPAWN")).ToArray();
        spawns.Should().HaveCount(24);
        foreach (var spawn in spawns)
        {
            var (i, j) = CellOf(spawn);
            reached[i, j].Should().BeTrue(
                $"{Text(spawn, "Tag")} at ({Number(spawn, "XPosition")}, {Number(spawn, "YPosition")}) must be reachable on foot from the entrance");
        }
    }

    // Qion Test Site, Forge Caverns and Canyon Range use base-game tiles whose walkmeshes are not in the repository.
    [TestCase("pw_sc_jedihalls", "sw_t_office")]
    [TestCase("pw_sc_cryptdeep", "sw_t_alienruin")]
    [TestCase("pw_sc_repubeng", "sw_t_facility")]
    [TestCase("pw_sc_tarnpres", "sw_t_jungle")]
    public void SlicingTerminals_HaveAClearApproachOnTheirUseSide(string area, string hakFolder)
    {
        var root = FindRoot();
        using var git = JsonDocument.Parse(File.ReadAllText(Path.Combine(root, "Module", "git", area + ".git.json")));
        var terminals = git.RootElement.GetProperty("WaypointList").GetProperty("value").EnumerateArray()
            .Where(w => Text(w, "Tag") == "SLICING_TERMINAL_T5").ToArray();
        terminals.Should().HaveCount(2);
        var floor = ReadWalkableFloor(root, area, hakFolder);
        var appearances = TwoDAReader.Read(Path.Combine(root, "SWLOR_Haks", "sw_2da", "placeables.2da"));
        var meshes = Directory.EnumerateFiles(Path.Combine(root, "SWLOR_Haks"), "*.pwk", SearchOption.AllDirectories)
            .ToDictionary(Path.GetFileNameWithoutExtension, StringComparer.OrdinalIgnoreCase);
        var scenery = git.RootElement.GetProperty("Placeable List").GetProperty("value").EnumerateArray()
            .Select(p => (Model: appearances.GetValue((int)Number(p, "Appearance"), "ModelName"), Placeable: p))
            .Where(p => meshes.ContainsKey(p.Model))
            .SelectMany(p => ReadCollisionBounds(meshes[p.Model], Matrix4x4.CreateRotationZ(Number(p.Placeable, "Bearing"))
                * Matrix4x4.CreateTranslation(Number(p.Placeable, "X"), Number(p.Placeable, "Y"), Number(p.Placeable, "Z"))))
            .ToArray();

        foreach (var terminal in terminals)
        {
            var position = new Vector3(Number(terminal, "XPosition"), Number(terminal, "YPosition"), Number(terminal, "ZPosition"));
            var facing = new Vector3(Number(terminal, "XOrientation"), Number(terminal, "YOrientation"), 0);
            // The terminal model (shp_crate1_m) is used from the side opposite its facing; verified in the client.
            var approach = position - facing * 1.5f;
            var body = Enumerable.Range(0, 8).Select(i => approach + new Vector3(
                .4f * MathF.Cos(i * MathF.PI / 4), .4f * MathF.Sin(i * MathF.PI / 4), 0)).Prepend(approach).ToArray();
            foreach (var sample in body)
            {
                floor.Any(t => Covers(t, sample, .5f)).Should().BeTrue(
                    $"{area} slicing terminal at {position} needs walkable floor on its use side at {sample}");
                scenery.Any(bounds => Inside(bounds, sample)).Should().BeFalse(
                    $"{area} slicing terminal at {position} needs its use side at {sample} clear of scenery");
            }
        }
    }

    private static bool Inside((Vector3 Min, Vector3 Max, Matrix4x4 Inverse) bounds, Vector3 sample)
    {
        var local = Vector3.Transform(sample, bounds.Inverse);
        return local.X >= bounds.Min.X && local.X <= bounds.Max.X
            && local.Y >= bounds.Min.Y && local.Y <= bounds.Max.Y
            && local.Z + 1.8f > bounds.Min.Z && local.Z + .05f < bounds.Max.Z;
    }

    private static IEnumerable<(Vector3 Min, Vector3 Max, Matrix4x4 Inverse)> ReadCollisionBounds(string file, Matrix4x4 placement)
    {
        foreach (Match node in Regex.Matches(File.ReadAllText(file), @"(?is)node\s+trimesh\s+\S+\s+(.*?)endnode"))
        {
            var lines = node.Groups[1].Value.Split('\n').Select(l => l.Split((char[])null, StringSplitOptions.RemoveEmptyEntries)).Where(l => l.Length > 0).ToArray();
            var vi = Array.FindIndex(lines, l => l[0] == "verts");
            if (vi < 0 || int.Parse(lines[vi][1]) == 0) continue;
            var vertices = lines.Skip(vi + 1).Take(int.Parse(lines[vi][1])).Select(l => new Vector3(Parse(l[0]), Parse(l[1]), Parse(l[2]))).ToArray();
            var position = lines.Single(l => l[0] == "position").Skip(1).Select(Parse).ToArray();
            var orientation = lines.Single(l => l[0] == "orientation").Skip(1).Select(Parse).ToArray();
            var axis = new Vector3(orientation[0], orientation[1], orientation[2]);
            var rotation = Math.Abs(orientation[3]) < .00001f ? Quaternion.Identity : Quaternion.CreateFromAxisAngle(Vector3.Normalize(axis), orientation[3]);
            var transform = Matrix4x4.CreateFromQuaternion(rotation) * Matrix4x4.CreateTranslation(position[0], position[1], position[2]) * placement;
            Matrix4x4.Invert(transform, out var inverse).Should().BeTrue();
            yield return (vertices.Aggregate(Vector3.Min), vertices.Aggregate(Vector3.Max), inverse);
        }
    }

    private static IEnumerable<Vector3[]> ReadCollisionTriangles(string file, Matrix4x4 placement)
    {
        foreach (Match node in Regex.Matches(File.ReadAllText(file), @"(?is)node\s+trimesh\s+\S+\s+(.*?)endnode"))
        {
            var lines = node.Groups[1].Value.Split('\n').Select(l => l.Split((char[])null, StringSplitOptions.RemoveEmptyEntries)).Where(l => l.Length > 0).ToArray();
            var vi = Array.FindIndex(lines, l => l[0] == "verts");
            var fi = Array.FindIndex(lines, l => l[0] == "faces");
            if (vi < 0 || fi < 0 || int.Parse(lines[vi][1]) == 0) continue;
            var position = lines.Single(l => l[0] == "position").Skip(1).Select(Parse).ToArray();
            var orientation = lines.Single(l => l[0] == "orientation").Skip(1).Select(Parse).ToArray();
            var axis = new Vector3(orientation[0], orientation[1], orientation[2]);
            var rotation = Math.Abs(orientation[3]) < .00001f ? Quaternion.Identity : Quaternion.CreateFromAxisAngle(Vector3.Normalize(axis), orientation[3]);
            var transform = Matrix4x4.CreateFromQuaternion(rotation) * Matrix4x4.CreateTranslation(position[0], position[1], position[2]) * placement;
            var vertices = lines.Skip(vi + 1).Take(int.Parse(lines[vi][1]))
                .Select(l => Vector3.Transform(new Vector3(Parse(l[0]), Parse(l[1]), Parse(l[2])), transform)).ToArray();
            foreach (var face in lines.Skip(fi + 1).Take(int.Parse(lines[fi][1])))
                yield return face.Take(3).Select(i => vertices[int.Parse(i)]).ToArray();
        }
    }

    // Calls back with the height of the triangle at every grid cell centre it covers.
    private static void Rasterize(Vector3[] triangle, float cell, int size, Action<int, int, float> covered)
    {
        var a = triangle[0]; var u = triangle[1] - a; var v = triangle[2] - a;
        var determinant = u.X * v.Y - u.Y * v.X;
        if (Math.Abs(determinant) < .00001f) return;
        var min = Vector3.Min(Vector3.Min(triangle[0], triangle[1]), triangle[2]);
        var max = Vector3.Max(Vector3.Max(triangle[0], triangle[1]), triangle[2]);
        for (var i = Math.Max(0, (int)(min.X / cell)); i <= Math.Min(size - 1, (int)(max.X / cell)); i++)
        for (var j = Math.Max(0, (int)(min.Y / cell)); j <= Math.Min(size - 1, (int)(max.Y / cell)); j++)
        {
            var dx = (i + .5f) * cell - a.X; var dy = (j + .5f) * cell - a.Y;
            var s = (dx * v.Y - dy * v.X) / determinant;
            var t = (u.X * dy - u.Y * dx) / determinant;
            if (s >= -.00001f && t >= -.00001f && s + t <= 1.00001f) covered(i, j, a.Z + s * u.Z + t * v.Z);
        }
    }

    private static string Local(JsonElement o, string name) => o.GetProperty("VarTable").GetProperty("value").EnumerateArray()
        .Single(v => Text(v, "Name") == name).GetProperty("Value").GetProperty("value").GetString()!;

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

    private static bool Covers(Vector3[] triangle, Vector3 point, float heightTolerance = .05f)
    {
        var a = triangle[0]; var u = triangle[1] - a; var v = triangle[2] - a; var d = point - a;
        var determinant = u.X * v.Y - u.Y * v.X;
        if (Math.Abs(determinant) < .00001f) return false;
        var s = (d.X * v.Y - d.Y * v.X) / determinant;
        var t = (u.X * d.Y - u.Y * d.X) / determinant;
        return s >= -.00001f && t >= -.00001f && s + t <= 1.00001f && Math.Abs(a.Z + s * u.Z + t * v.Z - point.Z) <= heightTolerance;
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
