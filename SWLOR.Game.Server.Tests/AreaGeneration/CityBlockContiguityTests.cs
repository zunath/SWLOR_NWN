using System;
using System.Collections.Generic;
using System.Linq;
using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Feature.DungeonDefinition;
using SWLOR.Game.Server.Service.AreaGenerationService;

namespace SWLOR.Game.Server.Tests.AreaGeneration;

/// <summary>
/// Street-canyon structure gates for the contiguous-building-block mechanism (see
/// DungeonTilesetProfile.BuildingBlockContiguity, LayoutGroupStamper.IsOpenSetPieceSiteValid's
/// stamped-seam acceptance + seam verification + block-size cap + room-split protection, and
/// LayoutParameterConstraints.ScaledRoomEnvelope's canyon growth origin).
///
/// Reference bands are the hand-built fcx01 promenade family's TILE-BUILT subset (ns_comrcial_ka,
/// pw_ar_nsshipyard, vrotrnsslums, narshadaar_promi -- the flagship pw_ar_narpromena assembles its
/// canyon from skyscraper PLACEABLES on flat cobble and measures 0 building tiles), measured by
/// _scratch_decor/promenade_benchmark.py (July 2026 street-canyon pass):
///   building-tile share        0.170 - 0.284   (mean 0.215)
///   largest contiguous block   24 - 48 tiles   (largest single fcx01 group footprint is 36)
///   mean block size            13.5 - 30
///   open-tile distance to mass: within-2-steps fraction 0.18 - 0.90, mean distance 1.4 - 5.6
///
/// Generated futcity/packed at 32x32 measured (same harness, seeds 5001-5010): share 0.230-0.275,
/// largest block 45-48, mean block 19.6-28.2, within-2 0.70-0.84, mean distance 1.7-2.2. Before the
/// mechanism the same seeds measured share 0.095-0.216 with blocks capped at single-group footprints
/// (largest 36, mean block 7.2-14.7) -- isolated towers on an open field, not canyon walls.
///
/// Thresholds sit between the measured after-values and both the hand-built band edges and the
/// before-values, so ordinary drift never flakes while a regression to isolated-tower stamping (or
/// runaway wall-to-wall mass) fails clearly. All inputs are fixed seeds -- runs are deterministic.
/// </summary>
public class CityBlockContiguityTests
{
    private sealed class AreaStructure
    {
        public int Seed;
        public double BuildingShare;
        public int LargestBlock;
        public double MeanBlock;
        public double OpenWithin2;
        public double MeanOpenDistance;
    }

    private static List<AreaStructure> Measure(string tilesetKey, string layoutKey, int size, int seedBase, int seedCount)
    {
        var tilesetProfile = new BaseGameTilesetProfiles().BuildTilesetProfiles()[tilesetKey];
        var layoutProfile = new StandardLayoutProfiles().BuildLayoutProfiles()[layoutKey];
        var model = TilesetTestSource.LoadTileset(tilesetProfile.TilesetResref);

        // Building tiles: members of multi-tile groups with a 4+ tile footprint -- same
        // classification as CityBlockDensityTests and the scratch promenade benchmark's Group bucket
        // (fcx01 has no 2-3 tile groups, so the two definitions coincide there).
        var buildingTileIds = new HashSet<int>();
        foreach (var group in model.Groups.Where(g => g.Rows * g.Columns >= 4))
        foreach (var tileId in group.TileIds.Where(t => t >= 0))
            buildingTileIds.Add(tileId);

        // Open tiles: road-crossed tiles plus doorless, crosser-free, uniformly open-cornered plain
        // tiles -- the same Road/PlainOpen buckets the benchmark's street-distance metric uses.
        var roadCrosser = tilesetProfile.RoadCrosser ?? string.Empty;
        var openTerrain = tilesetProfile.PrimaryOpenTerrain;

        bool IsOpenTile(int tileId)
        {
            if (tileId < 0 || tileId >= model.Tiles.Count || buildingTileIds.Contains(tileId)) return false;
            var tile = model.Tiles[tileId];
            if (roadCrosser.Length > 0 &&
                tile.Edges.Any(e => string.Equals(e, roadCrosser, StringComparison.OrdinalIgnoreCase)))
                return true;
            return tile.GroupIndex == -1 && !tile.HasAnyCrosser && tile.Doors.Count == 0 &&
                   tile.Corners.All(c => string.Equals(c, openTerrain, StringComparison.OrdinalIgnoreCase));
        }

        var results = new List<AreaStructure>();
        for (var i = 0; i < seedCount; i++)
        {
            var seed = seedBase + i;
            var composition = new DungeonComposition { Tileset = tilesetProfile, Layout = layoutProfile };
            var result = LayoutSolver.Solve(
                composition.BuildLayoutParameters(), model, size, size, seed, tilesetProfile.PrimaryOpenTerrain);

            result.Success.Should().BeTrue(
                $"{tilesetKey}/{layoutKey} seed {seed} must generate at {size}x{size}: {result.FailureReason}");

            var building = new HashSet<(int X, int Y)>();
            var open = new List<(int X, int Y)>();
            for (var y = 0; y < size; y++)
            for (var x = 0; x < size; x++)
            {
                var tileId = result.Resolved.Tiles[y * size + x].TileId;
                if (buildingTileIds.Contains(tileId)) building.Add((x, y));
                else if (IsOpenTile(tileId)) open.Add((x, y));
            }

            var blocks = BlockSizes(building);
            var distances = OpenDistances(building, open, size);

            results.Add(new AreaStructure
            {
                Seed = seed,
                BuildingShare = building.Count / (double)(size * size),
                LargestBlock = blocks.Count == 0 ? 0 : blocks.Max(),
                MeanBlock = blocks.Count == 0 ? 0 : blocks.Average(),
                OpenWithin2 = distances.Count == 0 ? 0 : distances.Count(d => d <= 2) / (double)distances.Count,
                MeanOpenDistance = distances.Count == 0 ? 0 : distances.Average()
            });
        }

        return results;
    }

