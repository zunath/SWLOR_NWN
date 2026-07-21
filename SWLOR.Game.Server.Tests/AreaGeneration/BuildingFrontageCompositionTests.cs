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
/// Round-11 acceptance suite: the building-placeable canyon frontage system and the wall-mounted
/// facade dressing pass (see BuildingFrontagePlanner).
///
/// Evidence baseline (_scratch_decor/r11_mine_buildings.py + promenade_benchmark.py): the
/// hand-built 12x12 flagship promenade (pw_ar_narpromena) walls its plaza with 39 building
/// placeables and ZERO building tiles -- swd_build007 rows at 9.8-10.1m pitch, 100%
/// cardinal-quantized bearings, 7 distinct building models, open-perimeter enclosure 0.804
/// (family band 0.46-0.98). The dense city areas hang 0.13-0.23 of their decoratives above
/// Z 0.5m, dominated by holo signage attached to building faces (median face distance ~0,
/// per-resref Z bands 1.1-7.0m).
/// </summary>
public class BuildingFrontageCompositionTests
{
    private const int SeedBase = 7001;
    private const int SeedCount = 10;

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

    private static (int X, int Y) TileOf(PlannedDecoration p) =>
        ((int)MathF.Floor(p.Position.X / 10f), (int)MathF.Floor(p.Position.Y / 10f));

    private static HashSet<(int X, int Y)> OpenCells(ResolvedLayout layout) =>
        layout.Rooms.Where(r => !r.IsSetPiece).SelectMany(r => r.Tiles).ToHashSet();

    private static (int Dx, int Dy) OutwardOf(PlannedDecoration p) => p.Facing switch
    {
        0f => (1, 0),
        90f => (0, 1),
        180f => (-1, 0),
        _ => (0, -1)
    };

    /// <summary>Reconstructs a frontage placement's axis-aligned footprint from its declared
    /// entry dimensions (scaled by the placement's per-instance visual scale -- see
    /// DungeonTilesetProfile.FrontageScaleJitter) and cardinal facing (the face's outward
    /// normal).</summary>
    private static (float MinX, float MinY, float MaxX, float MaxY) FootprintOf(
        PlannedDecoration p, Dictionary<string, BuildingFrontageEntry> entries)
    {
        var entry = entries[p.Resref];
        var outward = OutwardOf(p);
        var width = entry.FaceWidth * p.VisualScale;
        var depth = entry.Depth * p.VisualScale;
        var halfX = outward.Dx != 0 ? depth / 2f : width / 2f;
        var halfY = outward.Dx != 0 ? width / 2f : depth / 2f;
        return (p.Position.X - halfX, p.Position.Y - halfY, p.Position.X + halfX, p.Position.Y + halfY);
    }

    // ============================================================
    // Walkable clearance: buildings stand on the margin, never on a lane.
    // ============================================================

    [TestCase(MineCaveDungeonDefinition.ThemeKey, StandardLayoutProfiles.Packed, 12)]
    [TestCase(MineCaveDungeonDefinition.ThemeKey, StandardLayoutProfiles.Packed, 20)]
    [TestCase(AlienRuinDungeonDefinition.ThemeKey, StandardLayoutProfiles.Halls, 20)]
    public void FrontageBuildings_StandOnMarginCells_AndNeverBlockWalkableLanes(
        string themeKey, string layoutKey, int size)
    {
        var c = Composition(themeKey, BaseGameTilesetProfiles.FutCity, layoutKey);
        var entries = c.Tileset.FrontageBuildings.ToDictionary(e => e.Resref, StringComparer.OrdinalIgnoreCase);
        var violations = new List<string>();
        var checkedCount = 0;

        for (var i = 0; i < SeedCount; i++)
        {
            var seed = SeedBase + i;
            var (layout, plan) = PlanFor(c, seed, size);
            var open = OpenCells(layout);
            var setPieceOpen = layout.Rooms.Where(r => r.IsSetPiece).SelectMany(r => r.Tiles).ToHashSet();

            foreach (var p in plan.Where(p => p.Context == DecorationContext.BuildingFrontage))
            {
                checkedCount++;
                var cell = TileOf(p);
                if (open.Contains(cell) || setPieceOpen.Contains(cell))
                    violations.Add($"seed {seed}: '{p.Resref}' anchors on walkable cell {cell}");
                if (layout.StampedStructureTiles.Contains(cell))
                    violations.Add($"seed {seed}: '{p.Resref}' anchors on stamped structure cell {cell}");
                if (DungeonDecorationPlanner.TileCarriesRoadEdge(cell, layout, "Routes"))
                    violations.Add($"seed {seed}: '{p.Resref}' anchors on a road-carrying cell {cell}");

                // Footprint intrusion into any walkable cell stays within the clearance contract.
                var box = FootprintOf(p, entries);
                foreach (var openCell in open.Concat(setPieceOpen))
                {
                    var penetration = BuildingFrontagePlanner.CellPenetration(box, openCell.X, openCell.Y);
                    if (penetration > BuildingFrontagePlanner.MaxOpenIntrusion + 0.05f)
                        violations.Add($"seed {seed}: '{p.Resref}' footprint penetrates walkable cell {openCell} by {penetration:F2}m");
                }
            }
        }

        checkedCount.Should().BeGreaterThan(100, "the city compositions should erect frontage walls across 10 seeds");
        violations.Should().BeEmpty(string.Join(Environment.NewLine, violations.Take(20)));
    }

