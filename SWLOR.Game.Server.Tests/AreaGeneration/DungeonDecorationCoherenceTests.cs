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
}
