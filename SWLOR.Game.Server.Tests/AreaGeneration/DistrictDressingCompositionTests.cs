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
/// Round-8 district/variety acceptance suite: the district-flavor system (see DistrictFlavor and
/// DungeonDecorationPlanner.AssignDistrictFlavors), the size-aware repetition control
/// (DecorationSize, per-area caps, composed cargo yards), and the vocabulary-expansion variety
/// gates.
///
/// Evidence baseline (round-8 district mining over the 24 hand-built fcx01 areas --
/// _scratch_decor/mine_r8_districts.py): repetition is DISTRICT-SCOPED, not globally uniform.
/// Big cargo concentrates in the industrial shipyard/dock areas and is absent from the commercial
/// promenades; dense hand-built areas use 56-190 distinct decorative resrefs (the round-7
/// generated areas used ~29-34, with an 11.4m storage silo repeated 83x across one area -- the
/// reported "same massive building placeables" feedback this suite guards against regressing).
/// </summary>
public class DistrictDressingCompositionTests
{
    private const int SeedBase = 7001;
    private const int SeedCount = 10;
    private const int Size = 20;

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

    /// <summary>Maps every room tile to that room's assigned district flavor.</summary>
    private static Dictionary<(int X, int Y), DistrictFlavor> TileFlavors(ResolvedLayout layout, string roadCrosser)
    {
        var flavors = DungeonDecorationPlanner.AssignDistrictFlavors(layout, roadCrosser);
        var result = new Dictionary<(int X, int Y), DistrictFlavor>();
        foreach (var room in layout.Rooms)
        {
            if (room.IsSetPiece || !flavors.TryGetValue(room.Id, out var flavor))
                continue;
            foreach (var tile in room.Tiles)
                result.TryAdd(tile, flavor);
        }

        return result;
    }

    private static HashSet<string> HugeResrefs(DungeonTilesetProfile tileset) =>
        tileset.Decorations.Where(d => d.Size == DecorationSize.Huge)
            .Select(d => d.Resref).ToHashSet(StringComparer.OrdinalIgnoreCase);

    // ============================================================
    // District assignment: deterministic, and every flavor represented.
    // ============================================================

    [Test]
    public void AssignDistrictFlavors_IsDeterministic_AndCoversEveryFlavor()
    {
        var c = Composition(MineCaveDungeonDefinition.ThemeKey, BaseGameTilesetProfiles.FutCity, StandardLayoutProfiles.Packed);
        var flavorTotals = new Dictionary<DistrictFlavor, int>();

        for (var i = 0; i < SeedCount; i++)
        {
            var (layout, _) = PlanFor(c, SeedBase + i);
            var first = DungeonDecorationPlanner.AssignDistrictFlavors(layout, "Routes");
            var second = DungeonDecorationPlanner.AssignDistrictFlavors(layout, "Routes");
            second.Should().BeEquivalentTo(first, "district assignment must be deterministic (no RNG)");

            first.Values.Should().NotContain(DistrictFlavor.None, "urban rooms always get a real flavor");
            foreach (var flavor in first.Values)
                flavorTotals[flavor] = flavorTotals.GetValueOrDefault(flavor) + 1;

            // A 20x20 packed city always has enough rooms for real neighborhoods.
            first.Values.Distinct().Count().Should().BeGreaterOrEqualTo(2,
                $"seed {SeedBase + i}: a packed city should read as multiple neighborhoods");
        }

        // Across the seed batch every flavor must appear -- a city generator that never produces
        // an industrial yard (or never a commercial strip) has lost the district system.
        flavorTotals.Keys.Should().Contain(DistrictFlavor.Industrial);
        flavorTotals.Keys.Should().Contain(DistrictFlavor.Commercial);
        flavorTotals.Keys.Should().Contain(DistrictFlavor.Civic);
        TestContext.WriteLine("flavor totals: " + string.Join(", ", flavorTotals.Select(kv => $"{kv.Key}={kv.Value}")));
    }

    // ============================================================
    // District concentration: building-scale art stays in the yards.
    // ============================================================

