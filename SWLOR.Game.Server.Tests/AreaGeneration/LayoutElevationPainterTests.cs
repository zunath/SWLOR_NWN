using System;
using System.Collections.Generic;
using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Feature.DungeonDefinition;
using SWLOR.Game.Server.Service.AreaGenerationService;
using SWLOR.Game.Server.Service.AreaGenerationService.Tileset;

namespace SWLOR.Game.Server.Tests.AreaGeneration;

/// <summary>
/// LayoutElevationPainter invariants, exercised the same way every other shared post-pass is tested
/// in this suite (see FenceAndAlleyTests): only through the public MacroLayoutGenerator.Generate entry
/// point, since the pass itself is internal. Covers shape-check gating (skip gracefully with no
/// vocabulary), back-compat (ElevationRegions=0 leaves the corner-height grid untouched), painting
/// actually raising corners and TileResolver still resolving the result end to end, forbidden-corner
/// exclusions (border/transition-anchor/room-center/crosser cells never raised), and determinism. Also
/// locks in that the real production pairing (BaseGameTilesetProfiles.Dungeon x
/// StandardLayoutProfiles.Complex) actually paints elevation sometimes, not just structurally.
/// </summary>
public class LayoutElevationPainterTests
{
    private const string Wall = "Wall";
    private const string Floor = "Floor";

