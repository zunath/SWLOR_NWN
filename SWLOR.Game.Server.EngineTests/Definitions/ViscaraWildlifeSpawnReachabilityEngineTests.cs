using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using SWLOR.Game.Server.EngineTests.Framework;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.NWN.API.NWScript.Enum.Area;
using Vector3 = System.Numerics.Vector3;

namespace SWLOR.Game.Server.EngineTests.Definitions
{
    public static class ViscaraWildlifeSpawnReachabilityEngineTests
    {
        // A 1m step may rise this much: walkable slopes pass, cliff faces and drops into cut-off water do not.
        private const float MaxRisePerMeter = 0.75f;
        private const float AnchorSearchMeters = 10f;

        /// <summary>
        /// Floods the live walkmesh from each area transition and checks every baked random-spawn point
        /// is reached, so wildlife never spawns where players can see it but cannot walk to it.
        /// </summary>
        [EngineTest("Viscara wildlife spawn points are reachable from the area entrances", Category = "SpawnReachability", TimeoutSeconds = 60f)]
        public static async Task WildlifeSpawnPointsAreReachable(EngineTestContext ctx)
        {
            await Task.CompletedTask;
            var bakedByArea = (Dictionary<string, List<Vector3>>)typeof(Walkmesh)
                .GetField("_walkmeshesByArea", BindingFlags.NonPublic | BindingFlags.Static).GetValue(null);

            foreach (var resref in new[] { "viscarawildlands", "viscaralake", "viscaradeepmount" })
            {
                var area = FindArea(resref);
                ctx.Assert(GetIsObjectValid(area), $"{resref} is loaded");
                var points = bakedByArea[resref];
                ctx.Assert(points.Count > 0, $"{resref} has baked spawn points");

                var reached = FloodFromTransitions(area, out var width, out var height);
                var stranded = points
                    .Where(p => !reached[Math.Min(width - 1, (int)p.X), Math.Min(height - 1, (int)p.Y)])
                    .ToList();
                ctx.Assert(stranded.Count == 0,
                    $"{resref} has {stranded.Count} spawn points players cannot walk to, e.g. " +
                    string.Join(", ", stranded.Take(5).Select(p => $"({p.X:0},{p.Y:0},{p.Z:0.0})")));
            }

            // The plateau beside the entrance can only be climbed from the far side of the map.
            var plateau = bakedByArea["viscaradeepmount"]
                .Where(p => p.X >= 176 && (p.Y >= 152 || (p.Y >= 146 && p.Z >= 5f)))
                .ToList();
            ctx.Assert(plateau.Count == 0,
                $"Deep Mountains has {plateau.Count} spawn points on the plateau beside the entrance");
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

        private static bool[,] FloodFromTransitions(uint area, out int width, out int height)
        {
            width = GetAreaSize(Dimension.Width, area) * 10;
            height = GetAreaSize(Dimension.Height, area) * 10;
            var walk = new bool[width, height];
            var z = new float[width, height];
            var walkableMaterials = new Dictionary<int, bool>();
            for (var x = 0; x < width; x++)
            for (var y = 0; y < height; y++)
            {
                var location = Location(area, Vector3(x, y), 0f);
                var material = GetSurfaceMaterial(location);
                if (!walkableMaterials.TryGetValue(material, out var isWalkable))
                    walkableMaterials[material] = isWalkable = Get2DAString("surfacemat", "Walk", material) == "1";
                walk[x, y] = isWalkable;
                z[x, y] = GetGroundHeight(location);
            }

            var reached = new bool[width, height];
            var queue = new Queue<(int X, int Y)>();
            for (var obj = GetFirstObjectInArea(area); GetIsObjectValid(obj); obj = GetNextObjectInArea(area))
            {
                var type = GetObjectType(obj);
                if ((type != ObjectType.Door && type != ObjectType.Trigger) || !GetIsObjectValid(GetTransitionTarget(obj)))
                    continue;

                var anchor = NearestWalkable(walk, z, width, height, GetPosition(obj));
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
                        Math.Abs(z[nx, ny] - z[cx, cy]) > MaxRisePerMeter)
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
            var radius = (int)AnchorSearchMeters;
            for (var x = Math.Max(0, (int)position.X - radius); x < Math.Min(width, (int)position.X + radius); x++)
            for (var y = Math.Max(0, (int)position.Y - radius); y < Math.Min(height, (int)position.Y + radius); y++)
            {
                if (!walk[x, y])
                    continue;
                var distance = Vector3.DistanceSquared(new Vector3(x, y, z[x, y]), position);
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