    // ============================================================
    // Alignment: every frontage building faces the open cell it walls, cardinally.
    // ============================================================

    [TestCase(MineCaveDungeonDefinition.ThemeKey, StandardLayoutProfiles.Packed, 12)]
    [TestCase(MineCaveDungeonDefinition.ThemeKey, StandardLayoutProfiles.Packed, 20)]
    public void FrontageBuildings_FaceOpenSpace_WithCardinalBearing(string themeKey, string layoutKey, int size)
    {
        var c = Composition(themeKey, BaseGameTilesetProfiles.FutCity, layoutKey);
        var entries = c.Tileset.FrontageBuildings.ToDictionary(e => e.Resref, StringComparer.OrdinalIgnoreCase);
        var violations = new List<string>();

        for (var i = 0; i < SeedCount; i++)
        {
            var seed = SeedBase + i;
            var (layout, plan) = PlanFor(c, seed, size);
            var open = OpenCells(layout);

            foreach (var p in plan.Where(p => p.Context == DecorationContext.BuildingFrontage))
            {
                if (p.Facing is not (0f or 90f or 180f or 270f))
                {
                    violations.Add($"seed {seed}: '{p.Resref}' bearing {p.Facing} is not cardinal");
                    continue;
                }

                // The bearing is the FACE's outward normal: the building face sits
                // FaceIntrusion proud of the open-cell boundary, so a probe point just at the
                // face plane (center + outward * depth/2, i.e. 0.6m past the boundary) must land
                // in an open room cell. Probing the center's own cell would be wrong for the
                // deep models (build001/002/005 center 1-2 cells behind their face).
                var outward = OutwardOf(p);
                var entry = entries[p.Resref];
                var facePoint = (
                    X: p.Position.X + outward.Dx * entry.Depth * p.VisualScale / 2f,
                    Y: p.Position.Y + outward.Dy * entry.Depth * p.VisualScale / 2f);
                var facedCell = ((int)MathF.Floor(facePoint.X / 10f), (int)MathF.Floor(facePoint.Y / 10f));
                if (!open.Contains(facedCell))
                    violations.Add($"seed {seed}: '{p.Resref}' at ({p.Position.X:F1},{p.Position.Y:F1}) faces {p.Facing} but its face plane lands on non-open cell {facedCell}");
            }
        }

        violations.Should().BeEmpty(string.Join(Environment.NewLine, violations.Take(20)));
    }

    // ============================================================
    // Promenade-scale enclosure: the 12x12 showcase hits the flagship band.
    // ============================================================

