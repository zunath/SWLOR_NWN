using System;
using System.Collections.Generic;
using System.Linq;
using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Feature.DungeonDefinition;
using SWLOR.Game.Server.Service.AreaGenerationService;
using SWLOR.Game.Server.Service.AreaGenerationService.Tileset;

namespace SWLOR.Game.Server.Tests.AreaGeneration;

/// <summary>
/// LayoutElevationPoolPainter invariants, exercised the same way LayoutElevationPainterTests covers
/// its sibling pass: only through the public MacroLayoutGenerator.Generate entry point. Covers
/// shape-check gating (no vocabulary => fully inert), back-compat (PoolRegions=0 leaves the grid
/// untouched), painting actually sinking a Lava-labelled patch and TileResolver still resolving the
/// result end to end, connectivity preservation, and determinism.
/// </summary>
public class LayoutElevationPoolPainterTests
{
    private const string Wall = "Wall";
    private const string Floor = "Floor";
    private const string Lava = "Lava";

    /// <summary>Same base fixture as LayoutElevationPainterTests (one flat tile per Wall/Floor corner
    /// combo) but with zero Lava/pool vocabulary at all -- proves the painter is inert.</summary>
    private static TilesetModel BuildFlatOnlyTileset()
    {
        var tileset = new TilesetModel
        {
            Resref = "tst-flat-pool",
            Name = "Flat-only synthetic tileset",
            Terrains = new List<string> { Wall, Floor },
            DefaultTerrain = Wall,
            FloorTerrain = Floor
        };

        var nextTileId = 0;
        for (var combo = 0; combo < 16; combo++)
        {
            var tl = (combo & 8) != 0 ? Floor : Wall;
            var tr = (combo & 4) != 0 ? Floor : Wall;
            var br = (combo & 2) != 0 ? Floor : Wall;
            var bl = (combo & 1) != 0 ? Floor : Wall;
            tileset.Tiles.Add(NewFlatTile(nextTileId++, tl, tr, br, bl));
        }

        return tileset;
    }

    /// <summary>
    /// Adds every tile LayoutElevationPoolPainter needs: the plain Floor rim (one-corner/two-adjacent
    /// raised, reused from LayoutElevationPainter's own mechanism), a fully-flat all-Lava tile, and the
    /// two pool boundary shapes (one Lava corner cut into a raised Floor rim; two ADJACENT Lava corners
    /// along a straight rim edge) in every rotation.
    /// </summary>
    private static TilesetModel BuildPoolCapableTileset()
    {
        var tileset = BuildFlatOnlyTileset();
        var nextTileId = tileset.Tiles.Count;

        // Plain Floor rim (LayoutElevationPainter's own OpenTerrain mechanism -- pools need the outer
        // raise to succeed first).
        tileset.Tiles.Add(NewRaisedTile(nextTileId++, new[] { Floor, Floor, Floor, Floor }, new[] { 0, 0, 1, 0 }));
        tileset.Tiles.Add(NewRaisedTile(nextTileId++, new[] { Floor, Floor, Floor, Floor }, new[] { 0, 1, 1, 0 }));

        // Fully-interior flat pool cell.
        tileset.Tiles.Add(NewRaisedTile(nextTileId++, new[] { Lava, Lava, Lava, Lava }, new[] { 0, 0, 0, 0 }));

        // Corner-cut boundary: one Lava corner (BL), three Floor corners at height 1.
        tileset.Tiles.Add(NewRaisedTile(nextTileId++, new[] { Floor, Floor, Floor, Lava }, new[] { 1, 1, 1, 0 }));

        // Edge-cut boundary: two ADJACENT Lava corners (BR, BL), two Floor corners at height 1.
        tileset.Tiles.Add(NewRaisedTile(nextTileId++, new[] { Floor, Floor, Lava, Lava }, new[] { 1, 1, 0, 0 }));

        return tileset;
    }

    private static TileRecord NewFlatTile(int tileId, string tl, string tr, string br, string bl)
    {
        return new TileRecord
        {
            TileId = tileId,
            Corners = new[] { tl, tr, br, bl },
            CornerHeights = new[] { 0, 0, 0, 0 },
            Edges = new[] { "", "", "", "" },
            PathNode = "A",
            GroupIndex = -1
        };
    }

    private static TileRecord NewRaisedTile(int tileId, string[] corners, int[] heights)
    {
        return new TileRecord
        {
            TileId = tileId,
            Corners = corners,
            CornerHeights = heights,
            Edges = new[] { "", "", "", "" },
            PathNode = "A",
            GroupIndex = -1
        };
    }

