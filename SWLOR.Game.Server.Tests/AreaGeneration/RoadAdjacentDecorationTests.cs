using System;
using System.Collections.Generic;
using System.Linq;
using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Feature.DungeonDefinition;
using SWLOR.Game.Server.Service.AreaGenerationService;
using SWLOR.Game.Server.Service.AreaGenerationService.Decoration;
using SWLOR.Game.Server.Service.AreaGenerationService.Tileset;

namespace SWLOR.Game.Server.Tests.AreaGeneration;

/// <summary>
/// Round-4 decoration-anchoring pass: streetlight/kiosk-class fcx01 decoration should cluster along
/// carved Road lanes (LayoutRoadCarver), not just the owning room's own shape -- see
/// DungeonDecorationPlanner.IsRoadAdjacent/TryResolveContext and the FutCity/FutCityPlaza palette's
/// CorridorSide entries in BaseGameTilesetProfiles. Mirrors RoadCarverTests' own conventions: real
/// fcx01 .set data, the same "Complex" (RoomsAndCorridors/Tunnel) parameter shape the shipped
/// futcity_plaza:complex composition actually uses.
/// </summary>
public class RoadAdjacentDecorationTests
{
    private static readonly Dictionary<string, DungeonTilesetProfile> OnboardedProfiles = BuildOnboardedProfiles();

    private static Dictionary<string, DungeonTilesetProfile> BuildOnboardedProfiles()
    {
        // FutCityPlaza is a PaletteVariant profile (see BaseGameTilesetProfiles) -- its own
        // Decorations/Vignettes lists are empty until DungeonTilesetPaletteInheritance.Apply copies
        // the base FutCity profile's curated palette onto it, exactly as the real boot-time cache
        // (and DecorGen's own dump tool) does. Skipping this step silently produces an empty palette
        // for FutCityPlaza and zero decorations placed.
        var profiles = new BaseGameTilesetProfiles().BuildTilesetProfiles();
        DungeonTilesetPaletteInheritance.Apply(profiles);
        return profiles;
    }

    private static readonly DungeonDetail MineCaveDetail =
        new MineCaveDungeonDefinition().BuildDungeons()[MineCaveDungeonDefinition.ThemeKey];

    private static TilesetModel LoadTileset(string tilesetResref) => TilesetTestSource.LoadTileset(tilesetResref);

    private static MacroLayoutParameters FutCityParameters(DungeonTilesetProfile profile, int width = 24)
    {
        return new MacroLayoutParameters
        {
            Style = DungeonLayoutStyle.RoomsAndCorridors,
            CorridorMode = CorridorMode.Tunnel,
            MinRooms = 6,
            MaxRooms = 9,
            MinRoomCornerSize = 3,
            MaxRoomCornerSize = 5,
            CorridorWidth = Math.Max(2, profile.MinimumOpeningWidth),
            LoopFactor = 0.3,
            Width = width,
            Height = width,
            SolidTerrain = profile.SolidTerrainOverride,
            OpenTerrain = profile.PrimaryOpenTerrain,
            SetPieces = profile.SetPieces,
            ExitGroups = profile.ExitGroups,
            FeatureTiles = profile.FeatureTiles,
            DoorSlotCrossers = profile.DoorSlotCrossers,
            RoadLanes = 6,
            RoadCrosser = profile.RoadCrosser,
        };
    }

    private static readonly HashSet<string> StreetlightKioskResrefs = new(StringComparer.OrdinalIgnoreCase)
    {
        "_mdrn_pl_lights3", "swd_streel01", "swd2_kiosk004"
    };

    // ============================================================
    // IsRoadAdjacent: direct unit coverage.
    // ============================================================

    [Test]
    public void IsRoadAdjacent_EmptyRoadCrosser_ReturnsFalse()
    {
        var layout = new ResolvedLayout { Width = 4, Height = 4, Crossers = new EdgeCrosserGrid(4, 4) };
        DungeonDecorationPlanner.IsRoadAdjacent((1, 1), layout, "").Should().BeFalse();
    }

    [Test]
    public void IsRoadAdjacent_NullCrossers_ReturnsFalse()
    {
        var layout = new ResolvedLayout { Width = 4, Height = 4, Crossers = null };
        DungeonDecorationPlanner.IsRoadAdjacent((1, 1), layout, "Routes").Should().BeFalse();
    }