    [Test]
    public void PromenadeScaleShowcase_EnclosureMeetsFlagshipBand()
    {
        var c = Composition(MineCaveDungeonDefinition.ThemeKey, BaseGameTilesetProfiles.FutCity, StandardLayoutProfiles.Packed);
        var entries = c.Tileset.FrontageBuildings.ToDictionary(e => e.Resref, StringComparer.OrdinalIgnoreCase);
        var enclosures = new List<double>();

        for (var i = 0; i < SeedCount; i++)
        {
            var (layout, plan) = PlanFor(c, SeedBase + i, 12);
            var open = OpenCells(layout);
            var rects = plan
                .Where(p => p.Context == DecorationContext.BuildingFrontage)
                .Select(p => FootprintOf(p, entries))
                .ToList();

            // Same metric as _scratch_decor/promenade_benchmark.py: fraction of open-boundary
            // edges (open cell sides with a non-open neighbor or the grid rim) walled by building
            // mass -- a stamped structure tile behind the edge, or a frontage footprint within 6m
            // of the edge midpoint.
            var total = 0;
            var walled = 0;
            foreach (var (x, y) in open)
            foreach (var (dx, dy) in new[] { (1, 0), (-1, 0), (0, 1), (0, -1) })
            {
                var neighbor = (x + dx, y + dy);
                if (open.Contains(neighbor))
                    continue;

                total++;
                if (layout.StampedStructureTiles.Contains(neighbor))
                {
                    walled++;
                    continue;
                }

                var mx = x * 10f + 5f + dx * 5f;
                var my = y * 10f + 5f + dy * 5f;
                if (rects.Any(r =>
                        MathF.Sqrt(
                            MathF.Pow(MathF.Max(MathF.Max(r.MinX - mx, 0f), mx - r.MaxX), 2) +
                            MathF.Pow(MathF.Max(MathF.Max(r.MinY - my, 0f), my - r.MaxY), 2)) <= 6f))
                    walled++;
            }

            total.Should().BeGreaterThan(0);
            enclosures.Add(walled / (double)total);
        }

        TestContext.WriteLine($"12x12 enclosure per seed: {string.Join(", ", enclosures.Select(e => e.ToString("F3")))}");
        // Flagship pw_ar_narpromena measures 0.804; the hand-built city family band is
        // 0.458-0.976 (benchmark, WALL_DIST 6m). Generated 12x12 measured 0.816-0.944 at
        // calibration; assert with head-room against seed drift.
        enclosures.Min().Should().BeGreaterOrEqualTo(0.65,
            $"every 12x12 promenade-scale seed must read as a walled canyon (worst {enclosures.Min():F3})");
        enclosures.Average().Should().BeGreaterOrEqualTo(0.75,
            $"mean 12x12 enclosure must sit in the flagship band (got {enclosures.Average():F3})");
    }

    // ============================================================
    // Variety: the building mix follows the hand-built dominant-plus-accent pattern.
    // ============================================================

    [TestCase(MineCaveDungeonDefinition.ThemeKey, StandardLayoutProfiles.Packed, 12, 4)]
    [TestCase(MineCaveDungeonDefinition.ThemeKey, StandardLayoutProfiles.Packed, 20, 5)]
    // Round-14 expanded-pool floor at the signature scale: comparable-mass hand-built areas draw
    // 12-17 distinct building models (nsshipyard 17, narscorpd 12 -- r14_mine_variety.py);
    // measured generated 24x24 draws 16-21 with the expanded pool.
    [TestCase(MineCaveDungeonDefinition.ThemeKey, StandardLayoutProfiles.Packed, 24, 12)]
    public void FrontageModelVariety_MeetsHandBuiltMinimum(string themeKey, string layoutKey, int size, int minVariety)
    {
        var c = Composition(themeKey, BaseGameTilesetProfiles.FutCity, layoutKey);
        var caps = c.Tileset.FrontageBuildings
            .Where(e => e.MaxPerArea > 0)
            .ToDictionary(e => e.Resref, e => e.MaxPerArea, StringComparer.OrdinalIgnoreCase);

        for (var i = 0; i < SeedCount; i++)
        {
            var seed = SeedBase + i;
            var (_, plan) = PlanFor(c, seed, size);
            var buildings = plan.Where(p => p.Context == DecorationContext.BuildingFrontage).ToList();
            buildings.Should().NotBeEmpty($"seed {seed}: the city composition should erect frontage walls");

            // Hand-built flagship: 7 distinct building models on 12x12; dense areas 6-23.
            buildings.Select(p => p.Resref).Distinct(StringComparer.OrdinalIgnoreCase).Count()
                .Should().BeGreaterOrEqualTo(minVariety,
                    $"seed {seed}: frontage variety must reach the hand-built minimum");

            // Per-model caps hold (round 15: every model is capped -- accents at their
            // comparable-mass hand-built maxima, workhorses at their all-areas maxima).
            foreach (var group in buildings.GroupBy(p => p.Resref, StringComparer.OrdinalIgnoreCase))
            {
                if (caps.TryGetValue(group.Key, out var cap))
                    group.Count().Should().BeLessOrEqualTo(cap,
                        $"seed {seed}: '{group.Key}' exceeded its per-area frontage cap");
            }
        }
    }

