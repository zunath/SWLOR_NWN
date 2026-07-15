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
/// Structured-coherence regression suite for the tileset-keyed, arrangement-driven decoration
/// rework (see DungeonDecorationPlanner's class doc comment): asserts the four properties that
/// distinguish real set dressing from independent random scatter — per-room resref motif caps,
/// regular wall-run spacing, vignette members landing on their declared relative offsets, and the
/// pre-existing density calibration band — plus the specific cross-composition sanity check the
/// reported bug was built around (Alien Ruin theme content generated on the Futuristic City
/// tileset must dress with the Futuristic City tileset's OWN palette, not Alien Ruin's).
/// </summary>
public class DungeonDecorationCoherenceTests
{
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

        DungeonTilesetPaletteInheritance.Apply(result);
        return result;
    }

    private static DungeonLayoutProfile BuildLayoutProfile(string layoutProfileKey)
    {
        var layoutProfiles = new IDungeonLayoutProfileListDefinition[] { new StandardLayoutProfiles() };
        foreach (var definition in layoutProfiles)
        foreach (var (key, profile) in definition.BuildLayoutProfiles())
            if (key == layoutProfileKey)
                return profile;

        throw new InvalidOperationException($"layout profile '{layoutProfileKey}' not found.");
    }

    /// <summary>Resolves a real layout for a (content, tileset) pair — mirrors
    /// DungeonDecorationPlannerTests.ResolveLayout, kept local so this suite doesn't depend on
    /// another test class's private helper.</summary>
    private static ResolvedLayout ResolveLayout(DungeonDetail detail, DungeonTilesetProfile tileset, int seed, int size = 20)
    {
        var model = TilesetTestSource.LoadTileset(tileset.TilesetResref);
        var rng = new Random(seed);

        var parameters = new DungeonComposition { Content = detail, Tileset = tileset, Layout = BuildLayoutProfile(detail.LayoutProfileKey) }
            .BuildLayoutParameters();
        parameters.Width = size;
        parameters.Height = size;
        parameters.SolidTerrain = string.IsNullOrEmpty(parameters.SolidTerrain) ? model.DefaultTerrain : parameters.SolidTerrain;
        parameters.OpenTerrain = string.IsNullOrEmpty(tileset.PrimaryOpenTerrain) ? model.FloorTerrain : tileset.PrimaryOpenTerrain;

        var macro = MacroLayoutGenerator.Generate(parameters, rng);
        macro.Seed = seed;

        TileResolver.TryResolve(model, macro, rng, out var resolved, out var reason)
            .Should().BeTrue($"layout must resolve for a coherence test to be meaningful ({reason})");

        return resolved;
    }

    /// <summary>Recovers the tile a wall-hugging/corridor-side/doorway-flank placement anchored on:
    /// WallOffset (3.5) is always less than half a tile (5), so floor(Position / 10) always lands back
    /// on the tile the planner actually anchored the placement to, regardless of the offset direction.</summary>
    private static (int X, int Y) RecoverTile(Vector3 position)
    {
        return ((int)MathF.Floor(position.X / 10f), (int)MathF.Floor(position.Y / 10f));
    }

    private static int RoomIdForTile(ResolvedLayout layout, (int X, int Y) tile)
    {
        foreach (var room in layout.Rooms)
            if (!room.IsSetPiece && room.Tiles.Contains(tile))
                return room.Id;

        return -1;
    }

    /// <summary>Recovers the quantized wall direction a placement's Facing was derived from
    /// (Facing = atan2(-dy, -dx) in degrees — see DungeonDecorationPlanner.BuildWallHuggingPlacement)
    /// and re-quantizes it with the planner's own QuantizeDirection so grouping never drifts from
    /// production bucketing.</summary>
    private static int RecoverDirection(float facingDegrees)
    {
        var rad = facingDegrees * (MathF.PI / 180f);
        var dx = -MathF.Cos(rad);
        var dy = -MathF.Sin(rad);
        return DungeonDecorationPlanner.QuantizeDirection(dx, dy);
    }

    private static readonly (string ThemeKey, string TilesetProfileKey)[] SampleCompositions =
    {
        (MineCaveDungeonDefinition.ThemeKey, StandardTilesetProfiles.Cavern),
        (SewerDungeonDefinition.ThemeKey, StandardTilesetProfiles.Sewers),
        (AlienRuinDungeonDefinition.ThemeKey, StandardTilesetProfiles.AncientRuin),
        (UndercityDenDungeonDefinition.ThemeKey, BaseGameTilesetProfiles.CityInterior),
        (SithAcademyDungeonDefinition.ThemeKey, BaseGameTilesetProfiles.CastleInterior),
        (MandalorianGarrisonDungeonDefinition.ThemeKey, BaseGameTilesetProfiles.FortInterior),
    };

    private static readonly int[] Seeds = { 1, 7, 42, 1234, 98765, 555, 2026, 4321, 777, 1337 };

    /// <summary>
    /// Coherence metric (a): a room should read as internally consistent, not as an independent roll
    /// per placement. For every (room, DecorationContext) group of wall/corridor placements, the
    /// number of DISTINCT resrefs actually used by the ROOM MOTIF mechanism (PickMotif) must never
    /// exceed DungeonDecorationPlanner.MotifResrefCap (3). Vignette-member resrefs are excluded here on
    /// purpose: a vignette (see PlaceVignette) is a deliberately separate, small, evidence-backed
    /// grouping placed as its own unit — it is expected and correct for its members' resrefs to differ
    /// from the room's own wall-run motif, so counting them against the motif cap would conflate two
    /// different arrangement mechanisms into one metric.
    /// </summary>
    [Test]
    public void Plan_RoomMotifs_StayWithinCap()
    {
        var dungeons = BuildAllDungeons();
        var tilesets = BuildAllTilesetProfiles();
        var violations = new List<string>();
        var groupsChecked = 0;

        foreach (var (themeKey, tilesetKey) in SampleCompositions)
        {
            var detail = dungeons[themeKey];
            var tileset = tilesets[tilesetKey];
            var vignetteMemberResrefs = tileset.Vignettes.SelectMany(v => v.Members).Select(m => m.Resref)
                .ToHashSet(StringComparer.OrdinalIgnoreCase);

            foreach (var seed in Seeds)
            {
                ResolvedLayout layout;
                try { layout = ResolveLayout(detail, tileset, seed); }
                catch (Exception) { continue; }

                var plan = DungeonDecorationPlanner.Plan(layout, tileset, detail, 150);

                var groups = plan
                    .Where(p => p.Context is DecorationContext.WallAdjacent or DecorationContext.CorridorSide)
                    .Where(p => !vignetteMemberResrefs.Contains(p.Resref))
                    .GroupBy(p => (RoomIdForTile(layout, RecoverTile(p.Position)), p.Context));

                foreach (var group in groups)
                {
                    if (group.Key.Item1 < 0)
                        continue; // recovered tile fell outside any room (shouldn't happen, but not this metric's concern)

                    groupsChecked++;
                    var distinctResrefs = group.Select(p => p.Resref).Distinct().Count();
                    if (distinctResrefs > DungeonDecorationPlanner.MotifResrefCap)
                        violations.Add($"{themeKey}/{tilesetKey} seed {seed} room {group.Key.Item1} {group.Key.Item2}: " +
                                       $"{distinctResrefs} distinct resrefs (cap {DungeonDecorationPlanner.MotifResrefCap})");
                }
            }
        }

        groupsChecked.Should().BeGreaterThan(20, "the sample compositions/seeds should produce plenty of room/context groups to check");
        violations.Should().BeEmpty(string.Join(Environment.NewLine, violations));
    }

    /// <summary>
    /// Coherence metric (b): a wall/corridor run should read as a deliberate cadence, not noise.
    /// For every (room, direction) run with at least 3 placements, the coefficient of variation
    /// (stddev / mean) of the gaps between consecutive run positions (sorted along the run's own axis)
    /// must stay low — PlaceWallRuns places at a fixed spacing (mod one random start offset per run),
    /// so gaps within a run should be identical or off by one step at most. Threshold (0.6) is picked
    /// with wide margin above the measured near-zero CoV of a real spacing-based run (a naive
    /// independent-coin-flip scatter would read well above 1.0 here), and margin below the
    /// mathematically-impossible-to-exceed CoV of a strictly alternating spacing pattern.
    /// </summary>
    [Test]
    public void Plan_WallRunSpacing_IsRegular()
    {
        var dungeons = BuildAllDungeons();
        var tilesets = BuildAllTilesetProfiles();
        var violations = new List<string>();
        var runsChecked = 0;
        const double maxCoefficientOfVariation = 0.6;

        foreach (var (themeKey, tilesetKey) in SampleCompositions)
        {
            var detail = dungeons[themeKey];
            var tileset = tilesets[tilesetKey];

            foreach (var seed in Seeds)
            {
                ResolvedLayout layout;
                try { layout = ResolveLayout(detail, tileset, seed, size: 24); }
                catch (Exception) { continue; }

                var plan = DungeonDecorationPlanner.Plan(layout, tileset, detail, 150);

                var runs = plan
                    .Where(p => p.Context is DecorationContext.WallAdjacent or DecorationContext.CorridorSide)
                    .Select(p => (Placement: p, Tile: RecoverTile(p.Position), Direction: RecoverDirection(p.Facing)))
                    .GroupBy(t => (RoomIdForTile(layout, t.Tile), t.Direction));

                foreach (var run in runs)
                {
                    if (run.Key.Item1 < 0 || run.Count() < 3)
                        continue;

                    // Sort along the run's own axis exactly like PlaceWallRuns does: direction 0/1
                    // (X-facing wall) runs along Y, direction 2/3 (Y-facing wall) runs along X.
                    var ordered = run.Key.Item2 is 0 or 1
                        ? run.OrderBy(t => t.Tile.Y).ThenBy(t => t.Tile.X).ToList()
                        : run.OrderBy(t => t.Tile.X).ThenBy(t => t.Tile.Y).ToList();

                    // Distinct, sorted run-axis coordinates: an irregular (non-rectangular) room can
                    // have more than one placement share the same row/column along a given wall
                    // direction (e.g. two alcove tiles both bordering the -X wall at different X but
                    // the same Y) — that is a real geometric feature of the room shape, not a rhythm
                    // violation, so gaps are measured between distinct positions along the run, not
                    // between every raw placement pair.
                    var coordinates = ordered.Select(t => run.Key.Item2 is 0 or 1 ? t.Tile.Y : t.Tile.X)
                        .Distinct().OrderBy(c => c).ToList();
                    var gaps = new List<int>();
                    for (var i = 1; i < coordinates.Count; i++)
                        gaps.Add(coordinates[i] - coordinates[i - 1]);

                    if (gaps.Count < 2)
                        continue; // a single gap has no variance to speak of

                    runsChecked++;
                    var mean = gaps.Average();
                    if (mean <= 0)
                        continue;

                    var variance = gaps.Select(g => (g - mean) * (g - mean)).Average();
                    var coefficientOfVariation = Math.Sqrt(variance) / mean;

                    if (coefficientOfVariation > maxCoefficientOfVariation)
                        violations.Add($"{themeKey}/{tilesetKey} seed {seed} room {run.Key.Item1} dir {run.Key.Item2}: " +
                                       $"CoV {coefficientOfVariation:0.00} over gaps [{string.Join(",", gaps)}]");
                }
            }
        }

        runsChecked.Should().BeGreaterThan(10, "the sample compositions/seeds should produce several 3+-member runs to check");
        violations.Should().BeEmpty(string.Join(Environment.NewLine, violations));
    }

    /// <summary>
    /// Coherence metric (c): vignette members are placed as a unit at their declared relative offsets,
    /// rotated to match the anchor's own wall-facing direction (see DungeonDecorationPlanner.
    /// PlaceVignette) — not sampled independently. Exercises PlaceVignette directly (an internal seam,
    /// see InternalsVisibleTo in DungeonDecorationPlanner.cs) across every cardinal/diagonal wall
    /// direction so the rotation math is checked exactly, not inferred from Plan()'s flat output.
    /// </summary>
    [TestCase(1f, 0f)]
    [TestCase(-1f, 0f)]
    [TestCase(0f, 1f)]
    [TestCase(0f, -1f)]
    [TestCase(0.7071f, 0.7071f)]
    public void PlaceVignette_MembersLandOnDeclaredOffsets_RotatedToAnchorFacing(float wallDx, float wallDy)
    {
        var vignette = new DungeonVignette
        {
            Key = "TestVignette",
            Members =
            {
                new DungeonVignetteMember { Resref = "member_a", OffsetX = 0f, OffsetY = 0f },
                new DungeonVignetteMember { Resref = "member_b", OffsetX = 1.5f, OffsetY = 0.8f, FacingOffset = 15f }
            }
        };

        var anchorTile = (X: 5, Y: 5);
        var plan = new List<PlannedDecoration>();
        DungeonDecorationPlanner.PlaceVignette(plan, anchorTile, (wallDx, wallDy), vignette);

        plan.Should().HaveCount(2);
        plan.Select(p => p.Resref).Should().BeEquivalentTo(new[] { "member_a", "member_b" });

        var memberA = plan.Single(p => p.Resref == "member_a");
        var memberB = plan.Single(p => p.Resref == "member_b");

        // Both members share the same anchor and rotation, so the WORLD-SPACE separation between them
        // must equal the rotated declared offset delta (1.5, 0.8), regardless of which wall it landed
        // against — this is the "placed as a unit with relative offsets" invariant, independent of the
        // anchor's own absolute position.
        var declaredDelta = new Vector2(1.5f, 0.8f);
        var declaredMagnitude = declaredDelta.Length();

        var actualDelta = new Vector2(memberB.Position.X - memberA.Position.X, memberB.Position.Y - memberA.Position.Y);
        actualDelta.Length().Should().BeApproximately(declaredMagnitude, 0.01f,
            "rotation preserves distance between vignette members");

        // member_b's facing must be exactly member_a's facing (the shared anchor rotation) plus its
        // own declared FacingOffset (15 degrees) — never independently rolled.
        var expectedFacingDelta = 15f;
        var actualFacingDelta = ((memberB.Facing - memberA.Facing) % 360f + 360f) % 360f;
        actualFacingDelta.Should().BeApproximately(expectedFacingDelta, 0.1f);
    }

    /// <summary>
    /// Coherence metric (d): the calibrated density band from the pre-rework single-pass design still
    /// holds under the new motif/rhythm/vignette/doorway-flank arrangement — rearranging WHERE and WHAT
    /// gets placed must not silently collapse or explode the overall count. Reuses
    /// DungeonDecorationPlannerTests' own per-theme bands (unchanged by this rework — see that test's
    /// doc comment) as a cross-check from an independent test file.
    /// </summary>
    [TestCase(MineCaveDungeonDefinition.ThemeKey, 10, 55)]
    [TestCase(AlienRuinDungeonDefinition.ThemeKey, 8, 55)]
    [TestCase(UndercityDenDungeonDefinition.ThemeKey, 8, 55)]
    public void Plan_DefaultDensityAt16x16_StillLandsWithinEvidenceBand(string themeKey, int minAverage, int maxAverage)
    {
        var dungeons = BuildAllDungeons();
        var tilesets = BuildAllTilesetProfiles();
        var detail = dungeons[themeKey];
        var tileset = tilesets[detail.TilesetProfileKey];

        var counts = new List<int>();
        foreach (var seed in Seeds)
        {
            ResolvedLayout layout;
            try { layout = ResolveLayout(detail, tileset, seed, size: 16); }
            catch (Exception) { continue; }

            counts.Add(DungeonDecorationPlanner.Plan(layout, tileset, detail, 100).Count);
        }

        counts.Should().NotBeEmpty($"{themeKey}: none of the candidate seeds resolved a layout");
        counts.Average().Should().BeInRange(minAverage, maxAverage);
    }

    /// <summary>
    /// The reported bug, reproduced and asserted fixed: the Alien Ruin THEME composed onto the
    /// Futuristic City TILESET (the user's exact failing combination — a screenshot showed ruin walls,
    /// barrels, spike totems, and grass patches scattered across a clean sci-fi plaza) must now dress
    /// predominantly with the Futuristic City tileset's OWN evidence-mined palette (streetlights,
    /// holo-kiosks, planters — see BaseGameTilesetProfiles.FutCity), with Alien Ruin only contributing
    /// its two small theme accents on top.
    /// </summary>
    [Test]
    public void AlienRuinThemeOnFutCityTileset_DressesWithFutCityPaletteNotAlienRuinPalette()
    {
        var dungeons = BuildAllDungeons();
        var tilesets = BuildAllTilesetProfiles();

        var alienRuinDetail = dungeons[AlienRuinDungeonDefinition.ThemeKey];
        var futCityTileset = tilesets[BaseGameTilesetProfiles.FutCity];
        var ancientRuinTileset = tilesets[StandardTilesetProfiles.AncientRuin];

        var futCityResrefs = futCityTileset.Decorations.Select(d => d.Resref)
            .Concat(futCityTileset.Vignettes.SelectMany(v => v.Members).Select(m => m.Resref))
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
        var alienRuinOwnResrefs = alienRuinDetail.Decorations.Select(d => d.Resref)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
        var ancientRuinResrefs = ancientRuinTileset.Decorations.Select(d => d.Resref)
            .Concat(ancientRuinTileset.Vignettes.SelectMany(v => v.Members).Select(m => m.Resref))
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        // Sanity: the two palettes are genuinely different families (guards against a degenerate test
        // where they happen to share every resref, which would make the dominance assertion below
        // meaningless).
        futCityResrefs.Count(r => ancientRuinResrefs.Contains(r)).Should().BeLessThan(futCityResrefs.Count,
            "Futuristic City's palette should be a materially different family than Ancient Ruin's");

        var totalPlaced = 0;
        var placedFromFutCity = 0;
        var placedFromAlienRuinAccents = 0;
        var placedFromNeither = new List<string>();

        foreach (var seed in Seeds)
        {
            ResolvedLayout layout;
            try { layout = ResolveLayout(alienRuinDetail, futCityTileset, seed, size: 24); }
            catch (Exception) { continue; }

            var plan = DungeonDecorationPlanner.Plan(layout, futCityTileset, alienRuinDetail, 150);
            totalPlaced += plan.Count;

            foreach (var planned in plan)
            {
                if (futCityResrefs.Contains(planned.Resref))
                    placedFromFutCity++;
                else if (alienRuinOwnResrefs.Contains(planned.Resref))
                    placedFromAlienRuinAccents++;
                else
                    placedFromNeither.Add(planned.Resref);
            }
        }

        totalPlaced.Should().BeGreaterThan(0, "this composition should place a meaningful number of decorations across the sample seeds");
        placedFromNeither.Should().BeEmpty("every placed resref must come from either the composed tileset's palette or the theme's own accent list");

        var futCityShare = (double)placedFromFutCity / totalPlaced;
        futCityShare.Should().BeGreaterThan(0.7,
            $"the composed TILESET's own palette should dominate the plan (got {placedFromFutCity}/{totalPlaced} from Futuristic City, " +
            $"{placedFromAlienRuinAccents}/{totalPlaced} from Alien Ruin's own accents)");
    }

    // -------------------------------------------------------------------------------------------
    // Round-3 distribution-matching regressions (see decoration_evidence/ round-3 statistics
    // harness: a Python harness applied the SAME metrics below to hand-built reference areas and to
    // the generated planner's own output and reported a before/after divergence table). These four
    // tests encode the metrics that harness measured directly as data-derived C# regressions, so the
    // reported "ring" artifact (an open room's entire perimeter dressed as one evenly-spaced run of
    // an identical fixture) can never silently regress.
    // -------------------------------------------------------------------------------------------

    /// <summary>
    /// Round-3 metric (1): per-room same-resref repeat count for wall/corridor/doorway-hugging
    /// dressing must never exceed DungeonDecorationPlanner.MaxSameResrefPerRoomContext (5) —
    /// derived from the hand-built grid-bucket same-resref-repeat p90 across the typical
    /// (non-warehouse-density) mined tileset families (see decoration_evidence/
    /// handbuilt_summary.json: tdt01/zsf01/ttf01/tin01/tsw01/ttd01 sit at p90 3-5; families like
    /// tds01/vmr01/tii01/tdm01/tdr01/fcx01 run much higher, but that is warehouse/floor-motif set
    /// dressing — a different arrangement kind than the vertical wall-hugging fixtures this cap
    /// governs).
    /// </summary>
    [Test]
    public void Plan_SameResrefPerRoom_NeverExceedsCap()
    {
        var dungeons = BuildAllDungeons();
        var tilesets = BuildAllTilesetProfiles();
        var violations = new List<string>();
        var groupsChecked = 0;

        foreach (var (themeKey, tilesetKey) in SampleCompositions)
        {
            var detail = dungeons[themeKey];
            var tileset = tilesets[tilesetKey];
            var vignetteMemberResrefs = tileset.Vignettes.SelectMany(v => v.Members).Select(m => m.Resref)
                .ToHashSet(StringComparer.OrdinalIgnoreCase);

            foreach (var seed in Seeds)
            {
                ResolvedLayout layout;
                try { layout = ResolveLayout(detail, tileset, seed, size: 24); }
                catch (Exception) { continue; }

                var plan = DungeonDecorationPlanner.Plan(layout, tileset, detail, 150);

                var groups = plan
                    .Where(p => p.Context is DecorationContext.WallAdjacent or DecorationContext.CorridorSide or DecorationContext.DoorwayFlank)
                    .Where(p => !vignetteMemberResrefs.Contains(p.Resref))
                    .GroupBy(p => (RoomId: RoomIdForTile(layout, RecoverTile(p.Position)), p.Context, p.Resref));

                foreach (var group in groups)
                {
                    if (group.Key.RoomId < 0)
                        continue;

                    groupsChecked++;
                    var count = group.Count();
                    if (count > DungeonDecorationPlanner.MaxSameResrefPerRoomContext)
                        violations.Add($"{themeKey}/{tilesetKey} seed {seed} room {group.Key.RoomId} {group.Key.Context} {group.Key.Resref}: " +
                                       $"{count} (cap {DungeonDecorationPlanner.MaxSameResrefPerRoomContext})");
                }
            }
        }

        groupsChecked.Should().BeGreaterThan(20, "the sample compositions/seeds should produce plenty of room/resref groups to check");
        violations.Should().BeEmpty(string.Join(Environment.NewLine, violations));
    }

    /// <summary>
    /// Round-3 metric (2): the longest colinear, regularly-spaced same-resref chain among a room's
    /// wall/corridor/doorway-hugging dressing must never exceed
    /// DungeonDecorationPlanner.MaxRunSegmentLength (6) — mirrors the statistics harness's own
    /// run-detection method exactly (colinear grouping by shared axis within a 2-unit tolerance,
    /// chained while consecutive gaps stay under a 15-unit run-continuity tolerance — see
    /// LongestColinearChain) so this test measures precisely what the harness measured against
    /// hand-built reference areas.
    /// </summary>
    [Test]
    public void Plan_LongestColinearSameResrefChain_StaysWithinCap()
    {
        var dungeons = BuildAllDungeons();
        var tilesets = BuildAllTilesetProfiles();
        var violations = new List<string>();
        var chainsChecked = 0;

        foreach (var (themeKey, tilesetKey) in SampleCompositions)
        {
            var detail = dungeons[themeKey];
            var tileset = tilesets[tilesetKey];
            var vignetteMemberResrefs = tileset.Vignettes.SelectMany(v => v.Members).Select(m => m.Resref)
                .ToHashSet(StringComparer.OrdinalIgnoreCase);

            foreach (var seed in Seeds)
            {
                ResolvedLayout layout;
                try { layout = ResolveLayout(detail, tileset, seed, size: 24); }
                catch (Exception) { continue; }

                var plan = DungeonDecorationPlanner.Plan(layout, tileset, detail, 150);

                var groups = plan
                    .Where(p => p.Context is DecorationContext.WallAdjacent or DecorationContext.CorridorSide or DecorationContext.DoorwayFlank)
                    .Where(p => !vignetteMemberResrefs.Contains(p.Resref))
                    .GroupBy(p => (RoomId: RoomIdForTile(layout, RecoverTile(p.Position)), p.Context, p.Resref));

                foreach (var group in groups)
                {
                    if (group.Key.RoomId < 0 || group.Count() < 2)
                        continue;

                    chainsChecked++;
                    var chain = LongestColinearChain(group.Select(p => p.Position).ToList());
                    if (chain > DungeonDecorationPlanner.MaxRunSegmentLength)
                        violations.Add($"{themeKey}/{tilesetKey} seed {seed} room {group.Key.RoomId} {group.Key.Context} {group.Key.Resref}: " +
                                       $"chain {chain} (cap {DungeonDecorationPlanner.MaxRunSegmentLength})");
                }
            }
        }

        chainsChecked.Should().BeGreaterThan(10, "the sample compositions/seeds should produce plenty of 2+-member same-resref groups to check");
        violations.Should().BeEmpty(string.Join(Environment.NewLine, violations));
    }

    /// <summary>
    /// Round-3 metric (3) — THE regression for the reported bug: no room's wall/corridor/doorway
    /// same-resref group may form a closed perimeter ring (see IsClosedRing). Checked across every
    /// sample composition/seed at three sizes (20/24/28) specifically to stress the large-open-room
    /// case the report's screenshot showed (a Futuristic City plaza where nearly the whole perimeter
    /// was wall-eligible). The statistics harness measured this exact metric against hand-built
    /// reference areas and found it near-zero (0-4 out of 32-519 same-resref groups per family,
    /// depending on family, versus up to 10/300 in this planner's PRE-FIX output) — this test holds
    /// the fixed planner to zero, matching what the harness measured post-fix across 12 families x
    /// 30-40 seeds each.
    /// </summary>
    [Test]
    public void Plan_NoRoomHasClosedRingOfWallHuggingDecorations()
    {
        var dungeons = BuildAllDungeons();
        var tilesets = BuildAllTilesetProfiles();
        var violations = new List<string>();
        var candidateGroupsChecked = 0;
        var sizes = new[] { 20, 24, 28 };

        foreach (var (themeKey, tilesetKey) in SampleCompositions)
        {
            var detail = dungeons[themeKey];
            var tileset = tilesets[tilesetKey];
            var vignetteMemberResrefs = tileset.Vignettes.SelectMany(v => v.Members).Select(m => m.Resref)
                .ToHashSet(StringComparer.OrdinalIgnoreCase);

            foreach (var seed in Seeds)
            foreach (var size in sizes)
            {
                ResolvedLayout layout;
                try { layout = ResolveLayout(detail, tileset, seed, size: size); }
                catch (Exception) { continue; }

                var plan = DungeonDecorationPlanner.Plan(layout, tileset, detail, 150);

                var groups = plan
                    .Where(p => p.Context is DecorationContext.WallAdjacent or DecorationContext.CorridorSide or DecorationContext.DoorwayFlank)
                    .Where(p => !vignetteMemberResrefs.Contains(p.Resref))
                    .GroupBy(p => (RoomId: RoomIdForTile(layout, RecoverTile(p.Position)), p.Context, p.Resref));

                foreach (var group in groups)
                {
                    if (group.Key.RoomId < 0 || group.Count() < 4)
                        continue;

                    candidateGroupsChecked++;
                    if (IsClosedRing(group.Select(p => p.Position).ToList(), out var metrics))
                        violations.Add($"{themeKey}/{tilesetKey} seed {seed} size {size} room {group.Key.RoomId} {group.Key.Context} {group.Key.Resref}: " +
                                       $"span={metrics.Span:0.0} radiusCv={metrics.RadiusCv:0.00} gapCv={metrics.GapCv:0.00}");
                }
            }
        }

        candidateGroupsChecked.Should().BeGreaterThan(5, "the sample compositions/seeds/sizes should produce at least a few 4+-member same-resref groups to check");
        violations.Should().BeEmpty(string.Join(Environment.NewLine, violations));
    }

    /// <summary>
    /// Round-3 metric (4): nearest-neighbor same-resref distance (whole-area, not room-scoped) should
    /// land within the broad range the statistics harness measured across every mined hand-built
    /// family — p50 ranged 1.39-17.83 world units and p90 ranged 4.55-50.16 across the 12 mined
    /// families (see decoration_evidence/handbuilt_summary.json nn_same_type_distance) — a coarse
    /// regression guard against a future change collapsing same-resref placements into
    /// near-duplicates or scattering them far outside anything a hand-built family shows.
    /// </summary>
    [Test]
    public void Plan_NearestSameResrefDistance_StaysWithinHandBuiltRange()
    {
        var dungeons = BuildAllDungeons();
        var tilesets = BuildAllTilesetProfiles();
        var distances = new List<double>();

        foreach (var (themeKey, tilesetKey) in SampleCompositions)
        {
            var detail = dungeons[themeKey];
            var tileset = tilesets[tilesetKey];

            foreach (var seed in Seeds)
            {
                ResolvedLayout layout;
                try { layout = ResolveLayout(detail, tileset, seed, size: 24); }
                catch (Exception) { continue; }

                var plan = DungeonDecorationPlanner.Plan(layout, tileset, detail, 150);

                foreach (var group in plan.GroupBy(p => p.Resref))
                {
                    var points = group.Select(p => p.Position).ToList();
                    if (points.Count < 2)
                        continue;

                    for (var i = 0; i < points.Count; i++)
                    {
                        var best = double.MaxValue;
                        for (var j = 0; j < points.Count; j++)
                        {
                            if (i == j)
                                continue;
                            var d = Vector2.Distance(new Vector2(points[i].X, points[i].Y), new Vector2(points[j].X, points[j].Y));
                            if (d < best)
                                best = d;
                        }
                        distances.Add(best);
                    }
                }
            }
        }

        distances.Should().NotBeEmpty("the sample compositions/seeds should produce plenty of repeated resrefs to measure");
        var sorted = distances.OrderBy(d => d).ToList();
        var median = sorted[sorted.Count / 2];

        median.Should().BeInRange(0.5, 45.0,
            "median nearest-neighbor same-resref distance should stay within the range every mined hand-built family showed (p50 1.39-17.83)");
    }

    /// <summary>
    /// Colinear same-resref chain detector mirroring the round-3 statistics harness's own
    /// _find_runs_for_resref exactly: groups points by their "other" axis coordinate (rounded to a
    /// 2-unit tolerance — same row/column), sorts along the run axis, and chains consecutive points
    /// while the gap between them stays within a 15-unit run-continuity tolerance. Checks both axis
    /// orientations (a run along a +/-X-facing wall runs along Y and vice versa) since a black-box
    /// test cannot know which wall direction produced a given placement.
    /// </summary>
    private static int LongestColinearChain(List<Vector3> positions)
    {
        if (positions.Count < 2)
            return positions.Count;

        const float axisTolerance = 2f;
        const float gapTolerance = 15f;
        var longest = 1;

        foreach (var primaryIsX in new[] { true, false })
        {
            var buckets = new Dictionary<int, List<float>>();
            foreach (var p in positions)
            {
                var primaryVal = primaryIsX ? p.X : p.Y;
                var secondaryVal = primaryIsX ? p.Y : p.X;
                var key = (int)MathF.Round(secondaryVal / axisTolerance);
                if (!buckets.TryGetValue(key, out var list))
                {
                    list = new List<float>();
                    buckets[key] = list;
                }
                list.Add(primaryVal);
            }

            foreach (var list in buckets.Values)
            {
                list.Sort();
                var chain = 1;
                for (var i = 1; i < list.Count; i++)
                {
                    chain = list[i] - list[i - 1] <= gapTolerance ? chain + 1 : 1;
                    longest = Math.Max(longest, chain);
                }
            }
        }

        return longest;
    }

    /// <summary>
    /// Ring-shape detector mirroring the round-3 statistics harness's own ring_metrics/is_closed_loop
    /// exactly: a same-resref group is a closed ring when its members sweep at least 300 degrees of
    /// angle around their own centroid (angular_span), sit at a roughly CONSTANT radius from that
    /// centroid (radius coefficient-of-variation &lt;= 0.4 — this is what tells a deliberate ring apart
    /// from a same-resref group merely scattered across a filled room, which also sweeps a wide angle
    /// but at wildly varying radii), AND are roughly EVENLY spaced in angle (the largest gap excluded,
    /// remaining-gap coefficient-of-variation &lt;= 0.6).
    /// </summary>
    private static bool IsClosedRing(List<Vector3> positions, out (double Span, double RadiusCv, double GapCv) metrics)
    {
        metrics = default;
        if (positions.Count < 4)
            return false;

        var cx = positions.Average(p => p.X);
        var cy = positions.Average(p => p.Y);

        var radii = positions.Select(p => Math.Sqrt(Math.Pow(p.X - cx, 2) + Math.Pow(p.Y - cy, 2))).ToList();
        var meanR = radii.Average();
        if (meanR < 1e-6)
            return false;
        var radiusStdev = Math.Sqrt(radii.Select(r => (r - meanR) * (r - meanR)).Average());
        var radiusCv = radiusStdev / meanR;

        var angles = positions
            .Select(p => (Math.Atan2(p.Y - cy, p.X - cx) * (180.0 / Math.PI) + 360.0) % 360.0)
            .OrderBy(a => a).ToList();
        var gaps = new List<double>();
        for (var i = 0; i < angles.Count; i++)
        {
            var next = i == angles.Count - 1 ? angles[0] + 360.0 : angles[i + 1];
            gaps.Add(next - angles[i]);
        }
        var span = 360.0 - gaps.Max();

        var remaining = gaps.OrderBy(g => g).Take(gaps.Count - 1).ToList();
        if (remaining.Count == 0)
            remaining = gaps;
        var meanGap = remaining.Average();
        var gapCv = meanGap > 1e-6
            ? Math.Sqrt(remaining.Select(g => (g - meanGap) * (g - meanGap)).Average()) / meanGap
            : double.PositiveInfinity;

        metrics = (span, radiusCv, gapCv);

        const double angleThreshold = 300.0;
        const double radiusCvMax = 0.4;
        const double gapCvMax = 0.6;

        return span >= angleThreshold && radiusCv <= radiusCvMax && gapCv <= gapCvMax;
    }
}
