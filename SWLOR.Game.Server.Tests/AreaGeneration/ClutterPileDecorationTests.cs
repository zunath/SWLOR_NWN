using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Feature.DungeonDefinition;
using SWLOR.Game.Server.Service.AreaGenerationService;

namespace SWLOR.Game.Server.Tests.AreaGeneration;

/// <summary>
/// Round-5 decoration-quality acceptance suite: hand-built city dressing is CLUSTERS/PILES, not a
/// sparse even grid of identical street furniture. Mined reference (19 decorated hand-built fcx01
/// areas, _scratch round-5 harness): 1.61 decorative placeables per tile aggregate (flagship
/// promenades 2.8-4.6), all-placeable nearest-neighbor median 1.6m (p25 0.5m), 75% of decoratives
/// within 3m of another decorative, and a palette backbone of crates/containers/barrels/rubble/
/// ground decals with street lamps at only ~4% of placements. The delivered review module's
/// generated areas measured the OPPOSITE on every axis (0.10/tile, NN median 10m, within-3m ~0.1,
/// top-3 resref share 0.5-0.6 held by kiosk/lamp fixtures) -- the reported "vast empty plaza with
/// an even grid of glowing kiosks" quality failure.
///
/// These tests hold the planner's fcx01 output to the matched distribution: total-tile density for
/// the Packed pairing (rooms cover roughly half the grid, like hand-built areas' walkable share),
/// per-ROOM-tile density for both pairings (a Halls fcx01 area is ~70/400 tiles of chamber and the
/// rest solid chasm, so total-tile density is structurally capped -- the walkable chambers
/// themselves must dress at flagship density), tight nearest-neighbor statistics, a bounded top-3
/// resref share, zero stand-alone ground decals, structure-hugging piles at stamped tower bases,
/// and strict role gating (Landmark one-offs never free-float; clutter piles only exist for
/// palettes that curate Clutter-role entries).
/// </summary>
public class ClutterPileDecorationTests
{
    private const int SeedBase = 7001;
    private const int SeedCount = 10;

    // The STANDARD clean-city palette's decals (signage/markings). The dirt decals
    // (_mdrn_pl_dirtyg*) moved to the "ruined" decoration profile in the round-6 destruction split
    // -- see UrbanDressingCompositionTests for the ruined-profile coverage.
    private static readonly HashSet<string> GroundDecalResrefs = new(StringComparer.OrdinalIgnoreCase)
    {
        "swd_floorm01", "swd_flormh01", "swd_florrd01"
    };

    private static (DungeonDetail Detail, DungeonTilesetProfile Tileset, DungeonLayoutProfile Layout, TilesetModel Model)
        Composition(string themeKey, string tilesetKey, string layoutKey)
    {
        var themes = new Dictionary<string, DungeonDetail>();
        foreach (var definition in new IDungeonListDefinition[]
                 { new MineCaveDungeonDefinition(), new AlienRuinDungeonDefinition() })
        foreach (var (k, v) in definition.BuildDungeons())
            themes[k] = v;

        var tilesets = new BaseGameTilesetProfiles().BuildTilesetProfiles();
        foreach (var (k, v) in new StandardTilesetProfiles().BuildTilesetProfiles())
            tilesets.TryAdd(k, v);
        DungeonTilesetPaletteInheritance.Apply(tilesets);
        var layouts = new StandardLayoutProfiles().BuildLayoutProfiles();

        var tileset = tilesets[tilesetKey];
        return (themes[themeKey], tileset, layouts[layoutKey], TilesetTestSource.LoadTileset(tileset.TilesetResref));
    }

    private static (ResolvedLayout Layout, List<PlannedDecoration> Plan) PlanFor(
        (DungeonDetail Detail, DungeonTilesetProfile Tileset, DungeonLayoutProfile Layout, TilesetModel Model) c,
        int seed, int size)
    {
        var composition = new DungeonComposition { Content = c.Detail, Tileset = c.Tileset, Layout = c.Layout };
        var parameters = composition.BuildLayoutParameters();
        parameters.EntranceCount = 1;
        parameters.ExitCount = 1;
        parameters.DoorTransitions = true;

        var result = LayoutSolver.Solve(parameters, c.Model, size, size, seed, c.Tileset.PrimaryOpenTerrain);
        result.Success.Should().BeTrue($"seed {seed} must solve: {result.FailureReason}");

        return (result.Resolved, DungeonDecorationPlanner.Plan(result.Resolved, c.Tileset, c.Detail, 100));
    }