    [TestCase(MineCaveDungeonDefinition.ThemeKey, StandardLayoutProfiles.Packed)]
    [TestCase(AlienRuinDungeonDefinition.ThemeKey, StandardLayoutProfiles.Halls)]
    public void HugePlacements_ConcentrateInIndustrialZones(string themeKey, string layoutKey)
    {
        var c = Composition(themeKey, BaseGameTilesetProfiles.FutCity, layoutKey);
        var huge = HugeResrefs(c.Tileset);
        huge.Should().NotBeEmpty("fcx01 curates building-scale yard art (kyru08 silo family)");

        var total = 0;
        var inIndustrial = 0;

        for (var i = 0; i < SeedCount; i++)
        {
            var (layout, plan) = PlanFor(c, SeedBase + i);
            var tileFlavors = TileFlavors(layout, "Routes");

            // Structural frontage may legitimately erect the same tower models as canyon walls
            // (round 11 -- BuildingFrontagePlanner, its own gates); this gate is about Huge
            // DRESSING, which still places only through composed cargo-yard rows.
            foreach (var placement in plan.Where(p =>
                         huge.Contains(p.Resref) && p.Context != DecorationContext.BuildingFrontage))
            {
                total++;
                placement.Context.Should().Be(DecorationContext.CargoYard,
                    $"seed {SeedBase + i}: Huge art places ONLY through composed cargo-yard rows");
                if (tileFlavors.TryGetValue(TileOf(placement), out var flavor) && flavor == DistrictFlavor.Industrial)
                    inIndustrial++;
            }
        }

        TestContext.WriteLine($"{themeKey}/{layoutKey}: {inIndustrial}/{total} Huge placements in industrial zones");
        if (total > 0)
        {
            var fraction = (double)inIndustrial / total;
            fraction.Should().BeGreaterOrEqualTo(0.7,
                $"hand-built big cargo is district-scoped: shipyards carry it, promenades carry none (got {fraction:F2})");
        }
    }

    [TestCase(MineCaveDungeonDefinition.ThemeKey, StandardLayoutProfiles.Packed)]
    public void HugePlacements_NeverExceedPerAreaBudget_AndComposeAsSharedBearingRows(string themeKey, string layoutKey)
    {
        var c = Composition(themeKey, BaseGameTilesetProfiles.FutCity, layoutKey);
        var huge = HugeResrefs(c.Tileset);
        var landmarks = c.Tileset.Decorations
            .Where(d => d.Size == DecorationSize.Huge && d.Role == DecorationRole.Landmark)
            .Select(d => d.Resref).ToHashSet(StringComparer.OrdinalIgnoreCase);
        var rowsSeen = 0;

        for (var i = 0; i < SeedCount; i++)
        {
            var (_, plan) = PlanFor(c, SeedBase + i);
            // Structural frontage towers (BuildingFrontage context) are canyon walls, not yard
            // cargo -- the Huge budget and row-composition rules apply to dressing only.
            var hugePlacements = plan
                .Where(p => huge.Contains(p.Resref) && p.Context != DecorationContext.BuildingFrontage)
                .ToList();
            hugePlacements.Count.Should().BeLessOrEqualTo(DungeonDecorationPlanner.MaxHugePerArea,
                $"seed {SeedBase + i}: the hard per-area Huge budget");

            // Row composition: every non-landmark Huge placement belongs to a same-resref,
            // shared-bearing row with a neighbor at the 10m tile pitch (the hand-built silo-row
            // pattern) -- never an isolated one-off and never a cross-model jumble.
            foreach (var placement in hugePlacements.Where(p => !landmarks.Contains(p.Resref)))
            {
                var rowMates = hugePlacements
                    .Where(other => !ReferenceEquals(other, placement) &&
                                    other.Resref.Equals(placement.Resref, StringComparison.OrdinalIgnoreCase) &&
                                    Math.Abs(other.Facing - placement.Facing) < 0.5f &&
                                    Vector2.Distance(
                                        new Vector2(other.Position.X, other.Position.Y),
                                        new Vector2(placement.Position.X, placement.Position.Y)) <= 10.5f)
                    .ToList();
                rowMates.Should().NotBeEmpty(
                    $"seed {SeedBase + i}: Huge '{placement.Resref}' must stand in a composed row/pair, not alone");
            }

            if (hugePlacements.Count > 0)
                rowsSeen++;
        }

        rowsSeen.Should().BeGreaterThan(0, "the seed batch should produce industrial yards");
    }

