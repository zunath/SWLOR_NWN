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
/// Round-9 mid-room composition acceptance suite: interior ensembles (civic monument gardens /
/// commercial plaza islands), industrial depot blocks, zone-marking feature-tile dressing, and the
/// urban decal discipline.
///
/// Evidence baseline (_scratch_decor/mine_r9_interiors.py over the 24 hand-built fcx01 areas):
///  - Room-scale interior share (decoratives with no building/road tile within Chebyshev 1) runs
///    0.08-0.51 across the structured commercial/industrial/civic reference areas; the round-8
///    generated flagship measured 0.10 -- the reported "plaza middles are mostly empty floor".
///  - Industrial crate-family same-family NN median 0.09m with 93% within 2.2m, colinear runs of
///    4-12 sharing a dominant bearing (share 0.81) -- dense butt-jointed DEPOT rows, not the
///    generated "evenly-spaced exhibits, each on its own pad".
///  - The clean decal family splits placements across 4+ models (floorm01 0.55 / florrd01 0.18 /
///    flormh01 0.14 / hatch grills 0.13); round-8 output put ~70% of pads on one plate.
///  - zep_arch-family desert-ruin props appear in ZERO hand-built fcx01 areas (they live on the
///    desert/ruins tilesets), and the fcx01 palettes correctly curate none -- the reported
///    "free-standing fragments in pairs" artifact was sparse 3-member courtyard rings and naked
///    RoomCenter monuments, both retired here: urban interior set pieces are ALWAYS composed
///    (centerpiece + satellites), and urban courtyards commit at 4+ ring members.
/// </summary>
public class InteriorEnsembleCompositionTests
{
    private const int SeedBase = 7001;
    private const int SeedCount = 10;
    private const int Size = 20;

    private static readonly DecorationContext[] EnsembleContexts =
    {
        DecorationContext.EnsembleCenter, DecorationContext.EnsembleMember
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
        int seed, string decorationProfile = null)
    {
        var composition = new DungeonComposition { Content = c.Detail, Tileset = c.Tileset, Layout = c.Layout };
        var parameters = composition.BuildLayoutParameters();
        parameters.EntranceCount = 1;
        parameters.ExitCount = 1;
        parameters.DoorTransitions = true;

        var result = LayoutSolver.Solve(parameters, c.Model, Size, Size, seed, c.Tileset.PrimaryOpenTerrain);
        result.Success.Should().BeTrue($"seed {seed} must solve: {result.FailureReason}");

        return (result.Resolved, DungeonDecorationPlanner.Plan(result.Resolved, c.Tileset, c.Detail, 100, decorationProfile));
    }

    private static (int X, int Y) TileOf(PlannedDecoration p) =>
        ((int)MathF.Floor(p.Position.X / 10f), (int)MathF.Floor(p.Position.Y / 10f));

    private static float Dist(PlannedDecoration a, PlannedDecoration b) =>
        Vector2.Distance(new Vector2(a.Position.X, a.Position.Y), new Vector2(b.Position.X, b.Position.Y));

    // ============================================================
    // Room-scale interior fill: the "barren plaza middles" gate.
    // ============================================================

    [TestCase(MineCaveDungeonDefinition.ThemeKey, StandardLayoutProfiles.Packed)]
    [TestCase(AlienRuinDungeonDefinition.ThemeKey, StandardLayoutProfiles.Halls)]
    public void InteriorShare_MeetsHandBuiltBand(string themeKey, string layoutKey)
    {
        var c = Composition(themeKey, BaseGameTilesetProfiles.FutCity, layoutKey);
        var shares = new List<double>();

        for (var i = 0; i < SeedCount; i++)
        {
            var (layout, plan) = PlanFor(c, SeedBase + i);
            if (plan.Count == 0)
                continue;

            // Same methodology as the hand-built miner: a placement is INTERIOR when no
            // building/structure tile and no road-carrying tile sits within Chebyshev 1 of its
            // own cell (_scratch_decor/mine_r9_interiors.py).
            var interior = plan.Count(p =>
            {
                var tile = TileOf(p);
                return !DungeonDecorationPlanner.IsRoadAdjacent(tile, layout, "Routes") &&
                       !DungeonDecorationPlanner.IsStructureAdjacent(tile, layout);
            });
            shares.Add(interior / (double)plan.Count);
        }

        var average = shares.Average();
        TestContext.WriteLine($"{themeKey}/{layoutKey}: interior share avg {average:F3} " +
                              $"(per seed: {string.Join(", ", shares.Select(s => s.ToString("F2")))})");
        // Hand-built structured city areas measure 0.08-0.51; the round-8 flagship regressed to
        // 0.10 with everything hugging edges. The upper bound guards against overshooting into
        // mid-room clutter.
        average.Should().BeInRange(0.10, 0.45,
            $"generated city interiors must carry the hand-built share of mid-room content (got {average:F3})");
    }