    private static (double DensityPerTile, double DensityPerRoomTile, double NnMedian, double Within3Fraction,
        double Top3Share, int DistinctResrefs)
        Measure(ResolvedLayout layout, List<PlannedDecoration> plan, int size)
    {
        var roomTiles = layout.Rooms.Where(r => !r.IsSetPiece).SelectMany(r => r.Tiles).Distinct().Count();

        var nn = new List<double>();
        for (var i = 0; i < plan.Count; i++)
        {
            var best = double.MaxValue;
            for (var j = 0; j < plan.Count; j++)
            {
                if (i == j)
                    continue;
                var d = Vector2.Distance(
                    new Vector2(plan[i].Position.X, plan[i].Position.Y),
                    new Vector2(plan[j].Position.X, plan[j].Position.Y));
                if (d < best)
                    best = d;
            }
            if (best < double.MaxValue)
                nn.Add(best);
        }

        var sortedNn = nn.OrderBy(d => d).ToList();
        var nnMedian = sortedNn.Count > 0 ? sortedNn[sortedNn.Count / 2] : double.MaxValue;
        var within3 = nn.Count > 0 ? nn.Count(d => d <= 3.0) / (double)nn.Count : 0.0;

        var counts = plan.GroupBy(p => p.Resref).Select(g => g.Count()).OrderByDescending(x => x).ToList();
        var top3 = plan.Count > 0 ? counts.Take(3).Sum() / (double)plan.Count : 0.0;

        return (plan.Count / (double)(size * size),
            roomTiles > 0 ? plan.Count / (double)roomTiles : 0.0,
            nnMedian, within3, top3, counts.Count);
    }

    // ============================================================
    // Distribution matching: fcx01 Packed (the 32x32 showcase pairing at review size 20).
    // ============================================================

    [Test]
    public void FutCityPacked_At20_MatchesHandBuiltDressingDistribution()
    {
        var c = Composition(MineCaveDungeonDefinition.ThemeKey, BaseGameTilesetProfiles.FutCity, StandardLayoutProfiles.Packed);
        var densities = new List<double>();
        var roomDensities = new List<double>();
        var nnMedians = new List<double>();
        var within3s = new List<double>();
        var top3s = new List<double>();

        for (var i = 0; i < SeedCount; i++)
        {
            var (layout, plan) = PlanFor(c, SeedBase + i, 20);
            var m = Measure(layout, plan, 20);
            densities.Add(m.DensityPerTile);
            roomDensities.Add(m.DensityPerRoomTile);
            nnMedians.Add(m.NnMedian);
            within3s.Add(m.Within3Fraction);
            top3s.Add(m.Top3Share);
        }

        // Measured 1.38 mean (hand-built aggregate 1.61); the delivered failure state measured 0.10.
        densities.Average().Should().BeInRange(1.2, 2.5,
            $"packed city density per total tile (got {densities.Average():F3})");
        roomDensities.Average().Should().BeInRange(1.5, 4.5,
            $"packed city density per room tile (got {roomDensities.Average():F3})");
        // Hand-built all-NN median 1.6m; the delivered failure state measured 10.0m.
        nnMedians.Average().Should().BeLessThanOrEqualTo(3.0,
            $"all-placeable NN median (got {nnMedians.Average():F2})");
        // Hand-built within-3m fraction 0.75; the delivered failure state measured ~0.10.
        within3s.Average().Should().BeGreaterThanOrEqualTo(0.55,
            $"within-3m fraction (got {within3s.Average():F3})");
        // The delivered failure state measured 0.51-0.61 held by three street-furniture fixtures.
        top3s.Average().Should().BeLessThanOrEqualTo(0.35,
            $"top-3 resref share mean (got {top3s.Average():F3})");
        top3s.Max().Should().BeLessThanOrEqualTo(0.45,
            $"top-3 resref share worst seed (got {top3s.Max():F3})");
    }

