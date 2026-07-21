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
/// LayoutReliefPainter invariants, exercised only through the public MacroLayoutGenerator.Generate
/// entry point (the pass itself is internal), mirroring LayoutElevationPainterTests' conventions:
/// capability gating (zero vocabulary / zero regions / null tileset stay fully inert), painting
/// actually mutating corners with TileResolver still resolving end to end, and determinism.
///
/// Additionally -- and deliberately, after the illithid census-vs-practice lesson (a tile the census
/// structurally credits must be PROVEN to actually place in real generation, see
/// OnboardedTilesetPipelineTests' history) -- this fixture carries positive placement proofs for the
/// real production pairings: BaseGameTilesetProfiles.Dungeon (tde01) x StandardLayoutProfiles.Complex
/// must actually resolve formerly-height-exempt per-corner relief tiles into real layouts, stamp the
/// baked-mesh "Ramp - *" relief pieces, and BaseGameTilesetProfiles.MinesAndCaverns (tdm01) x Complex
/// must actually flip GentleSlope blend corners and splice Slope lanes.
/// </summary>
public class LayoutReliefPainterTests
{
    private const string Wall = "Wall";
    private const string Floor = "Floor";

    // ------------------------------------------------------------------
    // Synthetic fixtures (mirroring LayoutElevationPainterTests')
    // ------------------------------------------------------------------

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
            tileset.Tiles.Add(NewTile(nextTileId++, new[] { tl, tr, br, bl }, new[] { 0, 0, 0, 0 }));
        }

        return tileset;
    }

    /// <summary>
    /// Flat fixture plus EVERY nonzero all-Floor corner-height delta profile (all 15 raised subsets,
    /// including the diagonal saddles no uniform region-growth pass can produce) -- full per-corner
    /// relief vocabulary, so every height-toggle proposal the painter makes on interior floor corners
    /// verifies.
    /// </summary>
    private static TilesetModel BuildReliefCapableTileset()
    {
        var tileset = BuildFlatOnlyTileset();
        var nextTileId = tileset.Tiles.Count;

        for (var mask = 1; mask < 16; mask++)
        {
            var heights = new[] { (mask & 8) >> 3, (mask & 4) >> 2, (mask & 2) >> 1, mask & 1 };
            tileset.Tiles.Add(NewTile(nextTileId++, new[] { Floor, Floor, Floor, Floor }, heights));
        }

        return tileset;
    }

    private static TileRecord NewTile(int tileId, string[] corners, int[] heights)
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

    private static MacroLayoutParameters BuildParameters(int reliefRegions)
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
            ReliefRegions = reliefRegions
        };
    }

    // ------------------------------------------------------------------
    // Gating / back-compat
    // ------------------------------------------------------------------

    [Test]
    public void Paint_ZeroRegionsRequested_LeavesGridFlatAcrossManySeeds()
    {
        var tileset = BuildReliefCapableTileset();
        var parameters = BuildParameters(reliefRegions: 0);

        for (var seed = 0; seed < 15; seed++)
        {
            var layout = MacroLayoutGenerator.Generate(parameters, new Random(seed), tileset);
            layout.Corners.HasAnyHeight().Should().BeFalse(
                $"seed {seed}: ReliefRegions=0 is the default for every existing caller and must leave the legacy flat path untouched");
        }
    }

    [Test]
    public void Paint_NoVocabulary_LeavesGridFlatAcrossManySeeds()
    {
        var tileset = BuildFlatOnlyTileset();
        var parameters = BuildParameters(reliefRegions: 3);

        for (var seed = 0; seed < 15; seed++)
        {
            var layout = MacroLayoutGenerator.Generate(parameters, new Random(seed), tileset);
            layout.Corners.HasAnyHeight().Should().BeFalse(
                $"seed {seed}: a tileset with zero relief vocabulary must leave the painter fully inert (capability gate)");
        }
    }

    [Test]
    public void Paint_NullTileset_LeavesGridFlat()
    {
        var parameters = BuildParameters(reliefRegions: 3);

        var layout = MacroLayoutGenerator.Generate(parameters, new Random(1));

        layout.Corners.HasAnyHeight().Should().BeFalse(
            "a null tileset means no per-perturbation verification is possible, so the pass must no-op");
    }

    [Test]
    public void Paint_WithVocabulary_SometimesPaintsAndAlwaysResolves()
    {
        var tileset = BuildReliefCapableTileset();
        var parameters = BuildParameters(reliefRegions: 2);

        var paintedCount = 0;
        for (var seed = 0; seed < 30; seed++)
        {
            var layout = MacroLayoutGenerator.Generate(parameters, new Random(seed), tileset);
            if (layout.Corners.HasAnyHeight())
                paintedCount++;

            var success = TileResolver.TryResolve(tileset, layout, new Random(seed + 1000), out _, out var failureReason);
            success.Should().BeTrue($"seed {seed}: a relief-painted layout must still resolve end to end: {failureReason}");
        }

        paintedCount.Should().BeGreaterThan(0,
            "full per-corner Floor vocabulary exists, so at least one of 30 seeds should keep at least one perturbation");
    }

    /// <summary>The defining capability of this pass: a delta profile no uniform region-growth pass
    /// can paint (the diagonal saddle -- exactly two OPPOSITE corners of one cell raised) must
    /// actually occur in painted layouts, since the fixture carries that tile.</summary>
    [Test]
    public void Paint_WithFullVocabulary_SometimesPaintsADiagonalSaddle()
    {
        var tileset = BuildReliefCapableTileset();
        var parameters = BuildParameters(reliefRegions: 3);

        var saddleCount = 0;
        for (var seed = 0; seed < 60 && saddleCount == 0; seed++)
        {
            var layout = MacroLayoutGenerator.Generate(parameters, new Random(seed), tileset);
            var corners = layout.Corners;

            for (var x = 0; x < corners.Width && saddleCount == 0; x++)
            for (var y = 0; y < corners.Height && saddleCount == 0; y++)
            {
                var hTl = corners.Heights[x, y + 1];
                var hTr = corners.Heights[x + 1, y + 1];
                var hBr = corners.Heights[x + 1, y];
                var hBl = corners.Heights[x, y];
                if ((hTl == hBr && hTr == hBl && hTl != hTr) &&
                    corners.Labels[x, y] == Floor && corners.Labels[x + 1, y] == Floor &&
                    corners.Labels[x, y + 1] == Floor && corners.Labels[x + 1, y + 1] == Floor)
                {
                    saddleCount++;
                }
            }
        }

        saddleCount.Should().BeGreaterThan(0,
            "per-corner perturbation must be able to reach a same-terrain diagonal saddle across 60 seeds -- " +
            "the shape that motivated this pass (region growth can never produce it)");
    }

    [Test]
    public void Paint_SameSeed_IsDeterministic()
    {
        var tileset = BuildReliefCapableTileset();
        var parameters = BuildParameters(reliefRegions: 2);

        var first = MacroLayoutGenerator.Generate(parameters, new Random(4242), tileset);
        var second = MacroLayoutGenerator.Generate(parameters, new Random(4242), tileset);

        for (var x = 0; x <= first.Corners.Width; x++)
        for (var y = 0; y <= first.Corners.Height; y++)
        {
            second.Corners.Heights[x, y].Should().Be(first.Corners.Heights[x, y], $"corner ({x},{y}) height must be deterministic");
            second.Corners.Labels[x, y].Should().Be(first.Corners.Labels[x, y], $"corner ({x},{y}) label must be deterministic");
        }

        for (var x = 0; x < first.Crossers.Width; x++)
        for (var y = 0; y < first.Crossers.Height; y++)
        for (var slot = 0; slot < 4; slot++)
        {
            second.Crossers.GetEdge(x, y, slot).Should().Be(first.Crossers.GetEdge(x, y, slot), $"edge ({x},{y},{slot}) must be deterministic");
        }
    }

    // ------------------------------------------------------------------
    // Real-pairing positive placement proofs
    // ------------------------------------------------------------------

    private static bool IsFlat(TileRecord tile) =>
        tile.CornerHeights[0] == 0 && tile.CornerHeights[1] == 0 && tile.CornerHeights[2] == 0 && tile.CornerHeights[3] == 0;

    /// <summary>
    /// Local mirror of the PRE-relief height mechanisms' reachable shapes (uniform-terrain rim blobs,
    /// pool banks, ramp-crossed rim tiles) -- see TileCoverageCensusTests' IsElevationBlobReachable/
    /// IsPoolBankReachable/IsElevationRampReachable. A resolved non-flat tile matching NONE of these is
    /// definitionally content only the per-corner relief mechanism (or its ReliefPiece stamping twin)
    /// can have placed.
    /// </summary>
    private static bool IsPreReliefReachableShape(TileRecord tile, string open, IReadOnlyCollection<string> accents)
    {
        if (IsFlat(tile)) return true;

        var min = tile.CornerHeights.Min();
        var normalized = tile.CornerHeights.Select(h => h - min).ToArray();
        var nonZero = normalized.Where(h => h != 0).ToArray();

        bool AllCorners(string terrain) => tile.Corners.All(c => string.Equals(c, terrain, StringComparison.OrdinalIgnoreCase));

        // Uniform-terrain rim blob shapes (one corner; two ADJACENT; three, same delta) on open or
        // solid terrain -- solid never carries height on tde01/tdm01, so open+accents suffice here.
        foreach (var terrain in accents.Append(open))
        {
            if (!AllCorners(terrain)) continue;
            if (nonZero.Length == 1) return true;
            if (nonZero.Length == 3 && nonZero.Distinct().Count() == 1) return true;
            if (nonZero.Length == 2 && nonZero.Distinct().Count() == 1)
            {
                bool tl = normalized[0] != 0, tr = normalized[1] != 0, br = normalized[2] != 0, bl = normalized[3] != 0;
                if ((tl && tr) || (tr && br) || (br && bl) || (bl && tl)) return true;
            }
        }

        // Pool-bank shapes: open corners all one height, accent corners all exactly one story below.
        foreach (var accent in accents)
        {
            var openIdx = new List<int>();
            var accentIdx = new List<int>();
            var mixed = true;
            for (var i = 0; i < 4; i++)
            {
                if (string.Equals(tile.Corners[i], open, StringComparison.OrdinalIgnoreCase)) openIdx.Add(i);
                else if (string.Equals(tile.Corners[i], accent, StringComparison.OrdinalIgnoreCase)) accentIdx.Add(i);
                else { mixed = false; break; }
            }
            if (!mixed || openIdx.Count == 0 || accentIdx.Count == 0) continue;

            var openHeights = openIdx.Select(i => tile.CornerHeights[i]).Distinct().ToList();
            var accentHeights = accentIdx.Select(i => tile.CornerHeights[i]).Distinct().ToList();
            if (openHeights.Count == 1 && accentHeights.Count == 1 && openHeights[0] - accentHeights[0] == 1)
            {
                if (accentIdx.Count == 1 || accentIdx.Count == 3) return true;
                if (accentIdx.Count == 2)
                {
                    bool tlA = accentIdx.Contains(0), trA = accentIdx.Contains(1), brA = accentIdx.Contains(2), blA = accentIdx.Contains(3);
                    if ((tlA && trA) || (trA && brA) || (brA && blA) || (blA && tlA)) return true;
                }
            }
        }

        // Ramp-lane rim tiles: all-open corners, two-adjacent-raised, Ramp edges only.
        if (AllCorners(open) && nonZero.Length == 2 && nonZero.Distinct().Count() == 1)
        {
            bool tl = normalized[0] != 0, tr = normalized[1] != 0, br = normalized[2] != 0, bl = normalized[3] != 0;
            var adjacent = (tl && tr) || (tr && br) || (br && bl) || (bl && tl);
            if (adjacent && tile.Edges.Any(e => !string.IsNullOrEmpty(e)) &&
                tile.Edges.All(e => string.IsNullOrEmpty(e) || string.Equals(e, "Ramp", StringComparison.OrdinalIgnoreCase)))
                return true;
        }

        return false;
    }

    /// <summary>
    /// THE honesty proof for tde01: the real Dungeon x Complex composition must actually resolve
    /// formerly-height-exempt tiles -- non-flat tiles whose shape none of the pre-relief mechanisms
    /// could reach -- into real generated layouts, and must actually stamp at least one of the three
    /// configured baked-mesh "Ramp - *" relief pieces (all 1x1 non-flat GROUPs, so they can ONLY
    /// arrive via a ReliefPiece pin, never via the corner/edge resolver).
    /// </summary>
    [Test]
    public void RealDungeonComplexComposition_ResolvesPerCornerReliefTilesAndStampsRampPieces()
    {
        var tilesetProfile = new BaseGameTilesetProfiles().BuildTilesetProfiles()[BaseGameTilesetProfiles.Dungeon];
        var layoutProfile = new StandardLayoutProfiles().BuildLayoutProfiles()[StandardLayoutProfiles.Complex];
        var model = TilesetTestSource.LoadTileset(tilesetProfile.TilesetResref);
        var composition = new DungeonComposition { Content = null, Tileset = tilesetProfile, Layout = layoutProfile };

        var accents = new[] { "Lava", "Water", "Sewer", "Ice", "Pit" };
        var rampPieceTileIds = model.Groups
            .Where(g => g.Name is "Ramp - Straight" or "Ramp - Corner, Floor" or "Ramp - Corner, Lava")
            .SelectMany(g => g.TileIds.Where(id => id >= 0))
            .ToHashSet();
        rampPieceTileIds.Should().NotBeEmpty("tde01 must carry the configured Ramp - * groups");

        var resolvedCount = 0;
        var newlyReachedTileCount = 0;
        var rampPiecePinCount = 0;
        const int size = 24;

        for (var seed = 9100; seed < 9160; seed++)
        {
            var parameters = composition.BuildLayoutParameters();
            parameters.EntranceCount = 1;
            parameters.ExitCount = 1;
            parameters.DoorTransitions = true;

            parameters.ReliefRegions.Should().BeGreaterThan(0, "Dungeon/Complex must actually request relief regions");

            var solved = LayoutSolver.Solve(parameters, model, size, size, seed, tilesetProfile.PrimaryOpenTerrain);
            if (!solved.Success) continue;
            resolvedCount++;

            foreach (var resolvedTile in solved.Resolved.Tiles)
            {
                var record = model.Tiles[resolvedTile.TileId];
                if (rampPieceTileIds.Contains(resolvedTile.TileId))
                {
                    rampPiecePinCount++;
                    continue;
                }
                if (record.GroupIndex != -1) continue;
                if (!IsPreReliefReachableShape(record, "Floor", accents))
                    newlyReachedTileCount++;
            }
        }

        resolvedCount.Should().BeGreaterThan(0, "at least some seeds must generate successfully");
        newlyReachedTileCount.Should().BeGreaterThan(0,
            "across 60 seeds the per-corner relief pass must place at least one formerly-height-exempt tile shape " +
            "(mixed-grade accent banks, diagonal saddles, raised accent corners) -- the census credits these as " +
            "TerrainRelief-reachable, so real generation must prove it");
        rampPiecePinCount.Should().BeGreaterThan(0,
            "across 60 seeds LayoutGroupStamper's ReliefPiece kind must stamp at least one baked-mesh Ramp - * group piece " +
            "onto a painted height-matching cell");
    }

    /// <summary>
    /// The tdm01 twin: MinesAndCaverns x Complex must actually flip GentleSlope blend corners into
    /// real layouts (the ReliefBlendTerrain mechanism) and splice at least one Slope lane (the
    /// profile-declared alternate RampCrosser name) across the seed sweep.
    /// </summary>
    [Test]
    public void RealMinesComplexComposition_FlipsBlendCornersAndSplicesSlopeLanes()
    {
        var tilesetProfile = new BaseGameTilesetProfiles().BuildTilesetProfiles()[BaseGameTilesetProfiles.MinesAndCaverns];
        var layoutProfile = new StandardLayoutProfiles().BuildLayoutProfiles()[StandardLayoutProfiles.Complex];
        var model = TilesetTestSource.LoadTileset(tilesetProfile.TilesetResref);
        var composition = new DungeonComposition { Content = null, Tileset = tilesetProfile, Layout = layoutProfile };

        var resolvedCount = 0;
        var blendCornerSeeds = 0;
        var slopeLaneSeeds = 0;
        const int size = 24;

        for (var seed = 9200; seed < 9260; seed++)
        {
            var parameters = composition.BuildLayoutParameters();
            parameters.EntranceCount = 1;
            parameters.ExitCount = 1;
            parameters.DoorTransitions = true;

            parameters.ReliefBlendTerrain.Should().Be("GentleSlope", "the MinesAndCaverns profile declares its blend terrain");
            parameters.RampCrosser.Should().Be("Slope", "the MinesAndCaverns profile declares its alternate ramp crosser");

            var solved = LayoutSolver.Solve(parameters, model, size, size, seed, tilesetProfile.PrimaryOpenTerrain);
            if (!solved.Success) continue;
            resolvedCount++;

            var corners = solved.Layout.Corners;
            var anyBlend = false;
            for (var x = 0; x <= corners.Width && !anyBlend; x++)
            for (var y = 0; y <= corners.Height && !anyBlend; y++)
            {
                if (corners.Labels[x, y] == "GentleSlope") anyBlend = true;
            }
            if (anyBlend) blendCornerSeeds++;

            var crossers = solved.Layout.Crossers;
            var anySlope = false;
            for (var x = 0; x < crossers.Width && !anySlope; x++)
            for (var y = 0; y < crossers.Height && !anySlope; y++)
            for (var slot = 0; slot < 4 && !anySlope; slot++)
            {
                if (string.Equals(crossers.GetEdge(x, y, slot), "Slope", StringComparison.Ordinal)) anySlope = true;
            }
            if (anySlope) slopeLaneSeeds++;
        }

        resolvedCount.Should().BeGreaterThan(0, "at least some seeds must generate successfully");
        blendCornerSeeds.Should().BeGreaterThan(0,
            "across 60 seeds the relief pass must flip at least one Floor corner to GentleSlope in a real tdm01 layout");
        slopeLaneSeeds.Should().BeGreaterThan(0,
            "across 60 seeds the relief pass must splice at least one Slope lane in a real tdm01 layout");
    }

    /// <summary>
    /// The exterior twin (Desert/ttd01 and Forest/ttf01, the wave that motivated profile-declared
    /// ramp crossers on OUTDOOR ground): each profile declares its own ramp-lane vocabulary
    /// (RampCrosser("Dunes") / RampCrosser("Slope")) plus MaxElevationRegions/MaxReliefRegions(2),
    /// and the real Complex composition must actually write at least one declared ramp-crosser lane
    /// AND stamp the tileset's baked-mesh 1x1 raised "Ramp" group (a ReliefPiece pin -- it can never
    /// arrive via the corner/edge resolver) across the seed sweep. This is the placement proof
    /// behind the census's TerrainRelief/ReliefPiece credits for these tilesets.
    /// </summary>
    [TestCase(BaseGameTilesetProfiles.Desert, "Dunes")]
    [TestCase(BaseGameTilesetProfiles.Forest, "Slope")]
    public void RealExteriorComplexComposition_SplicesRampLanesAndStampsRampPieces(string tilesetKey, string rampCrosser)
    {
        var tilesetProfile = new BaseGameTilesetProfiles().BuildTilesetProfiles()[tilesetKey];
        var layoutProfile = new StandardLayoutProfiles().BuildLayoutProfiles()[StandardLayoutProfiles.Complex];
        var model = TilesetTestSource.LoadTileset(tilesetProfile.TilesetResref);
        var composition = new DungeonComposition { Content = null, Tileset = tilesetProfile, Layout = layoutProfile };

        var rampPieceTileIds = model.Groups
            .Where(g => g.Name == "Ramp")
            .SelectMany(g => g.TileIds.Where(id => id >= 0))
            .ToHashSet();
        rampPieceTileIds.Should().NotBeEmpty($"{tilesetProfile.TilesetResref} must carry the configured Ramp group");

        var resolvedCount = 0;
        var rampLaneSeeds = 0;
        var rampPiecePinCount = 0;
        const int size = 24;

        for (var seed = 9300; seed < 9360; seed++)
        {
            var parameters = composition.BuildLayoutParameters();
            parameters.EntranceCount = 1;
            parameters.ExitCount = 1;
            parameters.DoorTransitions = true;

            parameters.RampCrosser.Should().Be(rampCrosser, $"the {tilesetKey} profile declares its ramp crosser");
            parameters.ReliefRegions.Should().BeGreaterThan(0, $"{tilesetKey}/Complex must actually request relief regions");

            var solved = LayoutSolver.Solve(parameters, model, size, size, seed, tilesetProfile.PrimaryOpenTerrain);
            if (!solved.Success) continue;
            resolvedCount++;

            var crossers = solved.Layout.Crossers;
            var anyLane = false;
            for (var x = 0; x < crossers.Width && !anyLane; x++)
            for (var y = 0; y < crossers.Height && !anyLane; y++)
            for (var slot = 0; slot < 4 && !anyLane; slot++)
            {
                if (string.Equals(crossers.GetEdge(x, y, slot), rampCrosser, StringComparison.Ordinal)) anyLane = true;
            }
            if (anyLane) rampLaneSeeds++;

            rampPiecePinCount += solved.Resolved.Tiles.Count(t => rampPieceTileIds.Contains(t.TileId));
        }

        resolvedCount.Should().BeGreaterThan(0, "at least some seeds must generate successfully");
        rampLaneSeeds.Should().BeGreaterThan(0,
            $"across 60 seeds the elevation/relief passes must write at least one {rampCrosser} lane in a real {tilesetProfile.TilesetResref} layout");
        rampPiecePinCount.Should().BeGreaterThan(0,
            $"across 60 seeds LayoutGroupStamper's ReliefPiece kind must stamp the baked-mesh Ramp group at least once in a real {tilesetProfile.TilesetResref} layout");
    }

    /// <summary>
    /// Positive placement proof for BaseGameTilesetProfiles.ForestRural (ttf01's PaletteVariant
    /// closing the previously-unwired RuralWater/RuralTrees raised-bank census bucket): a real
    /// Complex composition (which requests PoolRegions=2/ReliefRegions=2, matching this profile's own
    /// MaxPoolRegions(2)/MaxReliefRegions(2) caps) must actually paint RuralWater corners via
    /// LayoutElevationPoolPainter's irregular pool-bank grower (AccentTerrain) AND RuralTrees corners
    /// via LayoutReliefPainter's blend flip (ReliefBlendTerrain) in real generated layouts -- not just
    /// theoretically reachable per TileCoverageCensusTests' PoolBank/TerrainRelief shape mirrors.
    /// </summary>
    [Test]
    public void RealForestRuralComplexComposition_PaintsRuralWaterAndRuralTreesBanks()
    {
        var tilesetProfile = new BaseGameTilesetProfiles().BuildTilesetProfiles()[BaseGameTilesetProfiles.ForestRural];
        var layoutProfile = new StandardLayoutProfiles().BuildLayoutProfiles()[StandardLayoutProfiles.Complex];
        var model = TilesetTestSource.LoadTileset(tilesetProfile.TilesetResref);
        var composition = new DungeonComposition { Content = null, Tileset = tilesetProfile, Layout = layoutProfile };

        var resolvedCount = 0;
        var ruralWaterSeeds = 0;
        var ruralTreesSeeds = 0;
        const int size = 24;

        for (var seed = 9400; seed < 9460; seed++)
        {
            var parameters = composition.BuildLayoutParameters();
            parameters.EntranceCount = 1;
            parameters.ExitCount = 1;
            parameters.DoorTransitions = true;

            parameters.PoolRegions.Should().BeGreaterThan(0, "ForestRural/Complex must actually request pool regions");
            parameters.ReliefRegions.Should().BeGreaterThan(0, "ForestRural/Complex must actually request relief regions");

            var solved = LayoutSolver.Solve(parameters, model, size, size, seed, tilesetProfile.PrimaryOpenTerrain);
            if (!solved.Success) continue;
            resolvedCount++;

            var corners = solved.Layout.Corners;
            var sawWater = false;
            var sawTrees = false;
            for (var x = 0; x <= corners.Width && !(sawWater && sawTrees); x++)
            for (var y = 0; y <= corners.Height && !(sawWater && sawTrees); y++)
            {
                if (string.Equals(corners.Labels[x, y], "RuralWater", StringComparison.Ordinal)) sawWater = true;
                if (string.Equals(corners.Labels[x, y], "RuralTrees", StringComparison.Ordinal)) sawTrees = true;
            }
            if (sawWater) ruralWaterSeeds++;
            if (sawTrees) ruralTreesSeeds++;
        }

        resolvedCount.Should().BeGreaterThan(0, "at least some seeds must generate successfully");
        ruralWaterSeeds.Should().BeGreaterThan(0,
            $"across 60 seeds the pool-bank pass must paint at least one RuralWater corner in a real {tilesetProfile.TilesetResref}/ForestRural layout (got 0/{resolvedCount})");
        ruralTreesSeeds.Should().BeGreaterThan(0,
            $"across 60 seeds the relief-blend pass must paint at least one RuralTrees corner in a real {tilesetProfile.TilesetResref}/ForestRural layout (got 0/{resolvedCount})");
    }

    /// <summary>
    /// Positive placement proof for BaseGameTilesetProfiles.ForestMarsh (ttf01's PaletteVariant wiring
    /// Marsh as a plain flat AccentTerrain -- see that profile's own doc comment): a real Halls
    /// composition must actually paint Marsh corners via LayoutAccentPainter.PaintAccents' blob-patch
    /// pass. Halls (like every RoomsAndCorridors-style StandardLayoutProfiles entry) does not itself
    /// declare AccentDensity &gt; 0 (only Organic/Warren do, neither a Forest-exterior pairing), so this
    /// test forces the same knob value Organic uses directly on the built parameters -- the identical
    /// technique RealForestRuralComplexComposition_PaintsRuralWaterAndRuralTreesBanks above uses to
    /// force EntranceCount/ExitCount/DoorTransitions -- to exercise the SAME production
    /// LayoutAccentPainter pass a future Organic/Warren-paired ttf01 composition would also drive.
    /// </summary>
    [Test]
    public void RealForestMarshComposition_PaintsMarshAccentPatches()
    {
        var tilesetProfile = new BaseGameTilesetProfiles().BuildTilesetProfiles()[BaseGameTilesetProfiles.ForestMarsh];
        var layoutProfile = new StandardLayoutProfiles().BuildLayoutProfiles()[StandardLayoutProfiles.Halls];
        var model = TilesetTestSource.LoadTileset(tilesetProfile.TilesetResref);
        var composition = new DungeonComposition { Content = null, Tileset = tilesetProfile, Layout = layoutProfile };

        var resolvedCount = 0;
        var marshSeeds = 0;
        const int size = 24;

        for (var seed = 9500; seed < 9560; seed++)
        {
            var parameters = composition.BuildLayoutParameters();
            parameters.EntranceCount = 1;
            parameters.ExitCount = 1;
            parameters.DoorTransitions = true;
            // DungeonComposition.BuildLayoutParameters computes MacroLayoutParameters.AccentTerrain
            // from the LAYOUT profile's own AccentDensity at build time (zeroing it out when
            // AccentDensity is 0, see that method's own doc comment) -- Halls doesn't declare
            // AccentDensity, so both must be forced here, not just AccentDensity, to actually exercise
            // LayoutAccentPainter.PaintAccents' gate.
            parameters.AccentDensity = 0.06;
            parameters.AccentTerrain = tilesetProfile.AccentTerrain;

            var solved = LayoutSolver.Solve(parameters, model, size, size, seed, tilesetProfile.PrimaryOpenTerrain);
            if (!solved.Success) continue;
            resolvedCount++;

            var corners = solved.Layout.Corners;
            var sawMarsh = false;
            for (var x = 0; x <= corners.Width && !sawMarsh; x++)
            for (var y = 0; y <= corners.Height && !sawMarsh; y++)
            {
                if (string.Equals(corners.Labels[x, y], "Marsh", StringComparison.Ordinal)) sawMarsh = true;
            }
            if (sawMarsh) marshSeeds++;
        }

        // Measured: 60/60 seeds resolve, and every single one paints at least one Marsh corner (a
        // forced AccentDensity=0.06 on a 24x24 grid gives the blob-patch pass ample open Forest space
        // to seed from).
        resolvedCount.Should().BeGreaterThan(0, "at least some seeds must generate successfully");
        marshSeeds.Should().BeGreaterThan(0,
            $"across 60 seeds the blob-patch pass must paint at least one Marsh corner in a real {tilesetProfile.TilesetResref}/ForestMarsh layout (got 0/{resolvedCount})");
    }
}