    private static MacroLayoutParameters BuildParameters(int poolRegions)
    {
        return new MacroLayoutParameters
        {
            Width = 28,
            Height = 28,
            SolidTerrain = Wall,
            OpenTerrain = Floor,
            Style = DungeonLayoutStyle.RoomsAndCorridors,
            CorridorMode = CorridorMode.Tunnel,
            MinRooms = 4,
            MaxRooms = 6,
            MinRoomCornerSize = 4,
            MaxRoomCornerSize = 6,
            EntranceCount = 1,
            ExitCount = 1,
            PoolTerrain = Lava,
            PoolRegions = poolRegions
        };
    }

    private static bool AnyLavaPainted(MacroLayout layout)
    {
        var corners = layout.Corners;
        for (var x = 0; x <= corners.Width; x++)
        for (var y = 0; y <= corners.Height; y++)
        {
            if (corners.Labels[x, y] == Lava) return true;
        }
        return false;
    }

    [Test]
    public void Paint_NoVocabulary_LeavesGridWithoutPoolTerrainAcrossManySeeds()
    {
        var tileset = BuildFlatOnlyTileset();
        var parameters = BuildParameters(poolRegions: 3);

        for (var seed = 0; seed < 15; seed++)
        {
            var layout = MacroLayoutGenerator.Generate(parameters, new Random(seed), tileset);
            AnyLavaPainted(layout).Should().BeFalse(
                $"seed {seed}: a tileset with zero pool vocabulary must leave the painter fully inert");
            layout.Corners.HasAnyHeight().Should().BeFalse($"seed {seed}: no vocabulary means no height either");
        }
    }

    [Test]
    public void Paint_NullTileset_LeavesGridUntouched()
    {
        var parameters = BuildParameters(poolRegions: 3);

        var layout = MacroLayoutGenerator.Generate(parameters, new Random(1));

        AnyLavaPainted(layout).Should().BeFalse("a null tileset means no shape verification is possible, so the pass must no-op");
    }

    [Test]
    public void Paint_ZeroRegionsRequested_LeavesGridUntouchedAcrossManySeeds()
    {
        var tileset = BuildPoolCapableTileset();
        var parameters = BuildParameters(poolRegions: 0);

        for (var seed = 0; seed < 15; seed++)
        {
            var layout = MacroLayoutGenerator.Generate(parameters, new Random(seed), tileset);
            AnyLavaPainted(layout).Should().BeFalse(
                $"seed {seed}: PoolRegions=0 is the default for every existing caller and must be byte-identical to the pre-pool legacy path");
        }
    }

    [Test]
    public void Paint_EmptyPoolTerrain_LeavesGridUntouchedEvenWithRegionsRequested()
    {
        var tileset = BuildPoolCapableTileset();
        var parameters = BuildParameters(poolRegions: 3);
        parameters.PoolTerrain = string.Empty;

        var layout = MacroLayoutGenerator.Generate(parameters, new Random(5), tileset);

        AnyLavaPainted(layout).Should().BeFalse("an empty PoolTerrain must disable the pass regardless of PoolRegions");
    }

    [Test]
    public void Paint_WithVocabulary_SometimesPaintsPoolsAndAlwaysResolves()
    {
        var tileset = BuildPoolCapableTileset();
        var parameters = BuildParameters(poolRegions: 2);

        var paintedCount = 0;
        for (var seed = 0; seed < 40; seed++)
        {
            var layout = MacroLayoutGenerator.Generate(parameters, new Random(seed), tileset);
            if (AnyLavaPainted(layout))
                paintedCount++;

            var success = TileResolver.TryResolve(tileset, layout, new Random(seed + 1000), out _, out var failureReason);
            success.Should().BeTrue($"seed {seed}: a painted (or unpainted) layout must still resolve end to end: {failureReason}");
        }

        paintedCount.Should().BeGreaterThan(0, "full pool vocabulary exists, so at least one of 40 seeds should paint a pool");
    }

    [Test]
    public void Paint_SameSeed_IsDeterministic()
    {
        var tileset = BuildPoolCapableTileset();
        var parameters = BuildParameters(poolRegions: 2);

        var layoutA = MacroLayoutGenerator.Generate(parameters, new Random(4242), tileset);
        var layoutB = MacroLayoutGenerator.Generate(parameters, new Random(4242), tileset);

        for (var x = 0; x <= layoutA.Corners.Width; x++)
        for (var y = 0; y <= layoutA.Corners.Height; y++)
        {
            layoutB.Corners.Labels[x, y].Should().Be(layoutA.Corners.Labels[x, y], $"corner ({x},{y}) label");
            layoutB.Corners.Heights[x, y].Should().Be(layoutA.Corners.Heights[x, y], $"corner ({x},{y}) height");
        }
    }

