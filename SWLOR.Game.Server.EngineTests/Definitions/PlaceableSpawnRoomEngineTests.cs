using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using SWLOR.Game.Server.Composition;
using SWLOR.Game.Server.EngineTests.Framework;
using SWLOR.Game.Server.Domain.Foundation;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.NWN.API.NWScript.Enum.Area;
using Vector3 = System.Numerics.Vector3;

namespace SWLOR.Game.Server.EngineTests.Definitions
{
    public static class PlaceableSpawnRoomEngineTests
    {
        private const string AreaResref = "pw_sc_esvein";
        private const string NodeResref = "keromber_vein";
        private const string WalkerResref = "civilian";
        // Where the Discord report placed the blocking rock.
        private static readonly Vector3 ReportedPosition = new(26.2f, 54.3f, 0f);
        private const float ReportRadius = 10f;
        // The reported tunnel is a narrow bridge with drops on both sides, east-west along Y 54 from X 18 to 30 on the
        // bake lattice; east of it the bridge widens into a slab.
        private const float TunnelMinX = 18f;
        private const float TunnelMaxX = 30f;
        private const float TunnelMinY = 52f;
        private const float TunnelMaxY = 56f;
        // Open cave floor where a walker must get around a vein, or the walk check proves nothing.
        private static readonly Vector3 OpenGroundPoint = new(75f, 55f, 0f);
        private const int MaximumAcceptedSamples = 60;
        private const float BatchSpacing = 14f;
        private const float ArrivalMeters = 1.0f;
        private const float WalkSeconds = 15f;

        /// <summary>
        /// The reported tunnel keeps no placeable spawn point. Then a real vein is created at sampled placeable points
        /// in the Shattervein and a civilian walks from one side of it to the other: every accepted point must leave a
        /// way around the vein. A vein in open ground must be walked around first, or the check proves nothing, and
        /// veins on refused points in the reported tunnel must block it.
        /// </summary>
        [EngineTest("Shattervein resource nodes leave a way around them", Category = "SpawnReachability", TimeoutSeconds = 900f)]
        public static async Task ShatterveinNodesLeaveAWayAround(EngineTestContext ctx)
        {
            var area = FindArea(AreaResref);
            ctx.Assert(GetIsObjectValid(area), $"{AreaResref} is loaded");
            var baked = GetBakedPoints()[AreaResref];
            var accepted = GameServices.Walkmesh.GetPlaceableSpawnPoints(area);
            var bakedSet = baked.ToHashSet();
            ctx.Assert(accepted.All(bakedSet.Contains), "Every placeable spawn point is a baked spawn point");
            ctx.Assert(accepted.Count >= 200, $"{AreaResref} keeps a usable placeable pool ({accepted.Count} of {baked.Count} points)");

            var acceptedSet = accepted.ToHashSet();
            var refusedNearReport = baked
                .Where(p => !acceptedSet.Contains(p) && Distance2D(p, ReportedPosition) <= ReportRadius)
                .ToList();
            ctx.Log($"{AreaResref}: {baked.Count} baked points, {accepted.Count} placeable points. Refused within " +
                    $"{ReportRadius} m of the report: " + string.Join(", ", refusedNearReport.Select(Format)));
            var inTunnel = baked.Where(IsInReportedTunnel).ToList();
            ctx.Assert(inTunnel.Count > 0, "The reported tunnel holds baked spawn points");
            ctx.Assert(!accepted.Any(IsInReportedTunnel),
                "No placeable spawn point is left in the reported tunnel: " + string.Join(", ", accepted.Where(IsInReportedTunnel).Select(Format)));

            // The real area: nothing spawns there until a player enters. Walkers in an area without players run at the
            // lowest AI level, so each walker's AI level is raised.
            var areaId = area;
            var control = await WalkAroundNodesAsync(ctx, areaId, new List<Vector3> { OpenGroundPoint });
            ctx.Assert(control.Count > 0 && control.All(r => r.Arrived),
                "A walker gets around a vein in open ground, so the walk check is valid: " + string.Join("; ", control.Select(r => r.Describe())));

            var stride = Math.Max(1, accepted.Count / MaximumAcceptedSamples);
            var samples = accepted.Where((_, index) => index % stride == 0).ToList();
            var acceptedResults = await WalkAroundNodesAsync(ctx, areaId, samples);
            var controlResults = await WalkAroundNodesAsync(ctx, areaId, inTunnel);

            var acceptedBlocked = acceptedResults.Where(r => !r.Arrived).ToList();
            var controlBlocked = controlResults.Where(r => !r.Arrived).ToList();
            ctx.Log($"Accepted points: {acceptedResults.Count} walks around {samples.Count} veins, {acceptedBlocked.Count} blocked.");
            ctx.Log($"Refused points in the reported tunnel: {controlResults.Count} walks, {controlBlocked.Count} blocked: " +
                    string.Join("; ", controlBlocked.Select(r => r.Describe())));

            ctx.Assert(acceptedBlocked.Count == 0,
                "Walks blocked by a vein on an accepted point: " + string.Join("; ", acceptedBlocked.Take(15).Select(r => r.Describe())));
            ctx.Assert(controlBlocked.Count > 0, "A vein on a refused point in the reported tunnel blocks it");
            ctx.SetResultDetail($"{accepted.Count} of {baked.Count} Shattervein points accept a resource node, none in the reported tunnel; " +
                                $"{acceptedResults.Count} walks around veins on {samples.Count} sampled accepted points all arrived; " +
                                $"{controlBlocked.Count} of {controlResults.Count} walks past veins on refused tunnel points were blocked.");
        }