    // ============================================================
    // Round 15 -- salience histogram shape: the per-model/per-family histogram, the
    // omnidirectional same-model spacing floors, the building budget ceiling, and the
    // workhorse/accent split (see _scratch_decor/r15_mine_salience.py). Perceived variety
    // follows the histogram SHAPE: hand-builders repeat plain workhorse towers while
    // distinctive neon/emissive models stay rare and spread -- round-14's entropy/run gates
    // were salience-blind and let the neon build003 anchor a street ("still seeing a lot of
    // repetition" on the halls-20 showcase).
    // ============================================================

    [Test]
    public void FrontagePool_DeclaresMinedSalienceClassification()
    {
        var c = Composition(MineCaveDungeonDefinition.ThemeKey, BaseGameTilesetProfiles.FutCity,
            StandardLayoutProfiles.Packed);
        var entries = c.Tileset.FrontageBuildings;

        // Exactly the three mined workhorses are dominant-eligible (top-2 hand-built per-area
        // counts sum >= 20 AND diffuse neon share < 0.15).
        entries.Where(e => e.DominantEligible).Select(e => e.Resref).Should().BeEquivalentTo(
            new[] { "swd_build007", "swd_build004", "_mdrn_pl_kyru08" },
            "only the mined low-salience workhorse models may anchor a street");

        // Every pool member carries a hard per-area cap mined from the hand-built histograms.
        foreach (var entry in entries)
            entry.MaxPerArea.Should().BeGreaterThan(0,
                $"'{entry.Resref}' must cap at its hand-built per-area maximum");

        // Every non-workhorse model is spread by an omnidirectional spacing floor (or is
        // effectively unique per area), so no distinctive tower recurs across parallel rows.
        foreach (var entry in entries.Where(e => !e.DominantEligible))
            (entry.MinSameModelSpacing > 0f || entry.MaxPerArea <= 1).Should().BeTrue(
                $"accent '{entry.Resref}' needs a mined same-model spacing floor or a cap of 1");

        // The reported clone tower: the neon-clad build003 (emissive coverage 0.61, neon share
        // 0.35 -- the highest-salience member) caps at its comparable-mass hand-built max of 4
        // and joins the shared-atlas daf neon family.
        var build003 = entries.Single(e => e.Resref.Equals("swd_build003", StringComparison.OrdinalIgnoreCase));
        build003.DominantEligible.Should().BeFalse();
        build003.MaxPerArea.Should().Be(4);
        build003.MinSameModelSpacing.Should().Be(15f);
        build003.FamilyKey.Should().Be("dafneon");

        // Shared-texture families aggregate their caps (dafneon 15 = narcatwalk family max;
        // jsfsky 5 = randoncity's total).
        entries.Where(e => e.FamilyKey == "dafneon").Select(e => e.Resref).Should().BeEquivalentTo(
            new[] { "swd_build001", "swd_build002", "swd_build003", "swd_build005", "swd_build006" });
        entries.Where(e => e.FamilyKey == "dafneon").Should().OnlyContain(e => e.FamilyMaxPerArea == 15);
        entries.Where(e => e.FamilyKey == "jsfsky").Select(e => e.Resref).Should().BeEquivalentTo(
            new[] { "_mdrn_pl_buildg2", "_mdrn_pl_buildg4" });
        entries.Where(e => e.FamilyKey == "jsfsky").Should().OnlyContain(e => e.FamilyMaxPerArea == 5);
    }