    // ============================================================
    // Ensembles are composed: never a free-standing monument or pair.
    // ============================================================

    [TestCase(MineCaveDungeonDefinition.ThemeKey, StandardLayoutProfiles.Packed, null)]
    [TestCase(AlienRuinDungeonDefinition.ThemeKey, StandardLayoutProfiles.Halls, null)]
    [TestCase(AlienRuinDungeonDefinition.ThemeKey, StandardLayoutProfiles.Halls, "ruined")]
    public void Ensembles_AreAlwaysComposed(string themeKey, string layoutKey, string profile)
    {
        var c = Composition(themeKey, BaseGameTilesetProfiles.FutCity, layoutKey);
        var centersSeen = 0;

        for (var i = 0; i < SeedCount; i++)
        {
            var (_, plan) = PlanFor(c, SeedBase + i, profile);
            var ensemble = plan.Where(p => EnsembleContexts.Contains(p.Context)).ToList();

            foreach (var center in ensemble.Where(p => p.Context == DecorationContext.EnsembleCenter))
            {
                centersSeen++;
                var satellites = ensemble.Count(m =>
                    m.Context == DecorationContext.EnsembleMember && !ReferenceEquals(m, center) &&
                    Dist(center, m) <= 6.5f);
                satellites.Should().BeGreaterOrEqualTo(DungeonDecorationPlanner.EnsembleMinSatellites,
                    $"seed {SeedBase + i}: ensemble centerpiece '{center.Resref}' must stand in a composed court");

                // An intentional plaza set piece squares up with the grid.
                (center.Facing % 90f).Should().Be(0f,
                    $"seed {SeedBase + i}: ensemble centerpiece '{center.Resref}' bears cardinally");
            }

            foreach (var member in ensemble.Where(p => p.Context == DecorationContext.EnsembleMember))
            {
                // 8m covers the widest committed shape: opposite satellites of a Large-center
                // garden ring (radius up to ~4.5) still see each other and their centerpiece.
                ensemble.Count(o => !ReferenceEquals(o, member) && Dist(member, o) <= 8.0f)
                    .Should().BeGreaterOrEqualTo(2,
                        $"seed {SeedBase + i}: ensemble member '{member.Resref}' is never a free-standing single/pair");
            }
        }

        if (profile == null)
            centersSeen.Should().BeGreaterOrEqualTo(SeedCount,
                $"the seed batch should compose at least one mid-room ensemble per area on average (got {centersSeen})");
    }

    [TestCase(MineCaveDungeonDefinition.ThemeKey, StandardLayoutProfiles.Packed)]
    [TestCase(AlienRuinDungeonDefinition.ThemeKey, StandardLayoutProfiles.Halls)]
    public void UrbanPlans_NeverEmitNakedRoomCenterPlacements(string themeKey, string layoutKey)
    {
        // The round-9 monument sweep: under the urban grammar the plain single-item RoomCenter
        // roll is replaced by the composed ensemble path, so a lone monument/pillar can never
        // stand free mid-plaza again (the reported "fragments standing free in pairs").
        var c = Composition(themeKey, BaseGameTilesetProfiles.FutCity, layoutKey);

        for (var i = 0; i < SeedCount; i++)
        {
            var (_, plan) = PlanFor(c, SeedBase + i);
            plan.Should().NotContain(p => p.Context == DecorationContext.RoomCenter,
                $"seed {SeedBase + i}: urban interior set pieces place only through composed ensembles");
        }
    }

    // ============================================================
    // Depot blocks: dense butt-jointed rows, shared bearing, pads per block.
    // ============================================================