    /// <summary>Orthogonally-connected component sizes over the building tile set.</summary>
    private static List<int> BlockSizes(HashSet<(int X, int Y)> building)
    {
        var sizes = new List<int>();
        var seen = new HashSet<(int X, int Y)>();
        foreach (var start in building)
        {
            if (!seen.Add(start)) continue;
            var count = 0;
            var queue = new Queue<(int X, int Y)>();
            queue.Enqueue(start);
            while (queue.Count > 0)
            {
                var (x, y) = queue.Dequeue();
                count++;
                foreach (var (nx, ny) in new[] { (x + 1, y), (x - 1, y), (x, y + 1), (x, y - 1) })
                {
                    if (building.Contains((nx, ny)) && seen.Add((nx, ny)))
                        queue.Enqueue((nx, ny));
                }
            }

            sizes.Add(count);
        }

        return sizes;
    }

    /// <summary>Multi-source BFS distance (orthogonal steps) from building mass, reported per open tile.</summary>
    private static List<int> OpenDistances(HashSet<(int X, int Y)> building, List<(int X, int Y)> open, int size)
    {
        if (building.Count == 0) return new List<int>();

        var dist = new Dictionary<(int X, int Y), int>();
        var queue = new Queue<(int X, int Y)>();
        foreach (var cell in building)
        {
            dist[cell] = 0;
            queue.Enqueue(cell);
        }

        while (queue.Count > 0)
        {
            var (x, y) = queue.Dequeue();
            var d = dist[(x, y)];
            foreach (var next in new[] { (x + 1, y), (x - 1, y), (x, y + 1), (x, y - 1) })
            {
                if (next.Item1 < 0 || next.Item1 >= size || next.Item2 < 0 || next.Item2 >= size) continue;
                if (dist.ContainsKey(next)) continue;
                dist[next] = d + 1;
                queue.Enqueue(next);
            }
        }

        return open.Select(c => dist.TryGetValue(c, out var d) ? d : size).ToList();
    }

    [Test]
    public void FutCityPacked_At32_FormsStreetCanyonBlocksInHandBuiltBands()
    {
        var areas = Measure(BaseGameTilesetProfiles.FutCity, StandardLayoutProfiles.Packed, 32, 5001, 10);

        foreach (var area in areas)
        {
            // Hand-built band 0.170-0.284; measured 0.230-0.275 across these exact seeds. The lower
            // gate also clears the pre-mechanism mean (0.145) by a wide margin.
            area.BuildingShare.Should().BeInRange(0.17, 0.30,
                $"seed {area.Seed}: building-mass share must stay inside the hand-built promenade band");

            // A block above 36 tiles is impossible without cross-group adjacency (largest single
            // fcx01 group is Tower07's 6x6), so this gate proves contiguity on EVERY seed; 48 is the
            // mechanism's own hand-built-derived hard cap (LayoutGroupStamper.MaxContiguousBlockTiles).
            area.LargestBlock.Should().BeInRange(40, 48,
                $"seed {area.Seed}: largest contiguous building block must exceed any single group footprint and respect the hand-built cap");

            // Hand-built band 13.5-30; pre-mechanism measured 7.2-14.7.
            area.MeanBlock.Should().BeInRange(15, 30,
                $"seed {area.Seed}: mean block size must reflect adjoined building groups");

            // Canyon street structure: most open tiles hug the building mass. Hand-built band
            // 0.18-0.90 (within-2) and 1.4-5.6 (mean distance); measured 0.70-0.84 and 1.7-2.2.
            area.OpenWithin2.Should().BeGreaterThan(0.5,
                $"seed {area.Seed}: open tiles must mostly sit within 2 tiles of building mass");
            area.MeanOpenDistance.Should().BeLessThan(4.0,
                $"seed {area.Seed}: streets must read as canyons, not open fields");
        }

        // Aggregate mean pinned to the hand-built band's interior (hand-built mean 0.215).
        areas.Average(a => a.BuildingShare).Should().BeInRange(0.20, 0.28,
            "10-seed mean building share must sit inside the hand-built promenade band");
    }

    [Test]
    public void FutCityPacked_At24_ReachesHandBuiltMeanDensityWithBlocks()
    {
        var areas = Measure(BaseGameTilesetProfiles.FutCity, StandardLayoutProfiles.Packed, 24, 6001, 10);

        // Measured mean 0.215 (the hand-built band mean exactly), seeds 0.115-0.306 -- variance is
        // wider at 24 (fewer rooms per area), so the per-seed gate is a floor rather than a band.
        areas.Average(a => a.BuildingShare).Should().BeInRange(0.15, 0.28,
            "10-seed mean building share at 24x24 must sit inside the hand-built promenade band");
        foreach (var area in areas)
        {
            area.BuildingShare.Should().BeGreaterThan(0.10,
                $"seed {area.Seed}: every 24x24 city area must carry substantial building mass");
            area.LargestBlock.Should().BeLessThanOrEqualTo(48,
                $"seed {area.Seed}: the contiguous-block cap must hold at every size");
        }

        // Contiguity must appear on most seeds at this size too (measured 9/10 with largest >= 24).
        areas.Count(a => a.LargestBlock >= 24).Should().BeGreaterThanOrEqualTo(7,
            "canyon blocks (24+ contiguous tiles) must appear across seeds at 24x24, not on lucky rolls");
    }
}
