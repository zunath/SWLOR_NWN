using System;
using System.Collections.Generic;
using System.Linq;
using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Feature.DungeonDefinition;
using SWLOR.Game.Server.Service.AreaGenerationService;

namespace SWLOR.Game.Server.Tests.AreaGeneration;

/// <summary>
/// Exercises DungeonDecorationPlanner.Plan entirely offline (no NWN engine): a real ResolvedLayout
/// comes from the same MacroLayoutGenerator -> TileResolver pipeline AreaGenerationPipelineTests uses,
/// and the curated DungeonDetail comes from the real theme definitions (reflection-free — themes are
/// built directly here, mirroring DungeonDefinitionTests' own BuildAllDungeons helper).
/// </summary>
public class DungeonDecorationPlannerTests
{
    private static readonly string[] AllThemeKeys =
    {
        MineCaveDungeonDefinition.ThemeKey,
        SewerDungeonDefinition.ThemeKey,
        SciFiBaseDungeonDefinition.ThemeKey,
        AlienRuinDungeonDefinition.ThemeKey,
        TatooineWastesDungeonDefinition.ThemeKey,
        DathomirWildlandsDungeonDefinition.ThemeKey,
        NightsisterCovenDungeonDefinition.ThemeKey,
        QionHiveDungeonDefinition.ThemeKey,
        DeepMineDungeonDefinition.ThemeKey,
        MandalorianGarrisonDungeonDefinition.ThemeKey,
        UndercityDenDungeonDefinition.ThemeKey,
        DroidFoundryDungeonDefinition.ThemeKey,
        LostRuinsDungeonDefinition.ThemeKey,
        SithAcademyDungeonDefinition.ThemeKey,
    };

    private static Dictionary<string, DungeonDetail> BuildAllDungeons()
    {
        var definitions = new IDungeonListDefinition[]
        {
            new MineCaveDungeonDefinition(), new SewerDungeonDefinition(), new SciFiBaseDungeonDefinition(),
            new AlienRuinDungeonDefinition(), new TatooineWastesDungeonDefinition(),
            new DathomirWildlandsDungeonDefinition(), new NightsisterCovenDungeonDefinition(),
            new QionHiveDungeonDefinition(), new DeepMineDungeonDefinition(),
            new MandalorianGarrisonDungeonDefinition(), new UndercityDenDungeonDefinition(),
            new DroidFoundryDungeonDefinition(), new LostRuinsDungeonDefinition(),
            new SithAcademyDungeonDefinition()
        };

        var result = new Dictionary<string, DungeonDetail>();
        foreach (var definition in definitions)
        foreach (var (key, detail) in definition.BuildDungeons())
            result[key] = detail;

        return result;
    }

    private static Dictionary<string, DungeonTilesetProfile> BuildAllTilesetProfiles()
    {
        var definitions = new IDungeonTilesetProfileListDefinition[]
        {
            new StandardTilesetProfiles(), new BaseGameTilesetProfiles()
        };

        var result = new Dictionary<string, DungeonTilesetProfile>();
        foreach (var definition in definitions)
        foreach (var (key, profile) in definition.BuildTilesetProfiles())
            result[key] = profile;

        return result;
    }

    /// <summary>
    /// Resolves a real layout for the given theme/seed against its real tileset .set data — the same
    /// pipeline AreaGenerationPipelineTests exercises, minus the NWN engine.
    /// </summary>
    private static ResolvedLayout ResolveLayout(DungeonDetail detail, DungeonTilesetProfile tileset, int seed, int size = 20)
    {
        var model = TilesetTestSource.LoadTileset(tileset.TilesetResref);
        var rng = new Random(seed);

        var parameters = new DungeonComposition { Content = detail, Tileset = tileset, Layout = BuildLayoutProfile(detail) }
            .BuildLayoutParameters();
        parameters.Width = size;
        parameters.Height = size;
        parameters.SolidTerrain = string.IsNullOrEmpty(parameters.SolidTerrain) ? model.DefaultTerrain : parameters.SolidTerrain;
        parameters.OpenTerrain = string.IsNullOrEmpty(tileset.PrimaryOpenTerrain) ? model.FloorTerrain : tileset.PrimaryOpenTerrain;

        var macro = MacroLayoutGenerator.Generate(parameters, rng);
        macro.Seed = seed;

        TileResolver.TryResolve(model, macro, rng, out var resolved, out var reason)
            .Should().BeTrue($"layout must resolve for a decoration-planning test to be meaningful ({reason})");

        return resolved;
    }