    // ============================================================
    // Distribution matching: fcx01 Halls (the exact user-reviewed composition).
    // ============================================================

    [Test]
    public void FutCityHalls_At20_MatchesHandBuiltDressingDistribution()
    {
        var c = Composition(AlienRuinDungeonDefinition.ThemeKey, BaseGameTilesetProfiles.FutCity, StandardLayoutProfiles.Halls);
        var roomDensities = new List<double>();
        var nnMedians = new List<double>();
        var within3s = new List<double>();
        var top3s = new List<double>();

        for (var i = 0; i < SeedCount; i++)
        {
            var (layout, plan) = PlanFor(c, SeedBase + 100 + i, 20);
            var m = Measure(layout, plan, 20);
            roomDensities.Add(m.DensityPerRoomTile);
            nnMedians.Add(m.NnMedian);
            within3s.Add(m.Within3Fraction);
            top3s.Add(m.Top3Share);
        }

        // A Halls fcx01 grid is ~70/400 tiles of walkable chamber (the rest is the holes chasm and
        // boundary art), so TOTAL-tile density is structurally capped near 0.65 -- the walkable
        // chambers themselves are what the reviewer sees, and they must dress at the hand-built
        // flagship band (2.8-4.6/tile on mostly-walkable hand-built areas). Measured 3.2 mean.
        roomDensities.Average().Should().BeInRange(1.5, 4.5,
            $"halls chamber density per room tile (got {roomDensities.Average():F3})");
        nnMedians.Average().Should().BeLessThanOrEqualTo(3.0,
            $"all-placeable NN median (got {nnMedians.Average():F2})");
        within3s.Average().Should().BeGreaterThanOrEqualTo(0.55,
            $"within-3m fraction (got {within3s.Average():F3})");
        top3s.Average().Should().BeLessThanOrEqualTo(0.35,
            $"top-3 resref share mean (got {top3s.Average():F3})");
        top3s.Max().Should().BeLessThanOrEqualTo(0.45,
            $"top-3 resref share worst seed (got {top3s.Max():F3})");
    }

    // ============================================================
    // Ground decals never stand alone.
    // ============================================================

    [TestCase(StandardLayoutProfiles.Packed, MineCaveDungeonDefinition.ThemeKey)]
    [TestCase(StandardLayoutProfiles.Halls, AlienRuinDungeonDefinition.ThemeKey)]
    public void FutCity_GroundDecals_NeverStandAlone(string layoutKey, string themeKey)
    {
        var c = Composition(themeKey, BaseGameTilesetProfiles.FutCity, layoutKey);
        var decalsChecked = 0;

        for (var i = 0; i < SeedCount; i++)
        {
            var (_, plan) = PlanFor(c, SeedBase + 200 + i, 20);

            foreach (var decal in plan.Where(p => GroundDecalResrefs.Contains(p.Resref)))
            {
                decalsChecked++;
                var hasNeighbor = plan.Any(other =>
                    !ReferenceEquals(other, decal) &&
                    Vector2.Distance(
                        new Vector2(decal.Position.X, decal.Position.Y),
                        new Vector2(other.Position.X, other.Position.Y)) <= 3.0f);

                hasNeighbor.Should().BeTrue(
                    $"seed {SeedBase + 200 + i}: ground decal '{decal.Resref}' at " +
                    $"({decal.Position.X:F1},{decal.Position.Y:F1}) must have clutter layered within 3m " +
                    "(decals only exist under piles or as courtyard centers with clutter on top)");
            }
        }

        decalsChecked.Should().BeGreaterThan(50, "the city palette should lay plenty of ground decals under its piles");
    }

    // ============================================================
    // Landmark one-offs never free-float mid-plaza.
    // ============================================================