        /// <summary>
        /// The startup bake leaves placeable points in every area with a random spawn table; an area without them
        /// would fall back to points where a node can block a passage. Logs each area's pool for review.
        /// </summary>
        [EngineTest("Areas with random placeable spawns keep placeable points", Category = "SpawnReachability", TimeoutSeconds = 300f)]
        public static async Task AreasKeepPlaceablePoints(EngineTestContext ctx)
        {
            await Task.CompletedTask;
            var baked = GetBakedPoints();
            var lines = new List<string>();
            var empty = new List<string>();
            for (var area = GetFirstArea(); GetIsObjectValid(area); area = GetNextArea())
            {
                if (!SpawnTableVariables.Any(variable => !string.IsNullOrWhiteSpace(GetLocalString(area, variable))))
                    continue;

                var resref = GetResRef(area);
                var points = GameServices.Walkmesh.GetPlaceableSpawnPoints(area);
                var all = baked.TryGetValue(resref, out var bakedPoints) ? bakedPoints.Count : 0;
                lines.Add($"{resref} {all} -> {points.Count}");
                if (points.Count == 0 && all > 0)
                    empty.Add(resref);
            }

            ctx.Log($"Placeable spawn pools ({lines.Count} areas, baked -> placeable): " + string.Join(", ", lines));
            ctx.Assert(lines.Count > 0, "Some areas have random spawn tables");
            ctx.Assert(empty.Count == 0, "Areas left without placeable points: " + string.Join(", ", empty));
        }

        private static readonly string[] SpawnTableVariables =
        {
            "RESOURCE_SPAWN_TABLE_ID", "SLICING_TERMINAL_SPAWN_TABLE_ID", "CREATURE_SPAWN_TABLE_ID",
        };

        private sealed record WalkResult(Vector3 Node, Vector3 From, Vector3 To, Vector3 End, bool Arrived)
        {
            public string Describe() =>
                $"vein {Format(Node)}: {Format(From)} -> {Format(To)} stopped at ({End.X:0.0},{End.Y:0.0})";
        }

        private sealed record Walk(Vector3 Node, Vector3 From, Vector3 To);