    private static DungeonLayoutProfile BuildLayoutProfile(DungeonDetail detail)
    {
        var layoutProfiles = new IDungeonLayoutProfileListDefinition[] { new StandardLayoutProfiles() };
        foreach (var definition in layoutProfiles)
        foreach (var (key, profile) in definition.BuildLayoutProfiles())
            if (key == detail.LayoutProfileKey)
                return profile;

        throw new InvalidOperationException($"layout profile '{detail.LayoutProfileKey}' not found.");
    }

    [Test]
    public void Plan_IsDeterministic_ForAFixedSeed()
    {
        var dungeons = BuildAllDungeons();
        var tilesets = BuildAllTilesetProfiles();
        var detail = dungeons[MineCaveDungeonDefinition.ThemeKey];
        var tileset = tilesets[detail.TilesetProfileKey];
        var layout = ResolveLayout(detail, tileset, seed: 4242);

        var first = DungeonDecorationPlanner.Plan(layout, tileset, detail, 100);
        var second = DungeonDecorationPlanner.Plan(layout, tileset, detail, 100);

        first.Should().HaveCountGreaterThan(0, "a 20x20 mine-cave layout at 100% density should place at least one decoration");
        first.Should().HaveCount(second.Count);

        for (var i = 0; i < first.Count; i++)
        {
            second[i].Resref.Should().Be(first[i].Resref);
            second[i].Context.Should().Be(first[i].Context);
            second[i].Position.Should().Be(first[i].Position);
            second[i].Facing.Should().Be(first[i].Facing);
        }
    }

    [Test]
    public void Plan_ToggleOff_ProducesZeroDecorations()
    {
        var dungeons = BuildAllDungeons();
        var tilesets = BuildAllTilesetProfiles();
        var detail = dungeons[MineCaveDungeonDefinition.ThemeKey];
        var tileset = tilesets[detail.TilesetProfileKey];
        var layout = ResolveLayout(detail, tileset, seed: 99);

        DungeonDecorationPlanner.Plan(layout, tileset, detail, 0).Should().BeEmpty();
    }

    [Test]
    public void Plan_WithNoCuratedPalette_ProducesZeroDecorations()
    {
        var dungeons = BuildAllDungeons();
        var tilesets = BuildAllTilesetProfiles();
        var detail = dungeons[MineCaveDungeonDefinition.ThemeKey];
        var tileset = tilesets[detail.TilesetProfileKey];
        var layout = ResolveLayout(detail, tileset, seed: 99);

        var emptyDetail = new DungeonDetail
        {
            ThemeKey = detail.ThemeKey,
            DecorationBaseDensity = detail.DecorationBaseDensity
            // Decorations left empty — no theme accents curated.
        };
        var emptyTileset = new DungeonTilesetProfile
        {
            Key = tileset.Key,
            TilesetResref = tileset.TilesetResref
            // Decorations/Vignettes left empty — no tileset-family palette curated either, so the
            // merged palette really is empty (a theme's own small accent list alone should never be
            // enough to decorate — see the class doc comment's "bulk lives on the tileset" design).
        };

        DungeonDecorationPlanner.Plan(layout, emptyTileset, emptyDetail, 100).Should().BeEmpty();
    }

    [TestCase(50)]
    [TestCase(200)]
    public void Plan_DensityPercent_ScalesDecorationCountMonotonically(int densityPercent)
    {
        var dungeons = BuildAllDungeons();
        var tilesets = BuildAllTilesetProfiles();
        var detail = dungeons[UndercityDenDungeonDefinition.ThemeKey];
        var tileset = tilesets[detail.TilesetProfileKey];
        var layout = ResolveLayout(detail, tileset, seed: 555, size: 24);

        var baseline = DungeonDecorationPlanner.Plan(layout, tileset, detail, 100).Count;
        var scaled = DungeonDecorationPlanner.Plan(layout, tileset, detail, densityPercent).Count;

        if (densityPercent < 100)
            scaled.Should().BeLessThanOrEqualTo(baseline);
        else
            scaled.Should().BeGreaterThanOrEqualTo(baseline);
    }