    [TestCase(MineCaveDungeonDefinition.ThemeKey, StandardLayoutProfiles.Packed, 12)]
    [TestCase(MineCaveDungeonDefinition.ThemeKey, StandardLayoutProfiles.Packed, 20)]
    [TestCase(MineCaveDungeonDefinition.ThemeKey, StandardLayoutProfiles.Packed, 24)]
    [TestCase(AlienRuinDungeonDefinition.ThemeKey, StandardLayoutProfiles.Halls, 20)]
    public void FrontageSalience_HistogramSpacingAndBudget_MatchHandBuiltShape(
        string themeKey, string layoutKey, int size)
    {
        var c = Composition(themeKey, BaseGameTilesetProfiles.FutCity, layoutKey);
        var entries = c.Tileset.FrontageBuildings.ToDictionary(e => e.Resref, StringComparer.OrdinalIgnoreCase);
        var violations = new List<string>();

        for (var i = 0; i < SeedCount; i++)
        {
            var seed = SeedBase + i;
            var (layout, plan) = PlanFor(c, seed, size);
            var buildings = plan.Where(p => p.Context == DecorationContext.BuildingFrontage).ToList();
            if (buildings.Count == 0)
                continue;

            var groups = buildings
                .GroupBy(p => p.Resref, StringComparer.OrdinalIgnoreCase)
                .ToDictionary(g => g.Key, g => g.ToList(), StringComparer.OrdinalIgnoreCase);

            // Per-model histogram: no model exceeds its mined per-area cap.
            foreach (var (resref, list) in groups)
            {
                var entry = entries[resref];
                if (entry.MaxPerArea > 0 && list.Count > entry.MaxPerArea)
                    violations.Add($"seed {seed}: {resref} x{list.Count} > cap {entry.MaxPerArea}");
            }

            // Family histogram: shared-atlas families aggregate.
            foreach (var family in groups.Keys
                         .Select(r => entries[r])
                         .Where(e => !string.IsNullOrEmpty(e.FamilyKey))
                         .GroupBy(e => e.FamilyKey))
            {
                var total = family.Sum(e => groups[e.Resref].Count);
                var cap = family.Max(e => e.FamilyMaxPerArea);
                if (cap > 0 && total > cap)
                    violations.Add($"seed {seed}: family {family.Key} x{total} > cap {cap}");
            }

            // Omnidirectional same-model spacing (the parallel-row / facing-pair clone check):
            // every pair of a spaced model keeps the mined distance in ANY direction.
            foreach (var (resref, list) in groups)
            {
                var spacing = entries[resref].MinSameModelSpacing;
                if (spacing <= 0f || list.Count < 2)
                    continue;
                for (var a = 0; a < list.Count; a++)
                for (var b = a + 1; b < list.Count; b++)
                {
                    var dx = list[a].Position.X - list[b].Position.X;
                    var dy = list[a].Position.Y - list[b].Position.Y;
                    var d = MathF.Sqrt(dx * dx + dy * dy);
                    if (d < spacing - 0.05f)
                        violations.Add($"seed {seed}: {resref} pair {d:F1}m < spacing {spacing}m");
                }
            }

            // Building budget: never denser per open floor tile than the densest hand-built
            // precedent (narcatwalk 0.589; see BuildingFrontagePlanner.MaxBuildingsPerOpenTile).
            var open = OpenCells(layout).Count;
            var budget = Math.Max(1, (int)Math.Round(open * BuildingFrontagePlanner.MaxBuildingsPerOpenTile));
            if (buildings.Count > budget)
                violations.Add($"seed {seed}: {buildings.Count} buildings > budget {budget} ({open} open tiles)");

            // Salience-weighted share: the non-workhorse slice of the skyline stays under the
            // hand-built ceiling (narscorpd 0.571, the swd-palette maximum).
            if (buildings.Count >= 15)
            {
                var nonWorkhorse = buildings.Count(p => !entries[p.Resref].DominantEligible);
                var share = nonWorkhorse / (double)buildings.Count;
                if (share > 0.60)
                    violations.Add($"seed {seed}: non-workhorse share {share:F3} > hand-built ceiling 0.60");
            }
        }

        violations.Should().BeEmpty(string.Join(Environment.NewLine, violations.Take(20)));
    }

    // ============================================================
    // Elevated dressing: the vertical share meets the hand-built dense-city band.
    // ============================================================

