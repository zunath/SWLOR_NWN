using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using SWLOR.Game.Server.EngineTests.Framework;
using SWLOR.Game.Server.Service;
using SWLOR.NWN.API.NWScript.Enum;
using NWNXLib = NWN.Native.API.NWNXLib;
using Vector3 = System.Numerics.Vector3;

namespace SWLOR.Game.Server.EngineTests.Definitions
{
    public static class RandomSpawnLocationEngineTests
    {
        private const float MinObjectDistance = 6f;
        private const float PlaceableNearbyMeters = 15f;
        private const float StepMeters = 0.5f;
        private const int MaxCreaturesPerArea = 600;

        /// <summary>
        /// Creates a Thune at the baked points beside placeables in the areas with the most placeable-blocked points
        /// and checks each one can step away from where the engine put it. A creature inside a placeable's walkmesh is
        /// blocked in every direction and never walks out.
        /// </summary>
        [EngineTest("Random spawns beside placeables can walk away", Category = "SpawnReachability", TimeoutSeconds = 300f)]
        public static async Task SpawnsBesidePlaceablesCanWalkAway(EngineTestContext ctx)
        {
            var server = NWNXLib.g_pAppManager.m_pServerExoApp;
            var failures = new List<string>();
            var created = 0;
            foreach (var resref in new[] { "dan_destroyfarm", "dath_mountains", "korr_waste2_zil", "dath_tribevill", "dath_grottos", "korr_waste_zil" })
            {
                var area = FindArea(resref);
                ctx.Assert(GetIsObjectValid(area), $"{resref} is loaded");
                var placeables = GetNonWalkablePlaceableTriangles(area).Select(t => t.Origin).Distinct().ToList();
                var points = GetBakedPoints()[resref]
                    .Where(p => placeables.Any(o => Vector2Distance(p, o) <= PlaceableNearbyMeters))
                    .ToList();
                var stride = Math.Max(1, points.Count / MaxCreaturesPerArea);
                points = points.Where((_, index) => index % stride == 0).ToList();

                var nativeArea = server.GetAreaByGameObjectID(area);
                // Thunes are wide, so neighbors 2 m apart would shove each other; each batch keeps them 8 m apart.
                var batches = points
                    .GroupBy(p => ((int)(p.X / 2) % 4, (int)(p.Y / 2) % 4))
                    .SelectMany(group => group.Chunk(150));
                foreach (var batch in batches)
                {
                    var creatures = new List<uint>();
                    AssignCommand(GetModule(), () =>
                    {
                        foreach (var point in batch)
                            creatures.Add(CreateObject(ObjectType.Creature, "thune", Location(area, point, 0f)));
                    });
                    foreach (var creature in creatures)
                        ctx.Track(creature);
                    await ctx.DelaySecondsAsync(0.5f);

                    for (var index = 0; index < creatures.Count; index++)
                    {
                        var p = GetPosition(creatures[index]);
                        var canStep = false;
                        for (var angle = 0; angle < 360 && !canStep; angle += 45)
                        {
                            var dx = StepMeters * MathF.Cos(angle * MathF.PI / 180f);
                            var dy = StepMeters * MathF.Sin(angle * MathF.PI / 180f);
                            canStep = nativeArea.NoNonWalkPolys(p.X, p.Y, p.X + dx, p.Y + dy, p.Z - 1f, p.Z + 2f, 0f) != 0;
                        }

                        if (!canStep && failures.Count < 20)
                            failures.Add($"{resref} ({batch[index].X:0},{batch[index].Y:0}) -> ({p.X:0.0},{p.Y:0.0})");
                    }

                    created += creatures.Count;
                    AssignCommand(GetModule(), () =>
                    {
                        foreach (var creature in creatures)
                            DestroyObject(creature);
                    });
                }
            }

            ctx.Log($"Created {created} Thunes beside placeables.");
            ctx.Assert(failures.Count == 0, "Thunes that cannot step away: " + string.Join("; ", failures));
        }