    [Test]
    public void IsRoadAdjacent_TileItselfCarriesRoadEdge_ReturnsTrue()
    {
        var crossers = new EdgeCrosserGrid(4, 4);
        crossers.SetEdge(1, 1, EdgeSlot.Right, "Routes");
        var layout = new ResolvedLayout { Width = 4, Height = 4, Crossers = crossers };

        DungeonDecorationPlanner.IsRoadAdjacent((1, 1), layout, "Routes").Should().BeTrue();
    }

    [Test]
    public void IsRoadAdjacent_DiagonalNeighborCarriesRoadEdge_ReturnsTrue()
    {
        var crossers = new EdgeCrosserGrid(4, 4);
        // (2,2) carries a road edge; (1,1) is its Chebyshev-1 diagonal neighbor.
        crossers.SetEdge(2, 2, EdgeSlot.Top, "Routes");
        var layout = new ResolvedLayout { Width = 4, Height = 4, Crossers = crossers };

        DungeonDecorationPlanner.IsRoadAdjacent((1, 1), layout, "Routes").Should().BeTrue();
    }

    [Test]
    public void IsRoadAdjacent_TwoTilesAway_ReturnsFalse()
    {
        var crossers = new EdgeCrosserGrid(6, 6);
        crossers.SetEdge(4, 4, EdgeSlot.Top, "Routes");
        var layout = new ResolvedLayout { Width = 6, Height = 6, Crossers = crossers };

        DungeonDecorationPlanner.IsRoadAdjacent((1, 1), layout, "Routes").Should().BeFalse();
    }

    [Test]
    public void IsRoadAdjacent_DifferentCrosserName_ReturnsFalse()
    {
        var crossers = new EdgeCrosserGrid(4, 4);
        crossers.SetEdge(1, 1, EdgeSlot.Right, "Fence");
        var layout = new ResolvedLayout { Width = 4, Height = 4, Crossers = crossers };

        DungeonDecorationPlanner.IsRoadAdjacent((1, 1), layout, "Routes").Should().BeFalse();
    }

    // ============================================================
    // Full pipeline: real fcx01 Road lanes + the real curated palette.
    // ============================================================

    /// <summary>
    /// Data-derived band, not an arbitrary guess: the _scratch_decor/tilecomp_report.py harness
    /// measured this fraction directly against the real onboarded FutCity/FutCityPlaza compositions
    /// ("minecave:futcity:complex"/"minecave:futcity_plaza:complex", the shipped showcase pairing, 30
    /// seeds/profile at the showcase size 20) before vs after the road-anchored composition pass:
    /// 0.38 (before: Manhattan-only road carving, no road-anchored decoration context) -> 0.81
    /// (after: BFS street routing + IsRoadAdjacent CorridorSide anchoring + street-furniture
    /// CorridorSide palette entries), against a hand-built fcx01 reference of 0.92 (19 real decorated
    /// Smuggler's Moon/Nar Shaddaa areas, decoration_evidence/modunpack). This test's own 30-seed run
    /// at size 16 measures 0.944-0.945 for both profiles. 0.70 is a conservative floor well below the
    /// measured values that still cleanly rejects every pre-change configuration measured (0.38-0.52)
    /// without being seed-sensitive to the exact point estimate.
    ///
    /// Re-measured after LayoutRoadCarver.CarveRoads was reordered to run BEFORE
    /// LayoutGroupStamper.Stamp (see that class's own doc comment: roads now carve first, buildings
    /// prefer a street-fronting site): this test's own 30-seed run at size 16 now measures 0.913
    /// (futcity) / 0.917 (futcity_plaza) -- essentially unchanged from the pre-reorder 0.944-0.945,
    /// still well clear of the 0.70 floor.
    /// </summary>
    private const double MinRoadAdjacencyFraction = 0.70;