    [TestCase(MineCaveDungeonDefinition.ThemeKey, StandardLayoutProfiles.Packed, 20)]
    [TestCase(MineCaveDungeonDefinition.ThemeKey, StandardLayoutProfiles.Packed, 24)]
    [TestCase(AlienRuinDungeonDefinition.ThemeKey, StandardLayoutProfiles.Halls, 20)]
    public void ElevatedDressingShare_MeetsHandBuiltBand(string themeKey, string layoutKey, int size)
    {
        var c = Composition(themeKey, BaseGameTilesetProfiles.FutCity, layoutKey);
        var shares = new List<double>();

        for (var i = 0; i < SeedCount; i++)
        {
            var (_, plan) = PlanFor(c, SeedBase + i, size);
            if (plan.Count == 0)
                continue;

            shares.Add(plan.Count(p => p.Position.Z > 0.5f) / (double)plan.Count);
        }

        TestContext.WriteLine($"{themeKey}/{layoutKey}@{size}: elevated share per seed: " +
                              string.Join(", ", shares.Select(s => s.ToString("F3"))));
        // Hand-built dense city areas: 0.137-0.230 (velundr/narscorpd/nsshipyard/ns_comrcial_ka/
        // narshadaar_promi). Per-seed calibration measured 0.16-0.21; the per-seed bound allows
        // seed drift while the mean stays inside the hand-built band.
        shares.Average().Should().BeInRange(0.13, 0.23,
            $"mean elevated share must sit in the hand-built dense-city band (got {shares.Average():F3})");
        foreach (var share in shares)
            share.Should().BeInRange(0.10, 0.26, "no single seed may leave the widened band");
    }

    // ============================================================
    // Facade mounts: on a face, inside the mined height band, cardinal.
    // ============================================================

    [TestCase(MineCaveDungeonDefinition.ThemeKey, StandardLayoutProfiles.Packed, 20)]
    [TestCase(AlienRuinDungeonDefinition.ThemeKey, StandardLayoutProfiles.Halls, 20)]
    public void FacadeMounts_HangOnBuildingFaces_AtMinedHeights(string themeKey, string layoutKey, int size)
    {
        var c = Composition(themeKey, BaseGameTilesetProfiles.FutCity, layoutKey);
        var bands = c.Tileset.FacadeMounts.ToDictionary(e => e.Resref,
            e => (e.MinHeight, e.MaxHeight), StringComparer.OrdinalIgnoreCase);
        var violations = new List<string>();
        var mountsChecked = 0;

        for (var i = 0; i < SeedCount; i++)
        {
            var seed = SeedBase + i;
            var (layout, plan) = PlanFor(c, seed, size);
            var structure = layout.StampedStructureTiles.Concat(layout.PlaceableStructureCells).ToList();

            foreach (var p in plan.Where(p => p.Context == DecorationContext.FacadeMount))
            {
                mountsChecked++;
                if (!bands.TryGetValue(p.Resref, out var band))
                {
                    violations.Add($"seed {seed}: mount '{p.Resref}' is not a declared FacadeMount entry");
                    continue;
                }

                if (p.Position.Z < band.MinHeight - 0.01f || p.Position.Z > band.MaxHeight + 0.01f)
                    violations.Add($"seed {seed}: '{p.Resref}' hangs at Z {p.Position.Z:F2} outside its mined band [{band.MinHeight}, {band.MaxHeight}]");
                if (p.Facing is not (0f or 90f or 180f or 270f))
                    violations.Add($"seed {seed}: '{p.Resref}' bearing {p.Facing} is not the face normal");

                // Proud of a real structure face: within 1.5m of a stamped or placeable
                // structure cell's rectangle.
                var near = structure.Any(cellPos =>
                {
                    var dx = MathF.Max(MathF.Max(cellPos.X * 10f - p.Position.X, 0f), p.Position.X - (cellPos.X * 10f + 10f));
                    var dy = MathF.Max(MathF.Max(cellPos.Y * 10f - p.Position.Y, 0f), p.Position.Y - (cellPos.Y * 10f + 10f));
                    return MathF.Sqrt(dx * dx + dy * dy) <= 1.5f;
                });
                if (!near)
                    violations.Add($"seed {seed}: '{p.Resref}' at ({p.Position.X:F1},{p.Position.Y:F1}) hangs on no building face");
            }
        }

        mountsChecked.Should().BeGreaterThan(100, "dense city compositions should hang facade signage across 10 seeds");
        violations.Should().BeEmpty(string.Join(Environment.NewLine, violations.Take(20)));
    }