        /// <summary>
        /// The 6 m object clearance is measured from the ground, so an area whose floor sits at Z 5 keeps the full radius
        /// instead of the 3.3 m a Z 0 measurement leaves. Placeables without a walkmesh are decoration and do not count.
        /// </summary>
        [EngineTest("Random spawn points keep 6 m from objects measured from the ground", Category = "SpawnReachability", TimeoutSeconds = 120f)]
        public static async Task BakedPointsKeepTheirObjectDistanceFromTheGround(EngineTestContext ctx)
        {
            await Task.CompletedTask;
            var area = FindArea("dan_destroyfarm");
            ctx.Assert(GetIsObjectValid(area), "dan_destroyfarm is loaded");

            var objects = new List<(Vector3 Position, string Name)>();
            for (var obj = GetFirstObjectInArea(area); GetIsObjectValid(obj); obj = GetNextObjectInArea(area))
            {
                var type = GetObjectType(obj);
                if (IsSpawned(obj))
                    continue;
                if ((type == ObjectType.Placeable && HasWalkmesh(obj)) || type == ObjectType.Door || type == ObjectType.Trigger)
                    objects.Add((GetPosition(obj), GetResRef(obj)));
            }

            var points = GetBakedPoints()["dan_destroyfarm"];
            ctx.Assert(points.Count > 1000, $"dan_destroyfarm keeps a large spawn pool ({points.Count} points)");
            foreach (var point in points)
            {
                foreach (var (position, name) in objects)
                {
                    ctx.Assert(Vector3.Distance(point, position) > MinObjectDistance,
                        $"({point.X:0},{point.Y:0},{point.Z:0.0}) is {Vector3.Distance(point, position):0.0} m from {name}");
                }
            }
        }

        /// <summary>
        /// Creates a Thune at every baked point near a placeable in the Ruined Farmlands and checks none of them ends
        /// up inside a placeable walkmesh, where it could not walk out.
        /// </summary>
        [EngineTest("Ruined Farmlands random spawns never land inside a placeable", Category = "SpawnReachability", TimeoutSeconds = 180f)]
        public static async Task RuinedFarmlandsSpawnsNeverLandInsideAPlaceable(EngineTestContext ctx)
        {
            var area = FindArea("dan_destroyfarm");
            ctx.Assert(GetIsObjectValid(area), "dan_destroyfarm is loaded");

            var meshTriangles = GetNonWalkablePlaceableTriangles(area);
            var triangles = new TriangleIndex(meshTriangles);
            var placeables = meshTriangles.Select(t => t.Origin).Distinct().ToList();
            var points = GetBakedPoints()["dan_destroyfarm"]
                .Where(p => placeables.Any(o => Vector2Distance(p, o) <= PlaceableNearbyMeters))
                .ToList();
            ctx.Assert(points.Count > 0, "some baked points lie near placeables");

            const int batchSize = 100;
            for (var start = 0; start < points.Count; start += batchSize)
            {
                var batch = points.Skip(start).Take(batchSize).ToList();
                var created = new List<uint>();
                AssignCommand(GetModule(), () =>
                {
                    foreach (var point in batch)
                        created.Add(CreateObject(ObjectType.Creature, "thune", Location(area, point, 0f)));
                });
                foreach (var creature in created)
                    ctx.Track(creature);
                await ctx.DelaySecondsAsync(0.5f);

                foreach (var creature in created)
                {
                    var position = GetPosition(creature);
                    var hit = triangles.FindCovering(position);
                    ctx.Assert(hit.Placeable == null,
                        $"A Thune created at a baked point ended inside {hit.Placeable} at ({position.X:0.0},{position.Y:0.0})");
                }

                AssignCommand(GetModule(), () =>
                {
                    foreach (var creature in created)
                        DestroyObject(creature);
                });
            }

            ctx.Log($"Created Thunes at {points.Count} baked points near placeables.");
        }

        private static Dictionary<string, List<Vector3>> GetBakedPoints()
        {
            var field = typeof(Walkmesh).GetField("_walkmeshesByArea", BindingFlags.NonPublic | BindingFlags.Static);
            return (Dictionary<string, List<Vector3>>)field!.GetValue(null)!;
        }

        private static uint FindArea(string resref)
        {
            for (var area = GetFirstArea(); GetIsObjectValid(area); area = GetNextArea())
            {
                if (GetResRef(area) == resref)
                    return area;
            }

            return OBJECT_INVALID;
        }

        private static bool HasWalkmesh(uint placeable)
        {
            var server = NWNXLib.g_pAppManager.m_pServerExoApp;
            var mesh = server.m_pcExoAppInternal.m_pPlaceMeshManager.GetWalkMesh(server.GetPlaceableByGameObjectID(placeable).m_nAppearance);
            return mesh != null && mesh.m_nTriangles > 0;
        }

        // Spawned objects (resource nodes, creatures) are placed on baked points after the bake.
        private static bool IsSpawned(uint obj) => !string.IsNullOrEmpty(GetLocalString(obj, "SPAWN_ID"));

        private static float Vector2Distance(Vector3 a, Vector3 b) =>
            MathF.Sqrt((a.X - b.X) * (a.X - b.X) + (a.Y - b.Y) * (a.Y - b.Y));