    /// <summary>
    /// Whenever a pool is painted, the room's remaining open (Floor) corners -- and the layout's open
    /// graph as a whole -- must stay a single connected component: LayoutElevationPoolPainter relabels
    /// interior corners away from OpenTerrain, exactly like LayoutAccentPainter's own blob paint, and
    /// must revert on disconnection the same way.
    /// </summary>
    [Test]
    public void Paint_NeverDisconnectsOpenSpace()
    {
        var tileset = BuildPoolCapableTileset();
        var parameters = BuildParameters(poolRegions: 3);

        for (var seed = 0; seed < 40; seed++)
        {
            var layout = MacroLayoutGenerator.Generate(parameters, new Random(seed), tileset);
            var openCorners = new List<(int X, int Y)>();
            var corners = layout.Corners;
            for (var x = 0; x <= corners.Width; x++)
            for (var y = 0; y <= corners.Height; y++)
            {
                if (corners.Labels[x, y] == Floor) openCorners.Add((x, y));
            }

            openCorners.Should().NotBeEmpty($"seed {seed}: a generated layout must always have open floor");

            // Flood fill from the first open corner; every open corner must be reachable.
            var visited = new HashSet<(int X, int Y)> { openCorners[0] };
            var frontier = new Queue<(int X, int Y)>();
            frontier.Enqueue(openCorners[0]);
            while (frontier.Count > 0)
            {
                var (cx, cy) = frontier.Dequeue();
                foreach (var (dx, dy) in new[] { (1, 0), (-1, 0), (0, 1), (0, -1) })
                {
                    var next = (cx + dx, cy + dy);
                    if (next.Item1 < 0 || next.Item1 > corners.Width || next.Item2 < 0 || next.Item2 > corners.Height) continue;
                    if (corners.Labels[next.Item1, next.Item2] != Floor) continue;
                    if (!visited.Add(next)) continue;
                    frontier.Enqueue(next);
                }
            }

            visited.Count.Should().Be(openCorners.Count, $"seed {seed}: every open Floor corner must be reachable from every other -- a pool must never sever the room's own floor");
        }
    }

    [Test]
    public void Paint_NeverRaisesBorderRing()
    {
        var tileset = BuildPoolCapableTileset();
        var parameters = BuildParameters(poolRegions: 3);

        for (var seed = 0; seed < 15; seed++)
        {
            var layout = MacroLayoutGenerator.Generate(parameters, new Random(seed), tileset);
            var corners = layout.Corners;

            for (var x = 0; x <= corners.Width; x++)
            {
                corners.Labels[x, 0].Should().Be(Wall, $"seed {seed}: border corner ({x},0) must stay solid");
                corners.Labels[x, corners.Height].Should().Be(Wall, $"seed {seed}: border corner ({x},{corners.Height}) must stay solid");
            }
            for (var y = 0; y <= corners.Height; y++)
            {
                corners.Labels[0, y].Should().Be(Wall, $"seed {seed}: border corner (0,{y}) must stay solid");
                corners.Labels[corners.Width, y].Should().Be(Wall, $"seed {seed}: border corner ({corners.Width},{y}) must stay solid");
            }
        }
    }

    /// <summary>
    /// Locks in the real production pairing actually paints pools at least some of the time --
    /// BaseGameTilesetProfiles.Dungeon (tde01) x StandardLayoutProfiles.Complex, the same composition
    /// LayoutElevationPainterTests exercises for its own real-pairing test. Best-effort by design, so
    /// this only asserts "sometimes", not "every seed".
    /// </summary>
    [Test]
    public void RealDungeonComplexComposition_SometimesPaintsPools()
    {
        var tilesetProfile = new BaseGameTilesetProfiles().BuildTilesetProfiles()[BaseGameTilesetProfiles.Dungeon];
        var layoutProfile = new StandardLayoutProfiles().BuildLayoutProfiles()[StandardLayoutProfiles.Complex];
        var model = TilesetTestSource.LoadTileset(tilesetProfile.TilesetResref);
        var composition = new DungeonComposition { Content = null, Tileset = tilesetProfile, Layout = layoutProfile };

        var paintedCount = 0;
        var resolvedCount = 0;
        const int size = 28;

        for (var seed = 20000; seed < 20040; seed++)
        {
            var parameters = composition.BuildLayoutParameters();
            parameters.EntranceCount = 1;
            parameters.ExitCount = 1;
            parameters.DoorTransitions = true;

            parameters.PoolRegions.Should().BeGreaterThan(0, "Dungeon/Complex must actually request pool regions");
            parameters.PoolTerrain.Should().NotBeNullOrEmpty("Dungeon's AccentTerrain (Lava) must be stamped as the pool terrain");

            var solved = LayoutSolver.Solve(parameters, model, size, size, seed, tilesetProfile.PrimaryOpenTerrain);
            if (!solved.Success) continue;

            resolvedCount++;
            if (AnyLavaPainted(solved.Layout))
                paintedCount++;
        }

        resolvedCount.Should().BeGreaterThan(0, "at least some seeds must generate successfully to evaluate pool painting");
        paintedCount.Should().BeGreaterThan(0, "the real Dungeon/Complex composition must paint at least one pool across 40 seeds");
    }
}