    // ============================================================
    // Commercial rows exist along road frontage.
    // ============================================================

    [Test]
    public void CommercialRooms_AreRoadFronted_AndCarryMarketDressing()
    {
        var c = Composition(MineCaveDungeonDefinition.ThemeKey, BaseGameTilesetProfiles.FutCity, StandardLayoutProfiles.Packed);
        var commercialResrefs = c.Tileset.Decorations
            .Where(d => d.DistrictWeights.ContainsKey(DistrictFlavor.Commercial))
            .Select(d => d.Resref)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
        commercialResrefs.Should().NotBeEmpty();

        var areasWithCommercialDressing = 0;

        for (var i = 0; i < SeedCount; i++)
        {
            var (layout, plan) = PlanFor(c, SeedBase + i);
            var flavors = DungeonDecorationPlanner.AssignDistrictFlavors(layout, "Routes");
            var commercialRooms = layout.Rooms
                .Where(r => !r.IsSetPiece && flavors.TryGetValue(r.Id, out var f) && f == DistrictFlavor.Commercial)
                .ToList();
            commercialRooms.Should().NotBeEmpty($"seed {SeedBase + i}: every packed city needs a commercial strip");

            // Commercial rooms front the roads: at least one commercial room touches the carved
            // road network (the promenade pattern -- commerce lines the walkways players travel).
            commercialRooms.Any(r => r.Tiles.Any(t => DungeonDecorationPlanner.IsRoadAdjacent(t, layout, "Routes")))
                .Should().BeTrue($"seed {SeedBase + i}: commercial rooms should have road frontage");

            var commercialTiles = commercialRooms.SelectMany(r => r.Tiles).ToHashSet();
            var dressed = plan.Count(p => commercialTiles.Contains(TileOf(p)) && commercialResrefs.Contains(p.Resref));
            if (dressed >= 3)
                areasWithCommercialDressing++;
        }

        areasWithCommercialDressing.Should().BeGreaterOrEqualTo(SeedCount - 1,
            "commercial rooms should visibly carry their kiosk/bench/signage vocabulary");
    }

    // ============================================================
    // Variety: the round-8 vocabulary-expansion gates.
    // ============================================================

    [Test]
    public void FutCityPacked_VarietyMeetsHandBuiltScale()
    {
        var c = Composition(MineCaveDungeonDefinition.ThemeKey, BaseGameTilesetProfiles.FutCity, StandardLayoutProfiles.Packed);
        var decalResrefs = c.Tileset.Decorations
            .Where(d => d.Role == DecorationRole.GroundDecal)
            .Select(d => d.Resref)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        for (var i = 0; i < SeedCount; i++)
        {
            var (_, plan) = PlanFor(c, SeedBase + i);
            var counts = plan.GroupBy(p => p.Resref).ToDictionary(g => g.Key, g => g.Count());

            // Hand-built dense fcx01 areas use 56-190 distinct decorative resrefs; the round-7
            // generated areas measured 27-34 (the reported repetition).
            counts.Count.Should().BeGreaterOrEqualTo(45,
                $"seed {SeedBase + i}: distinct-resref variety must reach the hand-built scale (got {counts.Count})");

            // Top-1 NON-DECAL share: hand-built dense areas' top resref (always a floor decal or
            // lamp) holds 0.10-0.22; the round-7 worst offenders were building-scale cargo.
            var nonDecal = counts.Where(kv => !decalResrefs.Contains(kv.Key)).ToList();
            var total = nonDecal.Sum(kv => kv.Value);
            var top1 = nonDecal.Max(kv => kv.Value) / (double)total;
            top1.Should().BeLessOrEqualTo(0.20,
                $"seed {SeedBase + i}: no single non-decal fixture may dominate (got {top1:F3})");
        }
    }

