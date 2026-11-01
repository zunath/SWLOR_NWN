using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using SWLOR.Game.Server.Core.Async;
using SWLOR.Game.Server.EngineTests.Framework;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.NWN.API.NWScript.Enum.Area;
using Vector3 = System.Numerics.Vector3;

namespace SWLOR.Game.Server.EngineTests.Definitions
{
    public static class CapstoneTierSpawnDepthEngineTests
    {
        // Half-metre cells; a step may rise this much, so ramps and stairs pass and ledges do not.
        private const float CellMeters = 0.5f;
        private const float MaxRisePerCell = 0.4f;
        private const float AnchorSearchMeters = 10f;
        private const float LinkReachMeters = 3f;

        // Each dungeon's arrival waypoint from its hub and its adept spawn table. Specialists use the same
        // table ID with a _DEEP suffix.
        private static readonly (string Resref, string Entry, string AdeptTable)[] Dungeons =
        {
            ("pw_sc_velesannx", "VELES_ANNEX_ENTRY", "CAPSTONE_VELES_MILITIA_ANNEX"),
            ("pw_sc_jedihalls", "pw_sc_jedihalls_entry", "CAPSTONE_DANTOOINE_JEDI_ENCLAVE_TRIAL_HALLS"),
            ("pw_sc_forgecav", "pw_sc_forgecav_entry", "CAPSTONE_KORRIBAN_FORGE_CAVERNS"),
            ("pw_sc_emfbackr", "SMUG_BACKROOMS", "CAPSTONE_SMUGGLERS_MOON_FIGHT_CLUB_BACKROOMS"),
            ("pw_sc_canyonrng", "pw_sc_canyonrng_entry", "CAPSTONE_ANCHORHEAD_CANYON_RANGE"),
            ("pw_ar_czarmrange", "SMUG_CZERKA_WEAPONS_TEST_ENT", "CAPSTONE_CZERKA_ARMS_TEST_RANGE"),
            ("pw_sc_qiontest", "pw_sc_qiontest_entry", "CAPSTONE_HUTLAR_QION_TEST_SITE"),
            ("pw_sc_cryptdeep", "pw_sc_cryptdeep_entry", "CAPSTONE_KORRIBAN_SITH_CRYPT_DEPTHS"),
            ("pw_sc_repubeng", "pw_sc_repubeng_entry", "CAPSTONE_VISCARA_REPUBLIC_ENGINEERING_BUNKER"),
            ("pw_sc_dantmedsub", "to_medsublevel", "CAPSTONE_DANTOOINE_MEDICAL_SUBLEVEL"),
            ("pw_sc_tarnpres", "pw_sc_tarnpres_entry", "CAPSTONE_DATHOMIR_TARN_JUNGLE_PRESERVE"),
            ("cz220shipbreakin", "CZ220_MAINT_ENTRANCE_BREAKER", "CAPSTONE_CZ220_BREAKER_YARD"),
            ("pw_sc_dath_apexd", "DATH_APEX_DEN_INSIDE", "CAPSTONE_DATHOMIR_GROTTO_APEX_DEN"),
        };

