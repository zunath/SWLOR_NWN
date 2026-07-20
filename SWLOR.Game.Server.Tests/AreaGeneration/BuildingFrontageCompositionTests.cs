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
    /// entry dimensions and cardinal facing (the face's outward normal).</summary>
    private static (float MinX, float MinY, float MaxX, float MaxY) FootprintOf(
        PlannedDecoration p, Dictionary<string, BuildingFrontageEntry> entries)
    {
        var entry = entries[p.Resref];
        var outward = OutwardOf(p);
        var halfX = outward.Dx != 0 ? entry.Depth / 2f : entry.FaceWidth / 2f;
        var halfY = outward.Dx != 0 ? entry.FaceWidth / 2f : entry.Depth / 2f;
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
                    X: p.Position.X + outward.Dx * entry.Depth / 2f,
                    Y: p.Position.Y + outward.Dy * entry.Depth / 2f);
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

            // Accent caps hold (the dominant wall models are deliberately uncapped).
            foreach (var group in buildings.GroupBy(p => p.Resref, StringComparer.OrdinalIgnoreCase))
            {
                if (caps.TryGetValue(group.Key, out var cap))
                    group.Count().Should().BeLessOrEqualTo(cap,
                        $"seed {seed}: accent '{group.Key}' exceeded its per-area frontage cap");
            }
        }
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