    /// <summary>
    /// Same base fixture as TileResolverTests.BuildFixtureTileset (one flat tile per Wall/Floor corner
    /// combo) but WITHOUT any raised-tile vocabulary at all -- proves the painter is inert (no
    /// vocabulary => no painting) even when a region count is requested.
    /// </summary>
    private static TilesetModel BuildFlatOnlyTileset()
    {
        var tileset = new TilesetModel
        {
            Resref = "tst-flat",
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
    /// Same base fixture PLUS the two rim shapes (one corner raised; two adjacent corners raised, same
    /// delta) for BOTH Wall and Floor, ungrouped and blank-edged -- enough vocabulary for
    /// LayoutElevationPainter to paint a rectangular blob of either terrain and have TileResolver
    /// actually resolve the result.
    /// </summary>
    private static TilesetModel BuildElevationCapableTileset()
    {
        var tileset = BuildFlatOnlyTileset();
        var nextTileId = tileset.Tiles.Count;

        foreach (var terrain in new[] { Wall, Floor })
        {
            tileset.Tiles.Add(NewRaisedTile(nextTileId++, terrain, new[] { 0, 0, 1, 0 })); // one corner raised
            tileset.Tiles.Add(NewRaisedTile(nextTileId++, terrain, new[] { 0, 1, 1, 0 })); // two adjacent corners raised
        }

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

    private static TileRecord NewRaisedTile(int tileId, string terrain, int[] heights)
    {
        return new TileRecord
        {
            TileId = tileId,
            Corners = new[] { terrain, terrain, terrain, terrain },
            CornerHeights = heights,
            Edges = new[] { "", "", "", "" },
            PathNode = "A",
            GroupIndex = -1
        };
    }

    private static MacroLayoutParameters BuildParameters(int elevationRegions)
    {
        return new MacroLayoutParameters
        {
            Width = 24,
            Height = 24,
            SolidTerrain = Wall,
            OpenTerrain = Floor,
            Style = DungeonLayoutStyle.RoomsAndCorridors,
            CorridorMode = CorridorMode.Tunnel,
            MinRooms = 4,
            MaxRooms = 6,
            MinRoomCornerSize = 3,
            MaxRoomCornerSize = 5,
            EntranceCount = 1,
            ExitCount = 1,
            ElevationRegions = elevationRegions
        };
    }

    /// <summary>
    /// BuildElevationCapableTileset plus every rotation of the Ramp-crossed rim variant (the same
    /// two-adjacent-corners-raised shape, plus a "Ramp" edge on the axis perpendicular to the height
    /// transition -- see LayoutElevationPainter.TryAddRampLane) needed for a lane on any of the 4
    /// rectangle edges, both/single Ramp-edge variants.
    /// </summary>
    private static TilesetModel BuildRampCapableTileset()
    {
        // Floor-only rim vocabulary (mirrors real tde01, where Wall/SolidTerrain never carries height
        // at all -- see LayoutElevationPainter class doc): giving Wall its own rim vocabulary too (as
        // BuildElevationCapableTileset does) would let TryPaintSolidBlob win the region budget ahead of
        // TryPaintOpenRoomBlob, starving the ramp hook, which only fires on the OpenTerrain path.
        var tileset = BuildFlatOnlyTileset();
        var nextTileId = tileset.Tiles.Count;
        tileset.Tiles.Add(NewRaisedTile(nextTileId++, Floor, new[] { 0, 0, 1, 0 })); // one corner raised
        tileset.Tiles.Add(NewRaisedTile(nextTileId++, Floor, new[] { 0, 1, 1, 0 })); // two adjacent corners raised

        // East/west lanes: raised LEFT column (TL,BL), ground RIGHT column (TR,BR) -- Ramp on Top/Bottom.
        tileset.Tiles.Add(NewEdgedTile(nextTileId++, Floor, new[] { 1, 0, 0, 1 }, "Ramp", "", "Ramp", ""));
        tileset.Tiles.Add(NewEdgedTile(nextTileId++, Floor, new[] { 1, 0, 0, 1 }, "Ramp", "", "", ""));
        tileset.Tiles.Add(NewEdgedTile(nextTileId++, Floor, new[] { 1, 0, 0, 1 }, "", "", "Ramp", ""));

        // The mirrored raised-RIGHT-column shape (west edge orientation).
        tileset.Tiles.Add(NewEdgedTile(nextTileId++, Floor, new[] { 0, 1, 1, 0 }, "Ramp", "", "Ramp", ""));
        tileset.Tiles.Add(NewEdgedTile(nextTileId++, Floor, new[] { 0, 1, 1, 0 }, "Ramp", "", "", ""));
        tileset.Tiles.Add(NewEdgedTile(nextTileId++, Floor, new[] { 0, 1, 1, 0 }, "", "", "Ramp", ""));

        // North/south lanes: raised BOTTOM row (BL,BR), ground TOP row (TL,TR) -- Ramp on Left/Right.
        tileset.Tiles.Add(NewEdgedTile(nextTileId++, Floor, new[] { 0, 0, 1, 1 }, "", "Ramp", "", "Ramp"));
        tileset.Tiles.Add(NewEdgedTile(nextTileId++, Floor, new[] { 0, 0, 1, 1 }, "", "Ramp", "", ""));
        tileset.Tiles.Add(NewEdgedTile(nextTileId++, Floor, new[] { 0, 0, 1, 1 }, "", "", "", "Ramp"));

        // The mirrored raised-TOP-row shape (south edge orientation).
        tileset.Tiles.Add(NewEdgedTile(nextTileId++, Floor, new[] { 1, 1, 0, 0 }, "", "Ramp", "", "Ramp"));
        tileset.Tiles.Add(NewEdgedTile(nextTileId++, Floor, new[] { 1, 1, 0, 0 }, "", "Ramp", "", ""));
        tileset.Tiles.Add(NewEdgedTile(nextTileId++, Floor, new[] { 1, 1, 0, 0 }, "", "", "", "Ramp"));

        return tileset;
    }

    private static TileRecord NewEdgedTile(int tileId, string terrain, int[] heights, string top, string right, string bottom, string left)
    {
        return new TileRecord
        {
            TileId = tileId,
            Corners = new[] { terrain, terrain, terrain, terrain },
            CornerHeights = heights,
            Edges = new[] { top, right, bottom, left },
            PathNode = "A",
            GroupIndex = -1
        };
    }

    private static MacroLayoutParameters BuildRampParameters(int elevationRegions)
    {
        var parameters = BuildParameters(elevationRegions);
        parameters.ElevationRamps = true;
        return parameters;
    }

    private static bool AnyCellCarriesACrosser(MacroLayout layout, int x, int y)
    {
        for (var dx = -1; dx <= 0; dx++)
        for (var dy = -1; dy <= 0; dy++)
        {
            var cx = x + dx;
            var cy = y + dy;
            if (cx < 0 || cy < 0 || cx >= layout.Corners.Width || cy >= layout.Corners.Height) continue;

            if (!string.IsNullOrEmpty(layout.Crossers.GetEdge(cx, cy, EdgeSlot.Top)) ||
                !string.IsNullOrEmpty(layout.Crossers.GetEdge(cx, cy, EdgeSlot.Right)) ||
                !string.IsNullOrEmpty(layout.Crossers.GetEdge(cx, cy, EdgeSlot.Bottom)) ||
                !string.IsNullOrEmpty(layout.Crossers.GetEdge(cx, cy, EdgeSlot.Left)))
                return true;
        }

        return false;
    }

    [Test]
    public void Paint_NoVocabulary_LeavesGridFlatAcrossManySeeds()
    {
        var tileset = BuildFlatOnlyTileset();
        var parameters = BuildParameters(elevationRegions: 3);

        for (var seed = 0; seed < 15; seed++)
        {
            var layout = MacroLayoutGenerator.Generate(parameters, new Random(seed), tileset);
            layout.Corners.HasAnyHeight().Should().BeFalse(
                $"seed {seed}: a tileset with zero rim vocabulary must leave the painter fully inert (skip gracefully)");
        }
    }

    [Test]
    public void Paint_NullTileset_LeavesGridFlat()
    {
        var parameters = BuildParameters(elevationRegions: 3);

        // No tileset argument at all -- mirrors every pre-height caller (LayoutGroupStamper's own
        // null-tileset guard is the precedent this mirrors).
        var layout = MacroLayoutGenerator.Generate(parameters, new Random(1));

        layout.Corners.HasAnyHeight().Should().BeFalse("a null tileset means no shape verification is possible, so the pass must no-op");
    }

    [Test]
    public void Paint_ZeroRegionsRequested_LeavesGridFlatAcrossManySeeds()
    {
        var tileset = BuildElevationCapableTileset();
        var parameters = BuildParameters(elevationRegions: 0);

        for (var seed = 0; seed < 15; seed++)
        {
            var layout = MacroLayoutGenerator.Generate(parameters, new Random(seed), tileset);
            layout.Corners.HasAnyHeight().Should().BeFalse(
                $"seed {seed}: ElevationRegions=0 is the default for every existing caller and must be byte-identical to the pre-elevation legacy path");
        }
    }

    [Test]
    public void Paint_WithVocabulary_SometimesRaisesCornersAndAlwaysResolves()
    {
        var tileset = BuildElevationCapableTileset();
        var parameters = BuildParameters(elevationRegions: 2);

        var paintedCount = 0;
        for (var seed = 0; seed < 30; seed++)
        {
            var layout = MacroLayoutGenerator.Generate(parameters, new Random(seed), tileset);
            if (layout.Corners.HasAnyHeight())
                paintedCount++;

            var success = TileResolver.TryResolve(tileset, layout, new Random(seed + 1000), out _, out var failureReason);
            success.Should().BeTrue($"seed {seed}: a painted (or unpainted) layout must still resolve end to end: {failureReason}");
        }

        paintedCount.Should().BeGreaterThan(0, "both Wall and Floor rim vocabulary exist, so at least one of 30 seeds should paint a region");
    }

    [Test]
    public void Paint_SameSeed_IsDeterministic()
    {
        var tileset = BuildElevationCapableTileset();
        var parameters = BuildParameters(elevationRegions: 2);

        var layoutA = MacroLayoutGenerator.Generate(parameters, new Random(777), tileset);
        var layoutB = MacroLayoutGenerator.Generate(parameters, new Random(777), tileset);

        for (var x = 0; x <= layoutA.Corners.Width; x++)
        for (var y = 0; y <= layoutA.Corners.Height; y++)
            layoutB.Corners.Heights[x, y].Should().Be(layoutA.Corners.Heights[x, y], $"corner ({x},{y})");
    }

    [Test]
    public void Paint_NeverRaisesBorderRing()
    {
        var tileset = BuildElevationCapableTileset();
        var parameters = BuildParameters(elevationRegions: 3);

        for (var seed = 0; seed < 10; seed++)
        {
            var layout = MacroLayoutGenerator.Generate(parameters, new Random(seed), tileset);
            var corners = layout.Corners;

            for (var x = 0; x <= corners.Width; x++)
            {
                corners.Heights[x, 0].Should().Be(0, $"seed {seed}: border corner ({x},0) must stay flat");
                corners.Heights[x, corners.Height].Should().Be(0, $"seed {seed}: border corner ({x},{corners.Height}) must stay flat");
            }
            for (var y = 0; y <= corners.Height; y++)
            {
                corners.Heights[0, y].Should().Be(0, $"seed {seed}: border corner (0,{y}) must stay flat");
                corners.Heights[corners.Width, y].Should().Be(0, $"seed {seed}: border corner ({corners.Width},{y}) must stay flat");
            }
        }
    }

    /// <summary>
    /// Transition anchors (Entrance/Exit tiles) must always stay flat. Room CenterTile is deliberately
    /// NOT asserted here: LayoutElevationPainter does not reserve it (see BuildForbiddenCorners doc) --
    /// a small room's only interior raise candidates are often exactly its center tile's own corners,
    /// and that tile is separately protected only when it actually hosts a transition.
    /// </summary>
    [Test]
    public void Paint_NeverRaisesTransitionAnchorCorners()
    {
        var tileset = BuildElevationCapableTileset();
        var parameters = BuildParameters(elevationRegions: 3);

        for (var seed = 0; seed < 15; seed++)
        {
            var layout = MacroLayoutGenerator.Generate(parameters, new Random(seed), tileset);
            var corners = layout.Corners;

            foreach (var transition in layout.Transitions)
            {
                var (tx, ty) = transition.Tile;
                foreach (var (x, y) in new[] { (tx, ty), (tx + 1, ty), (tx, ty + 1), (tx + 1, ty + 1) })
                    corners.Heights[x, y].Should().Be(0, $"seed {seed}: transition anchor corner ({x},{y}) must stay flat");
            }
        }
    }

    [Test]
    public void Paint_NeverRaisesCornersTouchingACrosserCell()
    {
        var tileset = BuildElevationCapableTileset();
        var parameters = BuildParameters(elevationRegions: 3);

        for (var seed = 0; seed < 15; seed++)
        {
            var layout = MacroLayoutGenerator.Generate(parameters, new Random(seed), tileset);
            var corners = layout.Corners;

            for (var x = 0; x <= corners.Width; x++)
            for (var y = 0; y <= corners.Height; y++)
            {
                if (corners.Heights[x, y] == 0) continue;
                AnyCellCarriesACrosser(layout, x, y).Should().BeFalse(
                    $"seed {seed}: raised corner ({x},{y}) must never touch a crosser-bearing cell (tunnels/fences)");
            }
        }
    }

    /// <summary>
    /// Locks in the real production pairing actually paints elevation at least some of the time
    /// (not just structurally present-but-inert) -- BaseGameTilesetProfiles.Dungeon (tde01) x
    /// StandardLayoutProfiles.Complex, the same composition OnboardedTilesetPipelineTests exercises
    /// for generation success. Best-effort by design (see MacroLayoutParameters.ElevationRegions), so
    /// this only asserts "sometimes", not "every seed".
    /// </summary>
    [Test]
    public void RealDungeonComplexComposition_SometimesPaintsElevation()
    {
        var tilesetProfile = new BaseGameTilesetProfiles().BuildTilesetProfiles()[BaseGameTilesetProfiles.Dungeon];
        var layoutProfile = new StandardLayoutProfiles().BuildLayoutProfiles()[StandardLayoutProfiles.Complex];
        var model = TilesetTestSource.LoadTileset(tilesetProfile.TilesetResref);
        var composition = new DungeonComposition { Content = null, Tileset = tilesetProfile, Layout = layoutProfile };

        var paintedCount = 0;
        var resolvedCount = 0;
        const int size = 24;

        for (var seed = 9000; seed < 9030; seed++)
        {
            var parameters = composition.BuildLayoutParameters();
            parameters.EntranceCount = 1;
            parameters.ExitCount = 1;
            parameters.DoorTransitions = true;

            parameters.ElevationRegions.Should().BeGreaterThan(0, "Dungeon/Complex must actually request elevation regions");

            var solved = LayoutSolver.Solve(parameters, model, size, size, seed, tilesetProfile.PrimaryOpenTerrain);
            if (!solved.Success) continue;

            resolvedCount++;
            if (solved.Layout.Corners.HasAnyHeight())
                paintedCount++;
        }

        resolvedCount.Should().BeGreaterThan(0, "at least some seeds must generate successfully to evaluate elevation painting");
        paintedCount.Should().BeGreaterThan(0, "the real Dungeon/Complex composition must paint elevation on at least one of 30 seeds");
    }

    /// <summary>
    /// Same "actually paints, not merely declared" gate for the exterior wave's two height-capable
    /// tilesets (Desert/ttd01 and Forest/ttf01, both resolved to their SWLOR hak copies -- see
    /// BaseGameTilesetProfiles' wave comment): each declares MaxElevationRegions(2) on the strength
    /// of its crosser-free raised-ground rim inventory (7 tiles in ttd01, 9 in ttf01 -- all on the
    /// walkable Desert/Forest OPEN terrain of the inverted composition, verified via the census's
    /// structural ElevationBlob classifier), and this locks in that LayoutElevationPainter's own
    /// runtime rim-vocabulary probe agrees often enough to paint raised desert hillocks/forest
    /// knolls on at least one of 30 seeds. Best-effort by design, so "sometimes", not "every seed".
    /// </summary>
    [TestCase(BaseGameTilesetProfiles.Desert)]
    [TestCase(BaseGameTilesetProfiles.Forest)]
    public void RealExteriorComplexComposition_SometimesPaintsElevation(string tilesetKey)
    {
        var tilesetProfile = new BaseGameTilesetProfiles().BuildTilesetProfiles()[tilesetKey];
        var layoutProfile = new StandardLayoutProfiles().BuildLayoutProfiles()[StandardLayoutProfiles.Complex];
        var model = TilesetTestSource.LoadTileset(tilesetProfile.TilesetResref);
        var composition = new DungeonComposition { Content = null, Tileset = tilesetProfile, Layout = layoutProfile };

        var paintedCount = 0;
        var resolvedCount = 0;
        const int size = 24;

        for (var seed = 9000; seed < 9030; seed++)
        {
            var parameters = composition.BuildLayoutParameters();
            parameters.EntranceCount = 1;
            parameters.ExitCount = 1;
            parameters.DoorTransitions = true;

            parameters.ElevationRegions.Should().BeGreaterThan(0, $"{tilesetKey}/Complex must actually request elevation regions");

            var solved = LayoutSolver.Solve(parameters, model, size, size, seed, tilesetProfile.PrimaryOpenTerrain);
            if (!solved.Success) continue;

            resolvedCount++;
            if (solved.Layout.Corners.HasAnyHeight())
                paintedCount++;
        }

        resolvedCount.Should().BeGreaterThan(0, "at least some seeds must generate successfully to evaluate elevation painting");
        paintedCount.Should().BeGreaterThan(0, $"the real {tilesetKey}/Complex composition must paint elevation on at least one of 30 seeds");
    }

    // ------------------------------------------------------------------
    // Ramp lane (MacroLayoutParameters.ElevationRamps / TryAddRampLane)
    // ------------------------------------------------------------------

    private static bool AnyRampEdgePresent(MacroLayout layout)
    {
        var crossers = layout.Crossers;
        for (var x = 0; x < crossers.Width; x++)
        for (var y = 0; y < crossers.Height; y++)
        {
            for (var slot = 0; slot < 4; slot++)
            {
                if (string.Equals(crossers.GetEdge(x, y, slot), "Ramp", StringComparison.Ordinal))
                    return true;
            }
        }
        return false;
    }

    [Test]
    public void Ramps_Disabled_NeverWritesRampEdgesAcrossManySeeds()
    {
        var tileset = BuildRampCapableTileset();
        var parameters = BuildParameters(elevationRegions: 3); // ElevationRamps left at its false default

        for (var seed = 0; seed < 20; seed++)
        {
            var layout = MacroLayoutGenerator.Generate(parameters, new Random(seed), tileset);
            AnyRampEdgePresent(layout).Should().BeFalse(
                $"seed {seed}: ElevationRamps=false is the default for every existing caller and must never write a Ramp edge");
        }
    }

    [Test]
    public void Ramps_NoVocabulary_LeavesGridWithoutRampEdgesEvenWhenEnabled()
    {
        var tileset = BuildElevationCapableTileset(); // no Ramp-edged tiles at all
        var parameters = BuildRampParameters(elevationRegions: 3);

        for (var seed = 0; seed < 20; seed++)
        {
            var layout = MacroLayoutGenerator.Generate(parameters, new Random(seed), tileset);
            AnyRampEdgePresent(layout).Should().BeFalse(
                $"seed {seed}: a tileset with zero Ramp vocabulary must leave TryAddRampLane fully inert");
        }
    }

    [Test]
    public void Ramps_Enabled_SometimesWritesRampEdgesAndAlwaysResolves()
    {
        var tileset = BuildRampCapableTileset();
        var parameters = BuildRampParameters(elevationRegions: 2);

        var rampedCount = 0;
        var paintedCount = 0;
        for (var seed = 0; seed < 40; seed++)
        {
            var layout = MacroLayoutGenerator.Generate(parameters, new Random(seed), tileset);
            if (layout.Corners.HasAnyHeight()) paintedCount++;
            if (AnyRampEdgePresent(layout))
                rampedCount++;

            var success = TileResolver.TryResolve(tileset, layout, new Random(seed + 1000), out _, out var failureReason);
            success.Should().BeTrue($"seed {seed}: a layout with (or without) a ramp lane must still resolve end to end: {failureReason}");
        }

        TestContext.WriteLine($"paintedCount={paintedCount} rampedCount={rampedCount}");
        rampedCount.Should().BeGreaterThan(0, "full Ramp vocabulary exists, so at least one of 40 seeds should splice a lane");
    }

    /// <summary>Every Ramp edge written must sit on a raised (nonzero-height) cell -- TryAddRampLane
    /// only ever operates on rim cells of an already-placed elevation blob, never a flat cell.</summary>
    [Test]
    public void Ramps_OnlyEverWrittenOnRaisedCells()
    {
        var tileset = BuildRampCapableTileset();
        var parameters = BuildRampParameters(elevationRegions: 3);

        for (var seed = 0; seed < 40; seed++)
        {
            var layout = MacroLayoutGenerator.Generate(parameters, new Random(seed), tileset);
            var corners = layout.Corners;
            var crossers = layout.Crossers;

            for (var x = 0; x < crossers.Width; x++)
            for (var y = 0; y < crossers.Height; y++)
            {
                var hasRamp = false;
                for (var slot = 0; slot < 4; slot++)
                {
                    if (string.Equals(crossers.GetEdge(x, y, slot), "Ramp", StringComparison.Ordinal))
                        hasRamp = true;
                }
                if (!hasRamp) continue;

                var anyRaisedCorner =
                    corners.Heights[x, y] != 0 || corners.Heights[x + 1, y] != 0 ||
                    corners.Heights[x, y + 1] != 0 || corners.Heights[x + 1, y + 1] != 0;
                anyRaisedCorner.Should().BeTrue($"seed {seed}: Ramp-crossed cell ({x},{y}) must touch at least one raised corner");
            }
        }
    }

    [Test]
    public void Ramps_SameSeed_IsDeterministic()
    {
        var tileset = BuildRampCapableTileset();
        var parameters = BuildRampParameters(elevationRegions: 2);

        var layoutA = MacroLayoutGenerator.Generate(parameters, new Random(555), tileset);
        var layoutB = MacroLayoutGenerator.Generate(parameters, new Random(555), tileset);

        for (var x = 0; x < layoutA.Crossers.Width; x++)
        for (var y = 0; y < layoutA.Crossers.Height; y++)
        for (var slot = 0; slot < 4; slot++)
        {
            layoutB.Crossers.GetEdge(x, y, slot).Should().Be(layoutA.Crossers.GetEdge(x, y, slot), $"cell ({x},{y}) slot {slot}");
        }
    }

    /// <summary>Locks in the real production pairing (BaseGameTilesetProfiles.Dungeon x
    /// StandardLayoutProfiles.Complex, which now sets ElevationRamps=true) actually splices a ramp
    /// lane at least some of the time.</summary>
    [Test]
    public void RealDungeonComplexComposition_SometimesWritesRampEdges()
    {
        var tilesetProfile = new BaseGameTilesetProfiles().BuildTilesetProfiles()[BaseGameTilesetProfiles.Dungeon];
        var layoutProfile = new StandardLayoutProfiles().BuildLayoutProfiles()[StandardLayoutProfiles.Complex];
        var model = TilesetTestSource.LoadTileset(tilesetProfile.TilesetResref);
        var composition = new DungeonComposition { Content = null, Tileset = tilesetProfile, Layout = layoutProfile };

        var rampedCount = 0;
        var resolvedCount = 0;
        const int size = 24;

        for (var seed = 30000; seed < 30040; seed++)
        {
            var parameters = composition.BuildLayoutParameters();
            parameters.EntranceCount = 1;
            parameters.ExitCount = 1;
            parameters.DoorTransitions = true;

            parameters.ElevationRamps.Should().BeTrue("Dungeon/Complex must enable ramp lanes");

            var solved = LayoutSolver.Solve(parameters, model, size, size, seed, tilesetProfile.PrimaryOpenTerrain);
            if (!solved.Success) continue;

            resolvedCount++;
            if (AnyRampEdgePresent(solved.Layout))
                rampedCount++;
        }

        resolvedCount.Should().BeGreaterThan(0, "at least some seeds must generate successfully to evaluate ramp splicing");
        rampedCount.Should().BeGreaterThan(0, "the real Dungeon/Complex composition must splice at least one ramp lane across 40 seeds");
    }
}