    [Test]
    public void FutCity_LandmarkOneOffs_NeverPlaceInRoomCenterOrWallAdjacentBuckets()
    {
        var c = Composition(MineCaveDungeonDefinition.ThemeKey, BaseGameTilesetProfiles.FutCity, StandardLayoutProfiles.Packed);
        var landmarkResrefs = c.Tileset.Decorations
            .Where(d => d.Role == DecorationRole.Landmark)
            .Select(d => d.Resref)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        landmarkResrefs.Should().NotBeEmpty("fcx01 curates its vehicles as Landmark one-offs");

        for (var i = 0; i < SeedCount; i++)
        {
            var (layout, plan) = PlanFor(c, SeedBase + 300 + i, 20);

            foreach (var placement in plan.Where(p => landmarkResrefs.Contains(p.Resref)))
            {
                placement.Context.Should().NotBe(DecorationContext.RoomCenter,
                    $"'{placement.Resref}' is a Landmark one-off and must never float mid-plaza");
                placement.Context.Should().NotBe(DecorationContext.WallAdjacent,
                    $"'{placement.Resref}' is a Landmark one-off and must never free-stand along an arbitrary room divider");

                if (placement.Context == DecorationContext.StructureAdjacent)
                {
                    var tile = ((int)(placement.Position.X / 10f), (int)(placement.Position.Y / 10f));
                    DungeonDecorationPlanner.IsStructureAdjacent(tile, layout).Should().BeTrue(
                        $"'{placement.Resref}' must anchor against a stamped structure footprint");
                }
            }
        }
    }

    // ============================================================
    // Tower bases collect piles (StructureAdjacent clusters).
    // ============================================================

    [Test]
    public void FutCityPacked_StampedStructureBases_CollectClutterPiles()
    {
        var c = Composition(MineCaveDungeonDefinition.ThemeKey, BaseGameTilesetProfiles.FutCity, StandardLayoutProfiles.Packed);
        var structureAdjacentPileMembers = 0;

        for (var i = 0; i < SeedCount; i++)
        {
            var (layout, plan) = PlanFor(c, SeedBase + i, 20);
            if (layout.StampedStructureTiles.Count == 0)
                continue;

            foreach (var member in plan.Where(p => p.Context == DecorationContext.ClutterPile))
            {
                var tile = ((int)(member.Position.X / 10f), (int)(member.Position.Y / 10f));
                if (DungeonDecorationPlanner.IsStructureAdjacent(tile, layout))
                    structureAdjacentPileMembers++;
            }
        }

        // Structure-adjacent anchors carry double pile weight (PileStructureAnchorWeight), so tower
        // bases must collect a healthy share of pile dressing -- the round-5 "stamped tower groups
        // with completely bare bases" fix. Measured ~1400/10 seeds; floor set far below.
        structureAdjacentPileMembers.Should().BeGreaterThan(200,
            $"stamped tower bases should collect clutter piles (got {structureAdjacentPileMembers})");
    }

    // ============================================================
    // Role gating: no piles for palettes without curated clutter.
    // ============================================================

    [Test]
    public void PaletteWithoutClutterRoles_NeverEmitsPileOrDecalContexts()
    {
        var c = Composition(MineCaveDungeonDefinition.ThemeKey, StandardTilesetProfiles.AncientRuin, StandardLayoutProfiles.Halls);
        c.Tileset.Decorations.Should().NotContain(d => d.Role == DecorationRole.Clutter,
            "this test needs a palette with no curated clutter");

        for (var i = 0; i < 4; i++)
        {
            var (_, plan) = PlanFor(c, SeedBase + i, 20);
            plan.Should().NotContain(p => p.Context == DecorationContext.ClutterPile || p.Context == DecorationContext.GroundDecal,
                "clutter piles only exist for palettes that curate Clutter-role entries");
        }
    }

    // ============================================================
    // Tileset-owned density override and variant inheritance.
    // ============================================================

    [Test]
    public void FutCityPlazaVariant_InheritsFamilyDecorationDensity()
    {
        var tilesets = new BaseGameTilesetProfiles().BuildTilesetProfiles();
        DungeonTilesetPaletteInheritance.Apply(tilesets);

        var futCity = tilesets[BaseGameTilesetProfiles.FutCity];
        var plaza = tilesets[BaseGameTilesetProfiles.FutCityPlaza];

        futCity.DecorationDensityPerTile.Should().BeGreaterThan(1.0,
            "the city family declares its own mined density band");
        plaza.DecorationDensityPerTile.Should().Be(futCity.DecorationDensityPerTile,
            "the Cobble2 palette variant inherits the family density");
    }
}