    [Test]
    public void IndustrialRooms_ComposeDepotBlocks_AtHandBuiltPitch()
    {
        var c = Composition(MineCaveDungeonDefinition.ThemeKey, BaseGameTilesetProfiles.FutCity, StandardLayoutProfiles.Packed);
        var blocksSeen = 0;

        for (var i = 0; i < SeedCount; i++)
        {
            var (_, plan) = PlanFor(c, SeedBase + i);
            var depot = plan.Where(p => p.Context == DecorationContext.DepotRow).ToList();
            var decals = plan.Where(p => p.Context == DecorationContext.GroundDecal).ToList();

            // Single-link clustering at 2.6m groups one block's rows+satellites together without
            // bridging across rooms.
            var clusters = Cluster(depot, 2.6f);
            foreach (var block in clusters)
            {
                block.Count.Should().BeGreaterOrEqualTo(DungeonDecorationPlanner.DepotBlockMinItems,
                    $"seed {SeedBase + i}: a depot block is a dense multi-crate stack, never a couple of strays");
                blocksSeen++;

                // Within-block NN at the hand-built butt pitch (family NN median far below the old
                // 10m pad-single spacing; generated pitch 1.35m).
                var nn = block.Select(m => block.Where(o => !ReferenceEquals(o, m)).Min(o => Dist(m, o)))
                    .OrderBy(d => d).ToList();
                nn[nn.Count / 2].Should().BeLessThanOrEqualTo(2.0f,
                    $"seed {SeedBase + i}: depot rows butt-joint at near-model-width pitch (got NN median {nn[nn.Count / 2]:F2})");

                // Shared block bearing: every member's facing is the block bearing or a quarter
                // turn of it (all congruent mod 90).
                var baseline = block[0].Facing % 90f;
                foreach (var member in block)
                    (member.Facing % 90f).Should().BeApproximately(baseline, 0.01f,
                        $"seed {SeedBase + i}: depot members share the block bearing (mod quarter turns)");

                // Pads per BLOCK, not per item: at most one ground decal inside the block's span.
                var cx = block.Average(m => m.Position.X);
                var cy = block.Average(m => m.Position.Y);
                decals.Count(d => Vector2.Distance(new Vector2(d.Position.X, d.Position.Y), new Vector2((float)cx, (float)cy)) <= 2.5f)
                    .Should().BeLessThanOrEqualTo(1,
                        $"seed {SeedBase + i}: one pad per depot block, never a pad under every crate");
            }
        }

        blocksSeen.Should().BeGreaterOrEqualTo(SeedCount,
            $"every packed city has industrial rooms, and each eligible one composes a depot block (got {blocksSeen})");
    }

    [Test]
    public void RuinedProfile_NeverStacksDepotRows()
    {
        // Organic collapse debris tumbles; it never butt-joints into neat depot rows.
        var c = Composition(AlienRuinDungeonDefinition.ThemeKey, BaseGameTilesetProfiles.FutCity, StandardLayoutProfiles.Halls);

        for (var i = 0; i < 4; i++)
        {
            var (_, plan) = PlanFor(c, SeedBase + i, "ruined");
            plan.Should().NotContain(p => p.Context == DecorationContext.DepotRow,
                "the ruined profile's organic clutter keeps the loose-pile arrangement");
        }
    }

    // ============================================================
    // Decal discipline: rotation, and no decal ever stands bare.
    // ============================================================

    [TestCase(MineCaveDungeonDefinition.ThemeKey, StandardLayoutProfiles.Packed, null)]
    [TestCase(AlienRuinDungeonDefinition.ThemeKey, StandardLayoutProfiles.Halls, "ruined")]
    public void GroundDecals_RotateAcrossTheFamily_AndNeverStandBare(string themeKey, string layoutKey, string profile)
    {
        var c = Composition(themeKey, BaseGameTilesetProfiles.FutCity, layoutKey);
        var counts = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        var total = 0;

        for (var i = 0; i < SeedCount; i++)
        {
            var (_, plan) = PlanFor(c, SeedBase + i, profile);

            foreach (var decal in plan.Where(p => p.Context == DecorationContext.GroundDecal))
            {
                total++;
                counts[decal.Resref] = counts.GetValueOrDefault(decal.Resref) + 1;

                // The empty-zone rule one level down: a pad implies the content on/around it.
                plan.Count(o => !ReferenceEquals(o, decal) && o.Context != DecorationContext.GroundDecal &&
                                Dist(decal, o) <= 3.0f)
                    .Should().BeGreaterOrEqualTo(1,
                        $"seed {SeedBase + i}: decal '{decal.Resref}' must carry content within 3m");
            }
        }

        total.Should().BeGreaterThan(80, "the city palette lays plenty of pads across 10 seeds");
        counts.Count.Should().BeGreaterOrEqualTo(3, "pads rotate through the decal family");
        var topShare = counts.Values.Max() / (double)total;
        TestContext.WriteLine($"{themeKey}/{layoutKey}/{profile ?? "standard"}: {total} decals, top share {topShare:F3}, " +
                              string.Join(", ", counts.OrderByDescending(kv => kv.Value).Select(kv => $"{kv.Key}={kv.Value}")));
        // Hand-built clean-family top share is 0.55 (swd_floorm01); round-8 output measured ~0.70
        // on one plate.
        topShare.Should().BeLessThanOrEqualTo(0.55,
            $"no single decal resref may dominate the area's pads (got {topShare:F3})");
    }