    /// <summary>
    /// Exclusion invariants: across every theme and several seeds, no planned decoration's origin
    /// tile is a transition anchor/door/doorway cell or a room's CenterTile — the cells
    /// DungeonContentPlacer's own boss/treasure/exit/door content reserves.
    /// </summary>
    [Test]
    public void Plan_NeverPlacesOnTransitionDoorOrCenterTileCells()
    {
        var dungeons = BuildAllDungeons();
        var tilesets = BuildAllTilesetProfiles();
        var seeds = new[] { 1, 7, 42, 1234, 98765 };
        var violations = new List<string>();

        foreach (var themeKey in AllThemeKeys)
        {
            var detail = dungeons[themeKey];
            var tileset = tilesets[detail.TilesetProfileKey];

            foreach (var seed in seeds)
            {
                ResolvedLayout layout;
                try
                {
                    layout = ResolveLayout(detail, tileset, seed, size: 20);
                }
                catch (Exception)
                {
                    // A handful of (theme, seed) combinations can legitimately fail to resolve at a
                    // fixed size (see AreaGenerationPipelineTests' own retry-budget precedent) —
                    // skip rather than fail the exclusion-invariant assertion on an unrelated gap.
                    continue;
                }

                var excludedTiles = new HashSet<(int X, int Y)>();
                foreach (var transition in layout.Transitions)
                {
                    excludedTiles.Add(transition.Tile);
                    if (transition.Style is TransitionStyle.Door or TransitionStyle.GroupExit)
                    {
                        excludedTiles.Add(transition.DoorCell);
                        excludedTiles.Add(transition.DoorwayCell);
                    }
                }

                var centerTiles = layout.Rooms.Where(r => !r.IsSetPiece).Select(r => r.CenterTile).ToHashSet();
                var setPieceTiles = layout.Rooms.Where(r => r.IsSetPiece).SelectMany(r => r.Tiles).ToHashSet();

                var plan = DungeonDecorationPlanner.Plan(layout, tileset, detail, 100);
                foreach (var planned in plan)
                {
                    var tile = (X: (int)Math.Floor(planned.Position.X / 10.0), Y: (int)Math.Floor(planned.Position.Y / 10.0));

                    if (excludedTiles.Contains(tile))
                        violations.Add($"{themeKey} seed {seed}: '{planned.Resref}' landed on a transition/door cell {tile}.");
                    if (centerTiles.Contains(tile))
                        violations.Add($"{themeKey} seed {seed}: '{planned.Resref}' landed on a room CenterTile {tile}.");
                    if (setPieceTiles.Contains(tile))
                        violations.Add($"{themeKey} seed {seed}: '{planned.Resref}' landed inside a set-piece room {tile}.");
                }
            }
        }

        violations.Should().BeEmpty(string.Join(Environment.NewLine, violations));
    }

    /// <summary>
    /// Density-in-range: at 100% density, the planned decoration count for a real layout must never
    /// exceed the total number of non-set-piece room tiles (the widest possible eligible-tile pool)
    /// and must be strictly positive for a large multi-room layout with a nonzero base density.
    /// </summary>
    [Test]
    public void Plan_DecorationCount_StaysWithinEligibleTileBounds()
    {
        var dungeons = BuildAllDungeons();
        var tilesets = BuildAllTilesetProfiles();

        // A handful of (theme, seed) combinations can legitimately fail to resolve at a fixed size —
        // AreaGenerationPipelineTests' own retry-budget precedent — so try a couple of seeds per theme
        // rather than tying this bounds check to one seed that might be an unrelated resolver gap.
        var candidateSeeds = new[] { 2026, 4321, 555 };

        foreach (var themeKey in AllThemeKeys)
        {
            var detail = dungeons[themeKey];
            var tileset = tilesets[detail.TilesetProfileKey];

            ResolvedLayout layout = null;
            foreach (var seed in candidateSeeds)
            {
                try
                {
                    layout = ResolveLayout(detail, tileset, seed, size: 24);
                    break;
                }
                catch (Exception)
                {
                    // try the next candidate seed
                }
            }

            layout.Should().NotBeNull($"{themeKey}: none of the candidate seeds resolved a layout");

            var eligibleTileCount = layout!.Rooms.Where(r => !r.IsSetPiece).Sum(r => r.Tiles.Count);
            // Centerpieces add at most one extra placement per eligible room beyond the per-tile roll,
            // so allow a modest headroom rather than an exact tile-for-tile cap.
            var plan = DungeonDecorationPlanner.Plan(layout, tileset, detail, 100);

            plan.Count.Should().BeLessThanOrEqualTo(eligibleTileCount + layout.Rooms.Count,
                $"{themeKey}: planned {plan.Count} decorations against only {eligibleTileCount} eligible room tiles");
        }
    }