    // ============================================================
    // Isolation and determinism.
    // ============================================================

    [Test]
    public void NonUrbanTileset_EmitsNoStructuralChannel()
    {
        // The mine-cave theme on its own default (non-urban) tileset: no frontage, no mounts, no
        // placeable structure cells -- the structural channel is scoped entirely behind the
        // city declarations.
        var themes = new MineCaveDungeonDefinition().BuildDungeons();
        var detail = themes[MineCaveDungeonDefinition.ThemeKey];
        var tilesets = new StandardTilesetProfiles().BuildTilesetProfiles();
        foreach (var (k, v) in new BaseGameTilesetProfiles().BuildTilesetProfiles())
            tilesets.TryAdd(k, v);
        DungeonTilesetPaletteInheritance.Apply(tilesets);
        var layouts = new StandardLayoutProfiles().BuildLayoutProfiles();

        var tileset = tilesets[detail.TilesetProfileKey];
        tileset.FrontageBuildings.Should().BeEmpty("only the city family declares frontage");
        var model = TilesetTestSource.LoadTileset(tileset.TilesetResref);

        var composition = new DungeonComposition { Content = detail, Tileset = tileset, Layout = layouts[detail.LayoutProfileKey] };
        var parameters = composition.BuildLayoutParameters();
        parameters.EntranceCount = 1;
        parameters.ExitCount = 1;
        var result = LayoutSolver.Solve(parameters, model, 16, 16, SeedBase, tileset.PrimaryOpenTerrain);
        result.Success.Should().BeTrue();

        var plan = DungeonDecorationPlanner.Plan(result.Resolved, tileset, detail, 100);
        plan.Should().NotContain(p => p.Context == DecorationContext.BuildingFrontage ||
                                      p.Context == DecorationContext.FacadeMount);
        result.Resolved.PlaceableStructureCells.Should().BeEmpty();
    }

    [Test]
    public void StructuralChannel_SameSeed_IsDeterministic_AndRePlanIsIdempotent()
    {
        var c = Composition(MineCaveDungeonDefinition.ThemeKey, BaseGameTilesetProfiles.FutCity, StandardLayoutProfiles.Packed);
        var composition = new DungeonComposition { Content = c.Detail, Tileset = c.Tileset, Layout = c.Layout };
        var parameters = composition.BuildLayoutParameters();
        parameters.EntranceCount = 1;
        parameters.ExitCount = 1;
        parameters.DoorTransitions = true;
        var result = LayoutSolver.Solve(parameters, c.Model, 20, 20, SeedBase, c.Tileset.PrimaryOpenTerrain);
        result.Success.Should().BeTrue();

        // Planning the SAME resolved layout twice must be byte-identical -- the frontage pass
        // publishes PlaceableStructureCells on the layout, and a re-plan must rebuild (never
        // accumulate) that state.
        var a = DungeonDecorationPlanner.Plan(result.Resolved, c.Tileset, c.Detail, 100);
        var cellsA = result.Resolved.PlaceableStructureCells.ToHashSet();
        var b = DungeonDecorationPlanner.Plan(result.Resolved, c.Tileset, c.Detail, 100);
        var cellsB = result.Resolved.PlaceableStructureCells.ToHashSet();

        a.Count.Should().Be(b.Count);
        for (var i = 0; i < a.Count; i++)
        {
            b[i].Resref.Should().Be(a[i].Resref);
            b[i].Position.Should().Be(a[i].Position);
            b[i].Facing.Should().Be(a[i].Facing);
            b[i].Context.Should().Be(a[i].Context);
        }

        cellsB.SetEquals(cellsA).Should().BeTrue("PlaceableStructureCells must be rebuilt identically on re-plan");
        a.Count(p => p.Context == DecorationContext.BuildingFrontage).Should().BeGreaterThan(0);
    }
}