    [TestCase(MineCaveDungeonDefinition.ThemeKey, StandardLayoutProfiles.Packed, null)]
    [TestCase(AlienRuinDungeonDefinition.ThemeKey, StandardLayoutProfiles.Halls, null)]
    [TestCase(AlienRuinDungeonDefinition.ThemeKey, StandardLayoutProfiles.Halls, "ruined")]
    public void PerAreaCaps_AreRespectedWithinArrangementSlack(string themeKey, string layoutKey, string profile)
    {
        var c = Composition(themeKey, BaseGameTilesetProfiles.FutCity, layoutKey);
        var entries = profile != null && c.Tileset.DecorationProfiles.TryGetValue(profile, out var named)
            ? named.Decorations
            : c.Tileset.Decorations;
        var caps = entries
            .Where(e => e.MaxPerArea > 0)
            .GroupBy(e => e.Resref, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(g => g.Key, g => g.Min(e => e.MaxPerArea), StringComparer.OrdinalIgnoreCase);
        if (caps.Count == 0)
            Assert.Ignore("no capped entries in this palette");

        for (var i = 0; i < SeedCount; i++)
        {
            var (_, plan) = PlanFor(c, SeedBase + i, profile);
            // Palette MaxPerArea caps govern the dressing mechanisms; the structural channel
            // (frontage buildings, facade mounts) carries its own per-area caps on
            // BuildingFrontageEntry and is asserted by BuildingFrontageCompositionTests.
            var dressing = plan.Where(p => p.Context is not
                (DecorationContext.BuildingFrontage or DecorationContext.FacadeMount));
            foreach (var group in dressing.GroupBy(p => p.Resref, StringComparer.OrdinalIgnoreCase))
            {
                if (!caps.TryGetValue(group.Key, out var cap))
                    continue;

                // Caps are enforced strictly at pick time by runs/piles/flanks/yards; a committed
                // courtyard ring may overshoot by at most one ring's members (its motif is drawn
                // before the ring commits).
                group.Count().Should().BeLessOrEqualTo(cap + DungeonDecorationPlanner.CourtyardMaxRingItems,
                    $"seed {SeedBase + i}: '{group.Key}' exceeded its per-area cap {cap} (got {group.Count()})");
            }
        }
    }

    // ============================================================
    // Palette audit: Huge art is industrial-only and yard-only by declaration.
    // ============================================================

    [Test]
    public void HugeEntries_DeclareIndustrialOnlyAffinity_AndPerAreaCaps()
    {
        var c = Composition(MineCaveDungeonDefinition.ThemeKey, BaseGameTilesetProfiles.FutCity, StandardLayoutProfiles.Packed);
        var hugeEntries = c.Tileset.Decorations.Where(d => d.Size == DecorationSize.Huge).ToList();
        hugeEntries.Should().NotBeEmpty();

        foreach (var entry in hugeEntries)
        {
            entry.DistrictWeights.Keys.Should().BeEquivalentTo(new[] { DistrictFlavor.Industrial },
                $"'{entry.Resref}' is building-scale and belongs to industrial yards only");
            entry.MaxPerArea.Should().BeGreaterThan(0,
                $"'{entry.Resref}' is building-scale and needs a hard per-area cap");
        }
    }

    // ============================================================
    // Non-urban tilesets: no district assignment side effects.
    // ============================================================

    [Test]
    public void NonUrbanTileset_PlansIdenticallyRegardlessOfDistrictMetadata()
    {
        // The cavern (tdt01) family declares no urban grammar: its plan must not consult district
        // metadata at all. Byte-level identity against the pre-district planner is additionally
        // verified by the scratch harness hash comparison (r8_base_* vs r8_after_*); here we pin
        // determinism and the absence of CargoYard output for a non-urban family.
        var c = Composition(MineCaveDungeonDefinition.ThemeKey, StandardTilesetProfiles.Cavern, StandardLayoutProfiles.Complex);

        for (var i = 0; i < 3; i++)
        {
            var (_, first) = PlanFor(c, SeedBase + i);
            var (_, second) = PlanFor(c, SeedBase + i);
            second.Should().BeEquivalentTo(first, options => options.WithStrictOrdering());
            first.Should().NotContain(p => p.Context == DecorationContext.CargoYard,
                "non-urban tilesets never compose cargo yards");
        }
    }
}