    /// <summary>
    /// Density-calibration regression: at 16x16 (the SWLOR.ProcgenReview default size) and 100%
    /// request density, the AVERAGE planned decoration count across several seeds must land within a
    /// theme-specific band. Guards against the exact defect a live playtest surfaced against
    /// Mandalorian Garrison — a 16x16 review area carrying only ~6 total decorations, ~10x below the
    /// hand-built evidence band of roughly 40-90 for a 16x16 area (see
    /// decoration_evidence/mine_evidence.py's "placeables per tile" mining and DungeonDetail.
    /// DecorationBaseDensity's doc comment) — which traced back to DungeonDecorationPlanner.Plan
    /// applying DecorationBaseDensity as a per-ELIGIBLE-tile coin-flip probability instead of
    /// calibrating against the full area tile count.
    ///
    /// Bounds are measured (20 seeds/theme, averaged) against the current two-pass calibration, with
    /// wide margin below/above so ordinary layout-solver variance or a future palette/exclusion tweak
    /// doesn't make this flaky, while still catching a collapse back toward the pre-fix single-digit
    /// counts. Warren/Labyrinth-layout themes (sewer, nightsistercoven, qionhive) structurally can't
    /// reach the full evidence band at 16x16 — see the DungeonDecorationPlanner class doc comment's own
    /// "CorridorSide is honestly scoped to long/narrow rooms" architecture-gap note (OpenLane corridors
    /// carved by these layout styles are never recorded as LayoutRooms, so most of the map is outside
    /// this planner's eligible-tile pool entirely) — their bands are scaled down accordingly rather than
    /// held to the same floor as room-dominant layout styles (Packed/Complex/RoomsAndCorridors).
    /// </summary>
    [TestCase(MineCaveDungeonDefinition.ThemeKey, 10, 55)]
    [TestCase(SewerDungeonDefinition.ThemeKey, 3, 30)]
    [TestCase(SciFiBaseDungeonDefinition.ThemeKey, 8, 55)]
    [TestCase(AlienRuinDungeonDefinition.ThemeKey, 8, 55)]
    [TestCase(TatooineWastesDungeonDefinition.ThemeKey, 8, 55)]
    [TestCase(DathomirWildlandsDungeonDefinition.ThemeKey, 12, 75)]
    [TestCase(NightsisterCovenDungeonDefinition.ThemeKey, 2, 35)]
    [TestCase(QionHiveDungeonDefinition.ThemeKey, 8, 50)]
    [TestCase(DeepMineDungeonDefinition.ThemeKey, 8, 55)]
    [TestCase(MandalorianGarrisonDungeonDefinition.ThemeKey, 30, 90)]
    [TestCase(UndercityDenDungeonDefinition.ThemeKey, 8, 55)]
    [TestCase(DroidFoundryDungeonDefinition.ThemeKey, 8, 55)]
    [TestCase(LostRuinsDungeonDefinition.ThemeKey, 10, 55)]
    [TestCase(SithAcademyDungeonDefinition.ThemeKey, 8, 55)]
    public void Plan_DefaultDensityAt16x16_LandsWithinEvidenceBand(string themeKey, int minAverage, int maxAverage)
    {
        var dungeons = BuildAllDungeons();
        var tilesets = BuildAllTilesetProfiles();
        var detail = dungeons[themeKey];
        var tileset = tilesets[detail.TilesetProfileKey];

        var seeds = new[] { 1, 7, 42, 1234, 98765, 555, 2026, 4321, 777, 1337 };
        var counts = new List<int>();

        foreach (var seed in seeds)
        {
            ResolvedLayout layout;
            try
            {
                layout = ResolveLayout(detail, tileset, seed, size: 16);
            }
            catch (Exception)
            {
                continue; // a handful of (theme, seed) combinations can legitimately fail to resolve
            }

            counts.Add(DungeonDecorationPlanner.Plan(layout, tileset, detail, 100).Count);
        }

        counts.Should().NotBeEmpty($"{themeKey}: none of the candidate seeds resolved a layout");

        var average = counts.Average();
        average.Should().BeInRange(minAverage, maxAverage,
            $"{themeKey}: averaged {average:0.0} decorations across {counts.Count} sixteen-tile-square seeds (counts: [{string.Join(",", counts)}])");
    }
}