        private static async Task<List<WalkResult>> WalkAroundNodesAsync(
            EngineTestContext ctx, uint areaId, List<Vector3> points)
        {
            var results = new List<WalkResult>();
            var areaWidth = GetAreaSize(Dimension.Width, areaId) * 10f;
            var areaHeight = GetAreaSize(Dimension.Height, areaId) * 10f;
            var grid = new WalkableGrid(areaWidth, areaHeight, (x, y) =>
            {
                var location = Location(areaId, new Vector3(x, y, 0f), 0f);
                return (Get2DAString("surfacemat", "Walk", GetSurfaceMaterial(location)) == "1", GetGroundHeight(location));
            });
            var walksByPoint = points.ToDictionary(p => p, p => PickWalks(grid, p));
            var remaining = points.Where(p => walksByPoint[p].Count > 0).ToList();
            while (remaining.Count > 0)
            {
                // Veins far enough apart that one walker never meets another vein.
                var batch = new List<Vector3>();
                foreach (var point in remaining)
                {
                    if (batch.All(other => Distance2D(other, point) >= BatchSpacing))
                        batch.Add(point);
                }
                remaining = remaining.Except(batch).ToList();

                var nodes = new List<uint>();
                foreach (var point in batch)
                {
                    // 45 degrees turns the square walkmesh's corners onto the axes, its widest reach along them.
                    var node = CreateObject(ObjectType.Placeable, NodeResref, Location(areaId, point, 45f));
                    ctx.Track(node);
                    nodes.Add(node);
                }

                for (var round = 0; round < 2; round++)
                {
                    var walks = batch.Where(p => walksByPoint[p].Count > round).Select(p => walksByPoint[p][round]).ToList();
                    if (walks.Count == 0)
                        break;

                    var walkers = new List<uint>();
                    foreach (var walk in walks)
                    {
                        var walker = CreateObject(ObjectType.Creature, WalkerResref, Location(areaId, walk.From, 0f));
                        ctx.Track(walker);
                        SetAILevel(walker, AILevel.High);
                        walkers.Add(walker);
                    }
                    await ctx.WaitFrameAsync();

                    for (var index = 0; index < walks.Count; index++)
                    {
                        var destination = Location(areaId, walks[index].To, 0f);
                        await ctx.ExecuteInCreatureContextAsync(walkers[index], () =>
                        {
                            ClearAllActions();
                            ActionMoveToLocation(destination, true);
                        });
                    }

                    bool Arrived(int index) =>
                        Distance2D(GetPosition(walkers[index]), walks[index].To) <= ArrivalMeters;
                    var deadline = WalkSeconds;
                    while (deadline > 0f && !Enumerable.Range(0, walks.Count).All(Arrived))
                    {
                        await ctx.DelaySecondsAsync(0.5f);
                        deadline -= 0.5f;
                    }

                    for (var index = 0; index < walks.Count; index++)
                    {
                        results.Add(new WalkResult(walks[index].Node, walks[index].From, walks[index].To,
                            GetPosition(walkers[index]), Arrived(index)));
                        DestroyObject(walkers[index]);
                    }
                    await ctx.WaitFrameAsync();
                }

                foreach (var node in nodes)
                    DestroyObject(node);
                await ctx.WaitFrameAsync();
            }

            return results;
        }

        /// <summary>
        /// Picks up to two walks straight across the point, between opposite ring points on walkable ground that a
        /// creature reaches from the point before the vein exists. The vein sits on the straight line, so the walker has to go around it.
        /// </summary>
        private static List<Walk> PickWalks(WalkableGrid grid, Vector3 point)
        {
            const int Pairs = PlaceableSpawnRoom.RingPoints / 2;
            var radius = PlaceableSpawnRoom.RingRadius;
            bool IsClear(Vector3 from, Vector3 to) => grid.IsLineWalkable(from.X, from.Y, to.X, to.Y);
            Vector3 Ring(int index)
            {
                var angle = index * 2f * MathF.PI / PlaceableSpawnRoom.RingPoints;
                return new Vector3(point.X + radius * MathF.Cos(angle), point.Y + radius * MathF.Sin(angle), point.Z);
            }

            var eligible = Enumerable.Range(0, Pairs)
                .Where(index => IsClear(point, Ring(index)) && IsClear(point, Ring(index + Pairs)))
                .ToList();
            var walks = new List<Walk>();
            if (eligible.Count == 0)
                return walks;

            walks.Add(new Walk(point, Ring(eligible[0]), Ring(eligible[0] + Pairs)));
            var crossing = eligible.FirstOrDefault(index => index - eligible[0] >= Pairs / 2, -1);
            if (crossing >= 0)
                walks.Add(new Walk(point, Ring(crossing), Ring(crossing + Pairs)));
            return walks;
        }

        private static Dictionary<string, List<Vector3>> GetBakedPoints() => WalkmeshBakes.ByArea();

        private static uint FindArea(string resref)
        {
            for (var area = GetFirstArea(); GetIsObjectValid(area); area = GetNextArea())
            {
                if (GetResRef(area) == resref)
                    return area;
            }

            return OBJECT_INVALID;
        }

        private static bool IsInReportedTunnel(Vector3 p) =>
            p.X >= TunnelMinX && p.X <= TunnelMaxX && p.Y >= TunnelMinY && p.Y <= TunnelMaxY;

        private static float Distance2D(Vector3 a, Vector3 b) => MathF.Sqrt((a.X - b.X) * (a.X - b.X) + (a.Y - b.Y) * (a.Y - b.Y));

        private static string Format(Vector3 p) => $"({p.X:0.#},{p.Y:0.#})";
    }
}