    [TestCase(BaseGameTilesetProfiles.FutCity)]
    [TestCase(BaseGameTilesetProfiles.FutCityPlaza)]
    public void Plan_StreetlightKioskDecoration_MeetsRoadAdjacencyBand(string profileKey)
    {
        var profile = OnboardedProfiles[profileKey];
        var model = LoadTileset(profile.TilesetResref);

        var totalStreetlightKiosk = 0;
        var adjacentToRoad = 0;

        for (var seed = 31000; seed < 31030; seed++)
        {
            var rng = new Random(seed);
            var parameters = FutCityParameters(profile, width: 16);

            var macro = MacroLayoutGenerator.Generate(parameters, rng, model);
            macro.Seed = seed;
            if (!TileResolver.TryResolve(model, macro, rng, out var resolved, out _))
                continue;

            var plan = DungeonDecorationPlanner.Plan(resolved, profile, MineCaveDetail, 100);

            var roadTiles = new HashSet<(int X, int Y)>();
            for (var y = 0; y < resolved.Height; y++)
            for (var x = 0; x < resolved.Width; x++)
            for (var slot = 0; slot < 4; slot++)
            {
                if (string.Equals(resolved.Crossers.GetEdge(x, y, slot), "Routes", StringComparison.OrdinalIgnoreCase))
                    roadTiles.Add((x, y));
            }

            if (roadTiles.Count == 0) continue;

            foreach (var decoration in plan)
            {
                if (!StreetlightKioskResrefs.Contains(decoration.Resref)) continue;

                totalStreetlightKiosk++;
                var tileX = (int)(decoration.Position.X / 10f);
                var tileY = (int)(decoration.Position.Y / 10f);

                var isAdjacent = false;
                for (var dx = -1; dx <= 1 && !isAdjacent; dx++)
                for (var dy = -1; dy <= 1 && !isAdjacent; dy++)
                    if (roadTiles.Contains((tileX + dx, tileY + dy)))
                        isAdjacent = true;

                if (isAdjacent) adjacentToRoad++;
            }
        }

        totalStreetlightKiosk.Should().BeGreaterThan(0,
            $"{profileKey} should place at least one streetlight/kiosk-class decoration across 30 seeds");

        var fraction = (double)adjacentToRoad / totalStreetlightKiosk;
        TestContext.WriteLine($"{profileKey}: {adjacentToRoad}/{totalStreetlightKiosk} streetlight/kiosk placements " +
                               $"within 1 tile of a Road edge ({fraction:F3})");
        fraction.Should().BeGreaterOrEqualTo(MinRoadAdjacencyFraction,
            $"{profileKey}: road-anchored decoration should measurably cluster along carved Road lanes");
    }

    /// <summary>
    /// A tile within one cell of a carved Road resolves to CorridorSide (the "street-side" bucket)
    /// regardless of the owning room's own shape -- proven directly against TryResolveContext's real
    /// decision, not just inferred from the aggregate adjacency fraction above.
    /// </summary>
    [Test]
    public void Plan_RoadAdjacentWallEligibleTile_ProducesCorridorSideContext()
    {
        var profile = OnboardedProfiles[BaseGameTilesetProfiles.FutCity];
        var model = LoadTileset(profile.TilesetResref);

        var foundRoadAdjacentCorridorSide = false;

        for (var seed = 31100; seed < 31115 && !foundRoadAdjacentCorridorSide; seed++)
        {
            var rng = new Random(seed);
            var parameters = FutCityParameters(profile, width: 16);
            var macro = MacroLayoutGenerator.Generate(parameters, rng, model);
            macro.Seed = seed;
            if (!TileResolver.TryResolve(model, macro, rng, out var resolved, out _))
                continue;

            var plan = DungeonDecorationPlanner.Plan(resolved, profile, MineCaveDetail, 100);

            var roadTiles = new HashSet<(int X, int Y)>();
            for (var y = 0; y < resolved.Height; y++)
            for (var x = 0; x < resolved.Width; x++)
            for (var slot = 0; slot < 4; slot++)
                if (string.Equals(resolved.Crossers.GetEdge(x, y, slot), "Routes", StringComparison.OrdinalIgnoreCase))
                    roadTiles.Add((x, y));

            foreach (var decoration in plan.Where(d => d.Context == DecorationContext.CorridorSide))
            {
                var tileX = (int)(decoration.Position.X / 10f);
                var tileY = (int)(decoration.Position.Y / 10f);
                for (var dx = -1; dx <= 1; dx++)
                for (var dy = -1; dy <= 1; dy++)
                    if (roadTiles.Contains((tileX + dx, tileY + dy)))
                        foundRoadAdjacentCorridorSide = true;
            }
        }

        foundRoadAdjacentCorridorSide.Should().BeTrue(
            "at least one CorridorSide decoration across 15 seeds should land within 1 tile of a carved Road edge");
    }

    // ============================================================
    // Group-tile (stamped multi-tile set piece) share band.
    // ============================================================