    // ============================================================
    // Zone-marking feature tiles: a park is never bare.
    // ============================================================

    [TestCase(MineCaveDungeonDefinition.ThemeKey, BaseGameTilesetProfiles.FutCity, StandardLayoutProfiles.Packed)]
    [TestCase(AlienRuinDungeonDefinition.ThemeKey, BaseGameTilesetProfiles.FutCity, StandardLayoutProfiles.Halls)]
    public void ZoneFeatureTiles_InRooms_AlwaysCarryTheirEnsemble(string themeKey, string tilesetKey, string layoutKey)
    {
        var c = Composition(themeKey, tilesetKey, layoutKey);
        c.Tileset.FeatureTileDressings.Should().NotBeEmpty("fcx01 declares its lawn/fountain dressing obligations");
        var dressedCellsSeen = 0;

        for (var i = 0; i < SeedCount; i++)
        {
            var (layout, plan) = PlanFor(c, SeedBase + i);

            var excluded = new HashSet<(int X, int Y)>();
            foreach (var transition in layout.Transitions)
            {
                excluded.Add(transition.Tile);
                if (transition.Style is TransitionStyle.Door or TransitionStyle.GroupExit)
                {
                    excluded.Add(transition.DoorCell);
                    excluded.Add(transition.DoorwayCell);
                }
            }

            foreach (var (cell, groupName) in layout.FeatureTileCells)
            {
                if (!c.Tileset.FeatureTileDressings.TryGetValue(groupName, out var dressing) ||
                    dressing == FeatureZoneDressing.None)
                    continue;

                var room = layout.Rooms.FirstOrDefault(r => !r.IsSetPiece && r.Tiles.Contains(cell));
                if (room == null || excluded.Contains(cell) || cell == room.CenterTile)
                    continue;

                dressedCellsSeen++;
                var center = new Vector2(cell.X * 10f + 5f, cell.Y * 10f + 5f);
                var members = plan.Count(p =>
                    EnsembleContexts.Contains(p.Context) &&
                    Vector2.Distance(new Vector2(p.Position.X, p.Position.Y), center) <= 7.5f);
                members.Should().BeGreaterOrEqualTo(3,
                    $"seed {SeedBase + i}: zone tile '{groupName}' at {cell} implies content -- a park with no park " +
                    "is the reported artifact");
            }
        }

        TestContext.WriteLine($"{themeKey}/{layoutKey}: {dressedCellsSeen} dressed zone cells across {SeedCount} seeds");
    }

    // ============================================================
    // Non-urban isolation: the new mechanisms never leak.
    // ============================================================

    [TestCase(StandardTilesetProfiles.Cavern, StandardLayoutProfiles.Complex)]
    [TestCase(StandardTilesetProfiles.AncientRuin, StandardLayoutProfiles.Halls)]
    public void NonUrbanTilesets_NeverEmitEnsembleOrDepotContexts(string tilesetKey, string layoutKey)
    {
        var c = Composition(MineCaveDungeonDefinition.ThemeKey, tilesetKey, layoutKey);

        for (var i = 0; i < 4; i++)
        {
            var (_, plan) = PlanFor(c, SeedBase + i);
            plan.Should().NotContain(p =>
                    p.Context == DecorationContext.EnsembleCenter ||
                    p.Context == DecorationContext.EnsembleMember ||
                    p.Context == DecorationContext.DepotRow,
                "mid-room ensembles and depot blocks are urban-grammar mechanisms only");
        }
    }

    private static List<List<PlannedDecoration>> Cluster(List<PlannedDecoration> items, float radius)
    {
        var parent = Enumerable.Range(0, items.Count).ToArray();

        int Find(int a)
        {
            while (parent[a] != a)
            {
                parent[a] = parent[parent[a]];
                a = parent[a];
            }

            return a;
        }

        for (var i = 0; i < items.Count; i++)
        for (var j = i + 1; j < items.Count; j++)
        {
            if (Dist(items[i], items[j]) > radius)
                continue;
            var ra = Find(i);
            var rb = Find(j);
            if (ra != rb)
                parent[ra] = rb;
        }

        return items
            .Select((item, index) => (item, root: Find(index)))
            .GroupBy(t => t.root)
            .Select(g => g.Select(t => t.item).ToList())
            .ToList();
    }
}
