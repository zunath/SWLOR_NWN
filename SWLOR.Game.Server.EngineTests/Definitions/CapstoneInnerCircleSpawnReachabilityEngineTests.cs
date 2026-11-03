using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using SWLOR.Game.Server.EngineTests.Framework;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.NWN.API.NWScript.Enum.Area;
using Vector3 = System.Numerics.Vector3;

namespace SWLOR.Game.Server.EngineTests.Definitions
{
    public static class CapstoneInnerCircleSpawnReachabilityEngineTests
    {
        // Half-metre cells; a step may rise this much, so ramps and stairs pass and ledges do not.
        private const float CellMeters = 0.5f;
        private const float MaxRisePerCell = 0.4f;
        private const float AnchorSearchMeters = 10f;
        private const int InnerCircleSpawnsPerLine = 4;

        private static readonly string[] CapstoneDungeons =
        {
            "pw_sc_velesannx", "cz220shipbreakin", "pw_ar_czarmrange", "pw_sc_canyonrng", "pw_sc_cryptdeep",
            "pw_sc_dantmedsub", "pw_sc_dath_apexd", "pw_sc_emfbackr", "pw_sc_forgecav", "pw_sc_jedihalls",
            "pw_sc_qiontest", "pw_sc_repubeng", "pw_sc_tarnpres",
        };

        /// <summary>
        /// Floods the live walkmesh of every capstone dungeon from its transitions and arrival waypoints and checks
        /// each inner circle spawn waypoint stands on walkable ground players can reach.
        /// </summary>
        [EngineTest("Capstone inner circle spawn points are on reachable walkable ground", Category = "SpawnReachability", TimeoutSeconds = 120f)]
        public static async Task InnerCircleSpawnPointsAreReachable(EngineTestContext ctx)
        {
            await Task.CompletedTask;
            foreach (var resref in CapstoneDungeons)
            {
                var area = FindArea(resref);
                ctx.Assert(GetIsObjectValid(area), $"{resref} is loaded");

                var spawns = new Dictionary<string, List<Vector3>>();
                for (var obj = GetFirstObjectInArea(area); GetIsObjectValid(obj); obj = GetNextObjectInArea(area))
                {
                    var tag = GetTag(obj);
                    if (GetObjectType(obj) != ObjectType.Waypoint || !tag.EndsWith("_IC_SPAWN"))
                        continue;
                    if (!spawns.TryGetValue(tag, out var points))
                        spawns[tag] = points = new List<Vector3>();
                    points.Add(GetPosition(obj));
                }

                ctx.Assert(spawns.Count == 3, $"{resref} has inner circle spawns for 3 lines (found {spawns.Count})");
                var reached = Flood(area, out var walk, out var z);
                foreach (var (tag, points) in spawns)
                {
                    ctx.Assert(points.Count == InnerCircleSpawnsPerLine, $"{resref} {tag} has {points.Count} spawn points");
                    foreach (var point in points)
                    {
                        var (x, y) = ((int)(point.X / CellMeters), (int)(point.Y / CellMeters));
                        ctx.Assert(walk[x, y] && reached[x, y],
                            $"{resref} {tag} at ({point.X:0.0},{point.Y:0.0}) is not on walkable ground players can reach");
                        ctx.Assert(Math.Abs(z[x, y] - point.Z) < 0.5f,
                            $"{resref} {tag} at ({point.X:0.0},{point.Y:0.0}) has Z {point.Z:0.00} but the ground is {z[x, y]:0.00}");
                    }
                }
            }
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

        private static bool[,] Flood(uint area, out bool[,] walk, out float[,] z)
        {
            var width = (int)(GetAreaSize(Dimension.Width, area) * 10 / CellMeters);
            var height = (int)(GetAreaSize(Dimension.Height, area) * 10 / CellMeters);
            walk = new bool[width, height];
            z = new float[width, height];
            var walkableMaterials = new Dictionary<int, bool>();
            for (var x = 0; x < width; x++)
            for (var y = 0; y < height; y++)
            {
                var location = Location(area, Vector3((x + 0.5f) * CellMeters, (y + 0.5f) * CellMeters), 0f);
                var material = GetSurfaceMaterial(location);
                if (!walkableMaterials.TryGetValue(material, out var isWalkable))
                    walkableMaterials[material] = isWalkable = Get2DAString("surfacemat", "Walk", material) == "1";
                walk[x, y] = isWalkable;
                z[x, y] = GetGroundHeight(location);
            }

            // Seeds: area transitions plus arrival and teleport destination waypoints (spawn waypoints excluded).
            var seeds = new List<Vector3>();
            for (var obj = GetFirstObjectInArea(area); GetIsObjectValid(obj); obj = GetNextObjectInArea(area))
            {
                var type = GetObjectType(obj);
                if ((type == ObjectType.Door || type == ObjectType.Trigger) && GetIsObjectValid(GetTransitionTarget(obj)))
                    seeds.Add(GetPosition(obj));
                var tag = GetTag(obj);
                if (type == ObjectType.Waypoint && !tag.StartsWith("CAPSTONE_") && !tag.EndsWith("_RARES") && !tag.EndsWith("_T5"))
                    seeds.Add(GetPosition(obj));
            }

            var reached = new bool[width, height];
            var queue = new Queue<(int X, int Y)>();
            foreach (var seed in seeds)
            {
                var anchor = NearestWalkable(walk, z, width, height, seed);
                if (anchor.X < 0 || reached[anchor.X, anchor.Y])
                    continue;
                reached[anchor.X, anchor.Y] = true;
                queue.Enqueue(anchor);
            }

            while (queue.Count > 0)
            {
                var (cx, cy) = queue.Dequeue();
                foreach (var (nx, ny) in new[] { (cx + 1, cy), (cx - 1, cy), (cx, cy + 1), (cx, cy - 1) })
                {
                    if (nx < 0 || ny < 0 || nx >= width || ny >= height || reached[nx, ny] || !walk[nx, ny] ||
                        Math.Abs(z[nx, ny] - z[cx, cy]) > MaxRisePerCell)
                        continue;
                    reached[nx, ny] = true;
                    queue.Enqueue((nx, ny));
                }
            }

            return reached;
        }

        private static (int X, int Y) NearestWalkable(bool[,] walk, float[,] z, int width, int height, Vector3 position)
        {
            var best = (X: -1, Y: -1);
            var bestDistance = float.MaxValue;
            var radius = (int)(AnchorSearchMeters / CellMeters);
            var px = (int)(position.X / CellMeters);
            var py = (int)(position.Y / CellMeters);
            for (var x = Math.Max(0, px - radius); x < Math.Min(width, px + radius); x++)
            for (var y = Math.Max(0, py - radius); y < Math.Min(height, py + radius); y++)
            {
                if (!walk[x, y])
                    continue;
                var distance = Vector3.DistanceSquared(new Vector3(x * CellMeters, y * CellMeters, z[x, y]), position);
                if (distance < bestDistance)
                {
                    bestDistance = distance;
                    best = (x, y);
                }
            }

            return best;
        }
    }
}