    /// <summary>
    /// Data-derived band: at the shipped showcase size 20, the _scratch_decor harness measured a
    /// hand-built fcx01 group-tile share of 0.152 (19 decorated reference areas) vs 0.0 generated
    /// before the SetPieceRoomCornerFloor room-size pass (Complex's own MaxRoomCornerSize=5 caps rooms
    /// at 4x4 tiles -- physically too small for ANY multi-tile group's footprint + margin + spare
    /// center tile) and 0.0217-0.0227 after it (mean 8.7-9.1 group tiles/area across 30 seeds/profile).
    ///
    /// Reordered again since (LayoutRoadCarver.CarveRoads now runs BEFORE LayoutGroupStamper.Stamp, so
    /// buildings can prefer road-adjacent sites -- see LayoutRoadCarver's own class doc comment):
    /// measured 0.0143 (172/12000, 29/30 seeds with a stamped group) with the final design (road
    /// anchors exclude building-candidate room centers -- see CarveRoads -- plus the Stamp-side road
    /// exclusion/preference and CarveSpurs fallback). A real regression was measured and fixed along
    /// the way: naively including every room's center as a road anchor pre-Stamp collapsed this to
    /// 0.001 (roads claimed the exact tight interior Stamp needed); the current anchor exclusion
    /// recovers most of the pre-reorder share while pushing building-road frontage from ~0 to ~0.97+
    /// (see _scratch_decor/measure_generated_frontage.py) -- the actual goal of this pass. The
    /// remaining gap to hand-built (and to the pre-reorder 0.0217-0.0227) is partly the same measured
    /// geometric ceiling as before (LayoutParameterConstraints.RoomSizeBounds caps rooms at corner size
    /// 6 for a size-20 RoomsAndCorridors area, fitting only 2x2 groups) and partly the frontage
    /// preference itself trading a few raw placements for near-universal street frontage. The floors
    /// below (aggregate share >= 0.008, at least 20/30 seeds with a nonzero group count) sit well under
    /// the measured 0.0143/29-of-30 while cleanly rejecting a regressed 0.001-share state.
    /// </summary>
    [TestCase(BaseGameTilesetProfiles.FutCity)]
    [TestCase(BaseGameTilesetProfiles.FutCityPlaza)]
    public void Stamp_MultiTileTowers_MeetGroupShareBand(string profileKey)
    {
        var profile = OnboardedProfiles[profileKey];
        var model = LoadTileset(profile.TilesetResref);

        // Tile IDs belonging to a multi-tile (2x2+) group configured as one of this profile's own
        // SetPieces -- the "group tiles" the tile-composition divergence table counts.
        var groupTileIds = new HashSet<int>();
        foreach (var group in model.Groups)
        {
            if (group.Rows * group.Columns <= 1) continue;
            if (!profile.SetPieces.Keys.Any(k => string.Equals(k, group.Name, StringComparison.OrdinalIgnoreCase)))
                continue;
            foreach (var tileId in group.TileIds)
                if (tileId >= 0)
                    groupTileIds.Add(tileId);
        }

        groupTileIds.Should().NotBeEmpty($"{profileKey} must configure at least one multi-tile SetPiece group");

        const int size = 20;
        const int seedCount = 30;
        var totalTiles = 0;
        var totalGroupTiles = 0;
        var seedsWithGroup = 0;
        var solveFailures = 0;

        for (var seed = 32000; seed < 32000 + seedCount; seed++)
        {
            var layoutProfile = new StandardLayoutProfiles().BuildLayoutProfiles()[StandardLayoutProfiles.Complex];
            var composition = new DungeonComposition { Content = MineCaveDetail, Tileset = profile, Layout = layoutProfile };
            var parameters = composition.BuildLayoutParameters();
            parameters.EntranceCount = 1;
            parameters.ExitCount = 1;
            parameters.DoorTransitions = true;

            var solved = LayoutSolver.Solve(parameters, model, size, size, seed, profile.PrimaryOpenTerrain);
            if (!solved.Success)
            {
                solveFailures++;
                continue;
            }

            var resolved = solved.Resolved;
            totalTiles += resolved.Width * resolved.Height;
            var groupTilesThisSeed = resolved.Tiles.Count(t => groupTileIds.Contains(t.TileId));
            totalGroupTiles += groupTilesThisSeed;
            if (groupTilesThisSeed > 0)
                seedsWithGroup++;
        }

        solveFailures.Should().Be(0, $"{profileKey} at size {size} should solve every seed");

        var share = (double)totalGroupTiles / totalTiles;
        TestContext.WriteLine($"{profileKey}: group-tile share {share:F4} ({totalGroupTiles}/{totalTiles}), " +
                               $"{seedsWithGroup}/{seedCount} seeds with a stamped multi-tile group");
        share.Should().BeGreaterOrEqualTo(0.008,
            $"{profileKey}: stamped multi-tile towers should hold a meaningful group-tile share at size {size}");
        seedsWithGroup.Should().BeGreaterOrEqualTo(20,
            $"{profileKey}: most size-{size} seeds should stamp at least one multi-tile tower");
    }
}
