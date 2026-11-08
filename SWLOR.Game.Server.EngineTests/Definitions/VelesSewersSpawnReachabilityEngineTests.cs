using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using SWLOR.Game.Server.EngineTests.Framework;
using SWLOR.Game.Server.Service;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.NWN.API.NWScript.Enum.Area;
using NWNXLib = NWN.Native.API.NWNXLib;
using Vector3 = System.Numerics.Vector3;

namespace SWLOR.Game.Server.EngineTests.Definitions
{
    public static class VelesSewersSpawnReachabilityEngineTests
    {
        private const string AreaResref = "veles_sewers";
        // Half-metre cells; a step may rise this much, so ramps pass and ledges do not.
        private const float CellMeters = 0.5f;
        private const float MaxRisePerCell = 0.4f;
        private const float AnchorSearchMeters = 10f;
        private const int RowsPerFrame = 20;

        /// <summary>
        /// Floods the Veles Sewers walkmesh from every arrival waypoint, stepping only where the engine's collision test
        /// finds no tile wall or placeable walkmesh in the way, and checks every baked random spawn point lies in the
        /// flooded floor. Outlaws spawned in sealed pockets (the small room behind the Defunct Machine, the Sewer
        /// Processing room inside its force fields, the Holding Area tank) where players could see them but never
        /// reach them.
        /// </summary>
        [EngineTest("Veles Sewers random spawn points are reachable from the entrances", Category = "SpawnReachability", TimeoutSeconds = 300f)]
        public static async Task VelesSewersRandomSpawnPointsAreReachable(EngineTestContext ctx)
        {
            var area = FindArea(AreaResref);
            ctx.Assert(GetIsObjectValid(area), $"{AreaResref} is loaded");

            var nativeArea = NWNXLib.g_pAppManager.m_pServerExoApp.GetAreaByGameObjectID(area);
            var width = (int)(GetAreaSize(Dimension.Width, area) * 10 / CellMeters);
            var height = (int)(GetAreaSize(Dimension.Height, area) * 10 / CellMeters);
            var walk = new bool[width, height];
            var z = new float[width, height];
            var walkableMaterials = new Dictionary<int, bool>();
            for (var x = 0; x < width; x++)
            {
                for (var y = 0; y < height; y++)
                {
                    var location = Location(area, Vector3(x * CellMeters, y * CellMeters), 0f);
                    var material = GetSurfaceMaterial(location);
                    if (!walkableMaterials.TryGetValue(material, out var isWalkable))
                        walkableMaterials[material] = isWalkable = Get2DAString("surfacemat", "Walk", material) == "1";
                    walk[x, y] = isWalkable;
                    z[x, y] = GetGroundHeight(location);
                }

                if (x % RowsPerFrame == 0)
                    await ctx.WaitFrameAsync();
            }

            var seeds = new List<Vector3>();
            for (var obj = GetFirstObjectInArea(area); GetIsObjectValid(obj); obj = GetNextObjectInArea(area))
            {
                if (GetObjectType(obj) == ObjectType.Waypoint && GetResRef(obj) == "nw_waypoint001")
                    seeds.Add(GetPosition(obj));
            }
            ctx.Assert(seeds.Count >= 10, $"{AreaResref} has its arrival waypoints ({seeds.Count})");

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

            var steps = 0;
            while (queue.Count > 0)
            {
                var (cx, cy) = queue.Dequeue();
                foreach (var (nx, ny) in new[] { (cx + 1, cy), (cx - 1, cy), (cx, cy + 1), (cx, cy - 1) })
                {
                    if (nx < 0 || ny < 0 || nx >= width || ny >= height || reached[nx, ny] || !walk[nx, ny] ||
                        Math.Abs(z[nx, ny] - z[cx, cy]) > MaxRisePerCell)
                        continue;

                    // The same collision test the spawn bake uses: a placeable walkmesh or tile wall across the step blocks it.
                    var low = Math.Min(z[cx, cy], z[nx, ny]);
                    var high = Math.Max(z[cx, cy], z[nx, ny]);
                    if (nativeArea.NoNonWalkPolys(cx * CellMeters, cy * CellMeters, nx * CellMeters, ny * CellMeters,
                            low - 1f, high + 2f, 0f) == 0)
                        continue;

                    reached[nx, ny] = true;
                    queue.Enqueue((nx, ny));
                }

                if (++steps % 20000 == 0)
                    await ctx.WaitFrameAsync();
            }

            var points = GetBakedPoints()[AreaResref];
            ctx.Assert(points.Count > 1000, $"{AreaResref} keeps a large spawn pool ({points.Count} points)");
            var unreachable = points
                .Where(p =>
                {
                    var (x, y) = ((int)MathF.Round(p.X / CellMeters), (int)MathF.Round(p.Y / CellMeters));
                    return !reached[x, y];
                })
                .ToList();
            ctx.Log($"{points.Count} baked points, {unreachable.Count} unreachable: " +
                    string.Join(", ", unreachable.Select(p => $"({p.X:0},{p.Y:0})")));
            ctx.Assert(unreachable.Count == 0,
                $"{AreaResref} has {unreachable.Count} random spawn points players cannot reach, e.g. " +
                string.Join(", ", unreachable.Take(10).Select(p => $"({p.X:0},{p.Y:0})")));
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

        private static (int X, int Y) NearestWalkable(bool[,] walk, float[,] z, int width, int height, Vector3 position)
        {
            var best = (X: -1, Y: -1);
            var bestDistance = float.MaxValue;
            var radius = (int)(AnchorSearchMeters / CellMeters);
            var px = (int)MathF.Round(position.X / CellMeters);
            var py = (int)MathF.Round(position.Y / CellMeters);
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