        /// <summary>
        /// Walks each capstone dungeon's live walkmesh from the hub arrival point (following in-area doors and
        /// teleporters) and checks every adept spawn point is reached before any specialist spawn point, so the
        /// easier tier holds the part of the dungeon nearest the entrance.
        /// </summary>
        [EngineTest("Capstone adept spawn points are nearer the entrance than specialist spawn points", Category = "SpawnReachability", TimeoutSeconds = 300f)]
        public static async Task AdeptSpawnPointsAreNearerTheEntrance(EngineTestContext ctx)
        {
            foreach (var (resref, entryTag, adeptTable) in Dungeons)
            {
                await NwTask.NextFrame();
                var area = FindArea(resref);
                ctx.Assert(GetIsObjectValid(area), $"{resref} is loaded");

                var entry = OBJECT_INVALID;
                var adepts = new List<Vector3>();
                var specialists = new List<Vector3>();
                var links = new List<(Vector3 From, Vector3 To)>();
                for (var obj = GetFirstObjectInArea(area); GetIsObjectValid(obj); obj = GetNextObjectInArea(area))
                {
                    var type = GetObjectType(obj);
                    var tag = GetTag(obj);
                    if (type == ObjectType.Waypoint)
                    {
                        if (tag == entryTag) entry = obj;
                        else if (tag == adeptTable) adepts.Add(GetPosition(obj));
                        else if (tag == adeptTable + "_DEEP") specialists.Add(GetPosition(obj));
                    }
                    else if (type == ObjectType.Door || type == ObjectType.Trigger)
                    {
                        var target = GetTransitionTarget(obj);
                        if (GetIsObjectValid(target) && GetArea(target) == area)
                            links.Add((GetPosition(obj), GetPosition(target)));
                    }
                    else if (type == ObjectType.Placeable && GetLocalString(obj, "DESTINATION") != "")
                    {
                        var target = GetWaypointByTag(GetLocalString(obj, "DESTINATION"));
                        if (GetIsObjectValid(target) && GetArea(target) == area)
                            links.Add((GetPosition(obj), GetPosition(target)));
                    }
                }

                ctx.Assert(GetIsObjectValid(entry), $"{resref} has its arrival waypoint {entryTag}");
                ctx.Assert(adepts.Count > 0 && specialists.Count > 0, $"{resref} has adept ({adepts.Count}) and specialist ({specialists.Count}) spawn points");

                var distance = WalkDistances(area, GetPosition(entry), links, out var walk, out var width, out var height);
                float Walk(Vector3 point)
                {
                    var (x, y) = NearestWalkable(walk, width, height, point);
                    return x < 0 || distance[x, y] == int.MaxValue ? -1f : distance[x, y] * CellMeters;
                }

                var adeptWalks = adepts.Select(point => (point, Walk(point))).ToArray();
                var specialistWalks = specialists.Select(point => (point, Walk(point))).ToArray();
                foreach (var (point, walked) in adeptWalks.Concat(specialistWalks))
                    ctx.Assert(walked >= 0, $"{resref} spawn point ({point.X:0.0},{point.Y:0.0}) cannot be walked to from the entrance");

                var farthestAdept = adeptWalks.OrderByDescending(walked => walked.Item2).First();
                var nearestSpecialist = specialistWalks.OrderBy(walked => walked.Item2).First();
                ctx.Assert(farthestAdept.Item2 <= nearestSpecialist.Item2,
                    $"{resref} adept point ({farthestAdept.point.X:0.0},{farthestAdept.point.Y:0.0}) is {farthestAdept.Item2:0} m from the entrance, " +
                    $"farther than specialist point ({nearestSpecialist.point.X:0.0},{nearestSpecialist.point.Y:0.0}) at {nearestSpecialist.Item2:0} m");
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

        private static int[,] WalkDistances(uint area, Vector3 start, List<(Vector3 From, Vector3 To)> links,
            out bool[,] walk, out int width, out int height)
        {
            width = (int)(GetAreaSize(Dimension.Width, area) * 10 / CellMeters);
            height = (int)(GetAreaSize(Dimension.Height, area) * 10 / CellMeters);
            walk = new bool[width, height];
            var z = new float[width, height];
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

            var distance = new int[width, height];
            for (var x = 0; x < width; x++)
            for (var y = 0; y < height; y++)
                distance[x, y] = int.MaxValue;

            var queue = new Queue<(int X, int Y)>();
            var walkGrid = walk;
            var gridWidth = width;
            var gridHeight = height;
            void Seed(Vector3 position, int steps)
            {
                var (x, y) = NearestWalkable(walkGrid, gridWidth, gridHeight, position);
                if (x < 0 || distance[x, y] <= steps)
                    return;
                distance[x, y] = steps;
                queue.Enqueue((x, y));
            }

            // Breadth-first from the entrance; reaching a door or teleporter continues from its destination.
            Seed(start, 0);
            var usedLinks = new HashSet<int>();
            while (queue.Count > 0)
            {
                var (cx, cy) = queue.Dequeue();
                var steps = distance[cx, cy];
                for (var i = 0; i < links.Count; i++)
                {
                    var from = links[i].From;
                    if (usedLinks.Contains(i) || Math.Abs(from.X - cx * CellMeters) > LinkReachMeters ||
                        Math.Abs(from.Y - cy * CellMeters) > LinkReachMeters)
                        continue;
                    usedLinks.Add(i);
                    Seed(links[i].To, steps + 1);
                }

                foreach (var (nx, ny) in new[] { (cx + 1, cy), (cx - 1, cy), (cx, cy + 1), (cx, cy - 1) })
                {
                    if (nx < 0 || ny < 0 || nx >= width || ny >= height || !walk[nx, ny] || distance[nx, ny] <= steps + 1 ||
                        Math.Abs(z[nx, ny] - z[cx, cy]) > MaxRisePerCell)
                        continue;
                    distance[nx, ny] = steps + 1;
                    queue.Enqueue((nx, ny));
                }
            }

            return distance;
        }

        private static (int X, int Y) NearestWalkable(bool[,] walk, int width, int height, Vector3 position)
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
                var distance = (x - px) * (x - px) + (y - py) * (y - py);
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