        private static List<MeshTriangle> GetNonWalkablePlaceableTriangles(uint area)
        {
            var server = NWNXLib.g_pAppManager.m_pServerExoApp;
            var meshManager = server.m_pcExoAppInternal.m_pPlaceMeshManager;
            var walkable = new Dictionary<int, bool>();
            var triangles = new List<MeshTriangle>();
            for (var obj = GetFirstObjectInArea(area); GetIsObjectValid(obj); obj = GetNextObjectInArea(area))
            {
                if (GetObjectType(obj) != ObjectType.Placeable || IsSpawned(obj))
                    continue;

                var placeable = server.GetPlaceableByGameObjectID(obj);
                var mesh = meshManager.GetWalkMesh(placeable.m_nAppearance);
                if (mesh == null || mesh.m_nTriangles <= 0 || mesh.m_pvVertices == null)
                    continue;

                var origin = GetPosition(obj);
                var cos = MathF.Cos(placeable.m_fBearing);
                var sin = MathF.Sin(placeable.m_fBearing);
                unsafe
                {
                    var vertices = (float*)mesh.m_pvVertices.Pointer;
                    Vector3 World(int index)
                    {
                        var v = vertices + index * 3;
                        return new Vector3(origin.X + v[0] * cos - v[1] * sin, origin.Y + v[0] * sin + v[1] * cos, origin.Z + v[2]);
                    }

                    for (var t = 0; t < mesh.m_nTriangles; t++)
                    {
                        var material = mesh.m_pnTriangleMaterial[t];
                        if (!walkable.TryGetValue(material, out var isWalkable))
                            walkable[material] = isWalkable = Get2DAString("surfacemat", "Walk", material) == "1";
                        if (isWalkable)
                            continue;

                        triangles.Add(new MeshTriangle(World(mesh.m_pnTriangles[t * 3]), World(mesh.m_pnTriangles[t * 3 + 1]),
                            World(mesh.m_pnTriangles[t * 3 + 2]), origin, GetResRef(obj)));
                    }
                }
            }

            return triangles;
        }

        private sealed class TriangleIndex
        {
            private const float CellMeters = 10f;
            private readonly Dictionary<(int X, int Y), List<MeshTriangle>> _cells = new();

            public TriangleIndex(IEnumerable<MeshTriangle> triangles)
            {
                foreach (var triangle in triangles)
                {
                    var minX = (int)MathF.Floor(MathF.Min(triangle.A.X, MathF.Min(triangle.B.X, triangle.C.X)) / CellMeters);
                    var maxX = (int)MathF.Floor(MathF.Max(triangle.A.X, MathF.Max(triangle.B.X, triangle.C.X)) / CellMeters);
                    var minY = (int)MathF.Floor(MathF.Min(triangle.A.Y, MathF.Min(triangle.B.Y, triangle.C.Y)) / CellMeters);
                    var maxY = (int)MathF.Floor(MathF.Max(triangle.A.Y, MathF.Max(triangle.B.Y, triangle.C.Y)) / CellMeters);
                    for (var x = minX; x <= maxX; x++)
                    for (var y = minY; y <= maxY; y++)
                    {
                        if (!_cells.TryGetValue((x, y), out var list))
                            _cells[(x, y)] = list = new List<MeshTriangle>();
                        list.Add(triangle);
                    }
                }
            }

            public MeshTriangle FindCovering(Vector3 point)
            {
                var cell = ((int)MathF.Floor(point.X / CellMeters), (int)MathF.Floor(point.Y / CellMeters));
                return _cells.TryGetValue(cell, out var list) ? list.FirstOrDefault(t => t.Covers(point)) : default;
            }
        }

        private readonly record struct MeshTriangle(Vector3 A, Vector3 B, Vector3 C, Vector3 Origin, string Placeable)
        {
            // Matches the bake's height window: polygons from 1 m below to 2 m above the point.
            public bool Covers(Vector3 point)
            {
                var lowest = MathF.Min(A.Z, MathF.Min(B.Z, C.Z));
                if (lowest > point.Z + 2f || MathF.Max(A.Z, MathF.Max(B.Z, C.Z)) < point.Z - 1f)
                    return false;

                var d1 = Side(point, A, B);
                var d2 = Side(point, B, C);
                var d3 = Side(point, C, A);
                // Strictly inside; a point on an edge can still step out.
                return (d1 > 0 && d2 > 0 && d3 > 0) || (d1 < 0 && d2 < 0 && d3 < 0);
            }

            private static float Side(Vector3 p, Vector3 a, Vector3 b) => (p.X - b.X) * (a.Y - b.Y) - (a.X - b.X) * (p.Y - b.Y);
        }
    }
}
