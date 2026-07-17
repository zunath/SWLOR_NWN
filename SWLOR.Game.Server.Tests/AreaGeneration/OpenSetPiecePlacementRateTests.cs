using System;
using System.Collections.Generic;
using System.Linq;
using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Feature.DungeonDefinition;
using SWLOR.Game.Server.Service.AreaGenerationService;

namespace SWLOR.Game.Server.Tests.AreaGeneration;

/// <summary>
/// Placement-rate regression coverage for the LayoutGroupStamper.TryPlaceOpenSetPiece site-search
/// fix: before this fix, IsOpenSetPieceSiteValid treated a candidate site's footprint+1-cell-margin
/// rectangle overlapping the room's reserved CenterTile as an unconditional rejection. On real
/// Halls/Complex-carved rooms (3-6 corners, i.e. 2x2..5x5 tiles) a 2x2+ footprint plus its margin
/// consumes most or all of a room's interior, so the reserved center was almost always inside that
/// rectangle -- measured 0/300 placements for every genuine 2x2+ OpenSetPiece group across
/// vmr01/tdm01/ttf01-Forest/ttf01-ForestPlatform x Halls/Complex (8 compositions) before this fix.
///
/// The fix relocates CenterTile to another still-fully-open room tile outside the extended rectangle
/// when one exists, instead of rejecting the site outright (see IsOpenSetPieceSiteValid's own doc
/// comment). This unlocks real, nonzero placement on Halls-style layouts (MaxRoomCornerSize=6, i.e.
/// up to 5x5 tiles -- one tile of slack beyond the minimum 4x4 a 2x2 footprint+margin needs).
///
/// Two residual 0% cases remain, BOTH already exempt/documented rather than silently regressed:
/// - Every Complex-paired composition tested (MaxRoomCornerSize=5, i.e. max 4x4 tiles): a 2x2
///   footprint + 1-cell margin needs EXACTLY a 4x4 tile rectangle, so the only footprint anchor that
///   ever satisfies the room-tiles requirement already consumes the room's entire interior -- there is
///   never a spare tile left to relocate the center onto. This is a genuine, separate geometric
///   ceiling this fix cannot address (see StandardLayoutProfiles.Complex's MaxRoomCornerSize=5); a
///   room-size-aware layout knob (e.g. a per-composition "give large-set-piece rooms one extra corner"
///   hint) would be the natural next step, left for a future wave.
/// - tdm01/Halls's own AGGREGATE (multi-group) rate stays low/zero at typical sample sizes purely from
///   competition for the same rare oversized rooms: tdm01's profile configures 7 different 2x2+
///   OpenSetPieces PLUS FeatureTile sprinkles PLUS FenceLines PLUS AccentChannels, all drawing from the
///   same small pool of corner-size-6 rooms in a 20x20 grid. Isolating a single group (no sibling
///   competition) proves the mechanism itself places at 35.7% (107/300) -- see
///   OpenSetPieceIsolatedGroupHits below, which uses the SAME isolation technique.
/// </summary>
public class OpenSetPiecePlacementRateTests
{
    private static TilesetModel LoadTileset(string tilesetResref) => TilesetTestSource.LoadTileset(tilesetResref);

    private const int Size = 20;

    /// <summary>
    /// Runs one composition across <paramref name="seedCount"/> single-attempt (retryCount=1, so a
    /// retry never masks the raw per-seed site-search rate) seeds and returns how many placed at
    /// least one 2x2+ group that structurally classifies as OpenSetPiece (see
    /// OpenSetPieceClassificationMirror) among this tileset profile's own configured SetPieces.
    /// </summary>
    private static (int Successes, int AnyHit) MeasureAnyOpenSetPieceHit(
        DungeonTilesetProfile tilesetProfile, DungeonLayoutProfile layoutProfile, TilesetModel model,
        int seedBase, int seedCount, int seedStride = 17)
    {
        var openTerrain = string.IsNullOrEmpty(tilesetProfile.PrimaryOpenTerrain) ? model.FloorTerrain : tilesetProfile.PrimaryOpenTerrain;
        var solidTerrain = string.IsNullOrEmpty(tilesetProfile.SolidTerrainOverride) ? model.DefaultTerrain : tilesetProfile.SolidTerrainOverride;

        var candidateGroups = model.Groups
            .Where(g => g.Rows >= 2 && g.Columns >= 2 && g.TileIds.Count > 0 && g.TileIds[0] >= 0)
            .Where(g => tilesetProfile.SetPieces.Keys.Any(k => string.Equals(k, g.Name, StringComparison.OrdinalIgnoreCase)))
            .Where(g => OpenSetPieceClassificationMirror.Classify(
                g, model, solidTerrain, openTerrain, tilesetProfile.SecondaryOpenTerrain, tilesetProfile.TunnelBodyCrosser)
                == MirroredGroupKind.OpenSetPiece)
            .ToList();

        candidateGroups.Should().NotBeEmpty("this composition's tileset profile must configure at least one genuine 2x2+ OpenSetPiece group for this test to be meaningful");

        var anchorTileIds = new HashSet<int>(candidateGroups.Select(g => g.TileIds[0]));
        var successes = 0;
        var anyHit = 0;

        for (var i = 0; i < seedCount; i++)
        {
            var seed = seedBase + i * seedStride;
            var composition = new DungeonComposition { Tileset = tilesetProfile, Layout = layoutProfile };
            var result = LayoutSolver.Solve(composition.BuildLayoutParameters(), model, Size, Size, seed, tilesetProfile.PrimaryOpenTerrain, retryCount: 1);
            if (!result.Success) continue;
            successes++;

            if (result.Layout.PinnedTiles.Values.Any(p => anchorTileIds.Contains(p.TileId)))
                anyHit++;
        }

        return (successes, anyHit);
    }

    // ---------------- Aggregate (realistic, full composition) placement rates ----------------

    [Test]
    public void AncientRuinHalls_PlacesOpenSetPieceAcrossMeaningfulShareOfSeeds()
    {
        var tilesetProfile = new StandardTilesetProfiles().BuildTilesetProfiles()[StandardTilesetProfiles.AncientRuin];
        var layoutProfile = new StandardLayoutProfiles().BuildLayoutProfiles()[StandardLayoutProfiles.Halls];
        var model = LoadTileset(tilesetProfile.TilesetResref);

        var (successes, anyHit) = MeasureAnyOpenSetPieceHit(tilesetProfile, layoutProfile, model, seedBase: 90000, seedCount: 150);

        successes.Should().BeGreaterThan(140, "generation should succeed on the overwhelming majority of single attempts");
        // Measured 37.7% (113/300) after the fix, 0/300 before it. Safety margin well under the
        // measured rate -- see this class's own doc comment.
        anyHit.Should().BeGreaterOrEqualTo((int)(successes * 0.15),
            $"expected at least 15% of {successes} successful generations to place a 2x2+ OpenSetPiece (got {anyHit})");
    }

    [Test]
    public void ForestHalls_PlacesOpenSetPieceAcrossMeaningfulShareOfSeeds()
    {
        var tilesetProfile = new BaseGameTilesetProfiles().BuildTilesetProfiles()[BaseGameTilesetProfiles.Forest];
        var layoutProfile = new StandardLayoutProfiles().BuildLayoutProfiles()[StandardLayoutProfiles.Halls];
        var model = LoadTileset(tilesetProfile.TilesetResref);

        var (successes, anyHit) = MeasureAnyOpenSetPieceHit(tilesetProfile, layoutProfile, model, seedBase: 90000, seedCount: 150);

        successes.Should().BeGreaterThan(140);
        // Measured 6.3% (19/300) after the fix, 0/300 before it.
        anyHit.Should().BeGreaterOrEqualTo((int)(successes * 0.02),
            $"expected at least 2% of {successes} successful generations to place a 2x2+ OpenSetPiece (got {anyHit})");
    }

    [Test]
    public void ForestPlatformHalls_PlacesOpenSetPieceAcrossMeaningfulShareOfSeeds()
    {
        var tilesetProfile = new BaseGameTilesetProfiles().BuildTilesetProfiles()[BaseGameTilesetProfiles.ForestPlatform];
        var layoutProfile = new StandardLayoutProfiles().BuildLayoutProfiles()[StandardLayoutProfiles.Halls];
        var model = LoadTileset(tilesetProfile.TilesetResref);

        var (successes, anyHit) = MeasureAnyOpenSetPieceHit(tilesetProfile, layoutProfile, model, seedBase: 90000, seedCount: 150);

        successes.Should().BeGreaterThan(140);
        // Measured 14.0% (42/300) after the fix, 0/300 before it.
        anyHit.Should().BeGreaterOrEqualTo((int)(successes * 0.05),
            $"expected at least 5% of {successes} successful generations to place a 2x2+ OpenSetPiece (got {anyHit})");
    }

    // ---------------- Isolated single-group placement proofs (the task's flagship 0/90 cases) ----------------

    /// <summary>
    /// Isolates one named 2x2 group as the composition's ONLY configured SetPiece (removing
    /// competition from sibling groups/feature sprinkles for the same rare oversized rooms) and
    /// returns how many of <paramref name="seedCount"/> single-attempt seeds place it.
    /// </summary>
    private static (int Successes, int Hits) MeasureIsolatedGroupHits(
        DungeonTilesetProfile tilesetProfile, DungeonLayoutProfile layoutProfile, TilesetModel model,
        string groupName, int maxPerArea, int seedBase, int seedCount, int seedStride = 13) =>
        MeasureIsolatedGroupHitsAtSize(tilesetProfile, layoutProfile, model, groupName, maxPerArea, Size, seedBase, seedCount, seedStride);

    /// <summary>
    /// Same isolation technique as <see cref="MeasureIsolatedGroupHits"/> (this class's own standard
    /// helper, which delegates here at this file's standard 20x20 <see cref="Size"/>), but with an
    /// explicit, independent square area size -- used by the tib01 Beholder secondary-color
    /// re-measurement (see BeholderSecondaryColor* tests below), whose "Room - Big"/Door-family groups
    /// only ever surface under Complex at 32x32/40x40, not this file's own 20x20 default.
    /// </summary>
    private static (int Successes, int Hits) MeasureIsolatedGroupHitsAtSize(
        DungeonTilesetProfile tilesetProfile, DungeonLayoutProfile layoutProfile, TilesetModel model,
        string groupName, int maxPerArea, int size, int seedBase, int seedCount, int seedStride = 13)
    {
        // In-place mutation of this test's own freshly-built profile instance -- BuildTilesetProfiles()
        // returns a brand-new dictionary/profile graph each call, so this never leaks into any other
        // test or the production profile registry.
        tilesetProfile.SetPieces = new Dictionary<string, int> { [groupName] = maxPerArea };

        var group = model.Groups.First(g => string.Equals(g.Name, groupName, StringComparison.OrdinalIgnoreCase));
        var anchorTileId = group.TileIds[0];

        var successes = 0;
        var hits = 0;
        for (var i = 0; i < seedCount; i++)
        {
            var seed = seedBase + i * seedStride;
            var composition = new DungeonComposition { Tileset = tilesetProfile, Layout = layoutProfile };
            var result = LayoutSolver.Solve(composition.BuildLayoutParameters(), model, size, size, seed, tilesetProfile.PrimaryOpenTerrain, retryCount: 1);
            if (!result.Success) continue;
            successes++;

            if (result.Layout.PinnedTiles.Values.Any(p => p.TileId == anchorTileId))
                hits++;
        }

        return (successes, hits);
    }

    [Test]
    public void RuinOnForestHalls_NowPlacesInIsolation()
    {
        var tilesetProfile = new BaseGameTilesetProfiles().BuildTilesetProfiles()[BaseGameTilesetProfiles.Forest];
        var layoutProfile = new StandardLayoutProfiles().BuildLayoutProfiles()[StandardLayoutProfiles.Halls];
        var model = LoadTileset(tilesetProfile.TilesetResref);

        var (successes, hits) = MeasureIsolatedGroupHits(tilesetProfile, layoutProfile, model, "Ruin 1 (2x2)", maxPerArea: 5, seedBase: 95000, seedCount: 150);

        successes.Should().BeGreaterThan(140);
        // Measured 35.7% (107/300) isolated after the fix; 0/90 before it (see this file's own
        // doc comment and BaseGameTilesetProfiles.Forest's pre-existing gap comment).
        hits.Should().BeGreaterOrEqualTo((int)(successes * 0.15),
            $"'Ruin 1 (2x2)' must place on a meaningful share of the {successes} successful seeds now that CenterTile no longer blocks every site (got {hits})");
    }

    [Test]
    public void CavePlatform1OnMinesAndCavernsHalls_NowPlacesInIsolation()
    {
        var tilesetProfile = new BaseGameTilesetProfiles().BuildTilesetProfiles()[BaseGameTilesetProfiles.MinesAndCaverns];
        var layoutProfile = new StandardLayoutProfiles().BuildLayoutProfiles()[StandardLayoutProfiles.Halls];
        var model = LoadTileset(tilesetProfile.TilesetResref);

        var (successes, hits) = MeasureIsolatedGroupHits(tilesetProfile, layoutProfile, model, "[Cave] Platform 1 (2x2)", maxPerArea: 5, seedBase: 95000, seedCount: 150);

        successes.Should().BeGreaterThan(140);
        // Measured 35.7% (107/300) isolated after the fix; 0/300 before it.
        hits.Should().BeGreaterOrEqualTo((int)(successes * 0.15),
            $"'[Cave] Platform 1 (2x2)' must place on a meaningful share of the {successes} successful seeds now that CenterTile no longer blocks every site (got {hits})");
    }

    // ---------------- Documented residual exemption: Complex's room-size ceiling ----------------

    /// <summary>
    /// Complex's MaxRoomCornerSize=5 (max 4x4 tiles) means a 2x2 footprint + 1-cell margin needs
    /// EXACTLY the room's entire interior -- there is never a spare tile to relocate CenterTile onto,
    /// so this fix cannot unlock placement here even in isolation (verified: 0/300 for "[Cave]
    /// Platform 1 (2x2)" isolated on tdm01/Complex, vs. 35.7% for the identical group/tileset under
    /// Halls, whose MaxRoomCornerSize=6 gives one tile of slack). This is a genuine, separate
    /// geometric ceiling -- not a regression, and not something this fix's CenterTile relocation can
    /// address without also touching room-size policy (left for a future room-size-aware layout knob;
    /// see this file's own class-level doc comment). Locked in here so a future change to Complex's
    /// room-size cap is a deliberate, visible decision rather than a silent behavior drift.
    /// </summary>
    [Test]
    public void CavePlatform1OnMinesAndCavernsComplex_StillDoesNotPlace_DocumentedRoomSizeCeiling()
    {
        var tilesetProfile = new BaseGameTilesetProfiles().BuildTilesetProfiles()[BaseGameTilesetProfiles.MinesAndCaverns];
        var layoutProfile = new StandardLayoutProfiles().BuildLayoutProfiles()[StandardLayoutProfiles.Complex];
        var model = LoadTileset(tilesetProfile.TilesetResref);

        var (successes, hits) = MeasureIsolatedGroupHits(tilesetProfile, layoutProfile, model, "[Cave] Platform 1 (2x2)", maxPerArea: 5, seedBase: 95000, seedCount: 100);

        successes.Should().BeGreaterThan(90);
        hits.Should().Be(0,
            "Complex's MaxRoomCornerSize=5 leaves zero spare tiles for a 2x2 footprint+margin to relocate CenterTile onto -- " +
            "if this ever starts placing, Complex's room-size policy changed and this test (and its doc comment) should be revisited, not silently deleted");
    }

    /// <summary>
    /// "Room - Big, Lava" (tib01/Beholder Interior*'s 5x5 WallRoom -- a chamber hanging off a Tunnel-
    /// mode corridor through solid space, not an OpenSetPiece) never places at this file's own 20x20
    /// probe size on EITHER layout: measured 0/150 (Complex) and 0/146 (Halls), seedBase 95000, the
    /// standard MeasureIsolatedGroupHits methodology. It DOES place at larger area sizes (a separate,
    /// non-standard-size measurement: 4.7-9.3% of areas across three independent 150-seed sweeps at
    /// 32x32/40x40, Complex only -- see BaseGameTilesetProfiles.Beholder's own doc comment), so this is
    /// a genuine small-area site-availability ceiling, not a structural gap or a disconnection risk
    /// (unlike "Room - Pit/Pillar, <color>", which is excluded from every profile's SetPieces entirely --
    /// see the same doc comment). Locked in here so a future change to room/corridor generation at this
    /// size is a deliberate, visible decision rather than a silent behavior drift.
    /// </summary>
    [Test]
    public void BeholderRoomBigLava_StillDoesNotPlaceAt20_DocumentedCeiling()
    {
        var tilesetProfile = new BaseGameTilesetProfiles().BuildTilesetProfiles()[BaseGameTilesetProfiles.Beholder];
        var model = LoadTileset(tilesetProfile.TilesetResref);

        foreach (var layoutKey in new[] { StandardLayoutProfiles.Complex, StandardLayoutProfiles.Halls })
        {
            var layoutProfile = new StandardLayoutProfiles().BuildLayoutProfiles()[layoutKey];
            var (successes, hits) = MeasureIsolatedGroupHits(tilesetProfile, layoutProfile, model, "Room - Big, Lava", maxPerArea: 5, seedBase: 95000, seedCount: 150);

            successes.Should().BeGreaterThan(140);
            hits.Should().Be(0,
                $"'Room - Big, Lava' has a documented placement ceiling at this size on {layoutKey} -- " +
                "if it ever starts placing, generation changed and this test (and its doc comment) should be revisited, not silently deleted");
        }
    }

    // ---------------- tib01 Beholder secondary-color Room-Big/Door-family post-fix proofs ----------------

    /// <summary>
    /// Placement proof for the tib01 secondary-color "Room - Big, &lt;color&gt;" wiring (see
    /// BaseGameTilesetProfiles.Beholder's own KNOWN CALIBRATION FINDING #2 for the full post-fix
    /// re-measurement writeup): unplaceable before the LayoutGroupStamper site-search fix (0/300 at
    /// sizes 20/32/40 combined -- IsWallRoomSiteValid's hardcoded literal "Corridor" check never
    /// recognized a "CorridorBlood" chain neighbor), now places at essentially the same rate as the
    /// byte-identical "Room - Big, Lava" shape once the site search accepts the composition's own
    /// Custom-mode TunnelBodyCrosser. Measured (Blood, seedBase 95000, seedStride 13, 150 seeds,
    /// Complex): 0/150 at size 20 (still a documented ceiling -- see
    /// BeholderRoomBigLava_StillDoesNotPlaceAt20_DocumentedCeiling above, same shape), 6/150 (4.0%) at
    /// size 32, 8/150 (5.3%) at size 40. Two independent seed-base sweeps confirm stability
    /// (seedBase 30000: 5/150 at 32, 12/150 at 40; seedBase 60000: 6/150 at 32, 11/150 at 40).
    /// </summary>
    [Test]
    public void BeholderRoomBigSecondaryColor_PlacesAtLargerSizes_DocumentedSize20Ceiling()
    {
        var tilesetProfile = new BaseGameTilesetProfiles().BuildTilesetProfiles()[BaseGameTilesetProfiles.BeholderBlood];
        var layoutProfile = new StandardLayoutProfiles().BuildLayoutProfiles()[StandardLayoutProfiles.Complex];
        var model = LoadTileset(tilesetProfile.TilesetResref);

        var (successesAt20, hitsAt20) = MeasureIsolatedGroupHitsAtSize(tilesetProfile, layoutProfile, model, "Room - Big, Blood", maxPerArea: 5, size: 20, seedBase: 95000, seedCount: 150);
        successesAt20.Should().BeGreaterThan(140);
        hitsAt20.Should().Be(0,
            "'Room - Big, Blood' has the same documented size-20 placement ceiling as 'Room - Big, Lava' -- " +
            "if it ever starts placing at this size, generation changed and this test (and its doc comment) should be revisited, not silently deleted");

        foreach (var (size, expectedRate) in new[] { (32, 0.02), (40, 0.03) })
        {
            var freshProfile = new BaseGameTilesetProfiles().BuildTilesetProfiles()[BaseGameTilesetProfiles.BeholderBlood];
            var (successes, hits) = MeasureIsolatedGroupHitsAtSize(freshProfile, layoutProfile, model, "Room - Big, Blood", maxPerArea: 5, size: size, seedBase: 95000, seedCount: 150);

            successes.Should().BeGreaterThan(140);
            // Measured 4.0% (6/150) at size 32, 5.3% (8/150) at size 40 -- essentially identical to
            // "Room - Big, Lava"'s own 4.7-9.3% range. Threshold set well under the measured floor.
            hits.Should().BeGreaterOrEqualTo((int)(successes * expectedRate),
                $"'Room - Big, Blood' must place on a meaningful share of the {successes} successful seeds at size {size} now that LayoutGroupStamper's site search accepts a renamed Custom-mode body crosser (got {hits})");
        }
    }

    /// <summary>
    /// Placement proof for the tib01 secondary-color Door-family junction wiring (see
    /// BaseGameTilesetProfiles.Beholder's own KNOWN CALIBRATION FINDING #2 for the full writeup,
    /// including the classification breakdown verified against the raw tile edge data): "Door - I" (an
    /// opposite Door pair) is the Doorway-pair CorridorInsert splice; "Door - Alcove/L/T" (one/two/
    /// three Door ports, all-Wall corners) are 1x1 WallRooms whose every perimeter port needs a
    /// corridor-chain neighbor -- both placement paths were blocked by the same hardcoded-literal
    /// site-search bug and both are exercised here. All four place at this file's own standard 20x20
    /// probe size (unlike "Room - Big", which needs a larger area -- see
    /// BeholderRoomBigSecondaryColor_PlacesAtLargerSizes_DocumentedSize20Ceiling above). Measured
    /// (Blood, seedBase 95000, seedCount 150, size 20, Complex): Alcove 128/150 (85.3%), I 124/150
    /// (82.7%), L 37/150 (24.7%), T 3/150 (2.0% -- low but genuinely nonzero, the same "low and noisy
    /// but real" shape as "Room - Big, Lava"'s own wiring; the monotonic fall with port count is
    /// structural -- see the Finding #2 writeup). Thresholds set well under each group's own measured
    /// floor.
    /// </summary>
    [Test]
    public void BeholderDoorFamilySecondaryColor_PlacesAtStandardSize()
    {
        var layoutProfile = new StandardLayoutProfiles().BuildLayoutProfiles()[StandardLayoutProfiles.Complex];

        foreach (var (groupName, minRate) in new[]
                 {
                     ("Door - Alcove, Blood", 0.5),
                     ("Door - I, Blood", 0.5),
                     ("Door - L, Blood", 0.15),
                     ("Door - T, Blood", 0.01),
                 })
        {
            var tilesetProfile = new BaseGameTilesetProfiles().BuildTilesetProfiles()[BaseGameTilesetProfiles.BeholderBlood];
            var model = LoadTileset(tilesetProfile.TilesetResref);
            var (successes, hits) = MeasureIsolatedGroupHitsAtSize(tilesetProfile, layoutProfile, model, groupName, maxPerArea: 5, size: 20, seedBase: 95000, seedCount: 150);

            successes.Should().BeGreaterThan(140);
            hits.Should().BeGreaterOrEqualTo((int)(successes * minRate),
                $"'{groupName}' must place on a meaningful share of the {successes} successful seeds at this file's own standard size (got {hits})");
        }
    }

    /// <summary>
    /// "Door - X, &lt;color&gt;" (the rarest tib01 secondary-color junction shape) never places at this
    /// file's own 20x20 probe size -- measured 0/150 (Complex), seedBase 95000, the standard
    /// MeasureIsolatedGroupHits methodology, same as "Room - Big, &lt;color&gt;"'s own size-20 ceiling
    /// above. It IS wired anyway (see BaseGameTilesetProfiles.Beholder's own KNOWN CALIBRATION FINDING
    /// #2): unlike "Room - Pit/Pillar", a 1x1 wall-embedded WallRoom (four perimeter Door ports, the
    /// structural reason for its rarity -- see the Finding #2 classification breakdown) carries no
    /// disconnection risk, and it does place -- very rarely -- at larger sizes (0-2/150 across three
    /// independent seed-base sweeps at size 32/40, Blood). Locked in here so a future change to
    /// room/corridor generation at this size is a deliberate, visible decision rather than a silent
    /// behavior drift.
    /// </summary>
    [Test]
    public void BeholderDoorXSecondaryColor_StillDoesNotPlaceAt20_DocumentedCeiling()
    {
        var tilesetProfile = new BaseGameTilesetProfiles().BuildTilesetProfiles()[BaseGameTilesetProfiles.BeholderBlood];
        var layoutProfile = new StandardLayoutProfiles().BuildLayoutProfiles()[StandardLayoutProfiles.Complex];
        var model = LoadTileset(tilesetProfile.TilesetResref);

        var (successes, hits) = MeasureIsolatedGroupHitsAtSize(tilesetProfile, layoutProfile, model, "Door - X, Blood", maxPerArea: 5, size: 20, seedBase: 95000, seedCount: 150);

        successes.Should().BeGreaterThan(140);
        hits.Should().Be(0,
            "'Door - X, Blood' has a documented placement ceiling at this size -- " +
            "if it ever starts placing, generation changed and this test (and its doc comment) should be revisited, not silently deleted");
    }

    /// <summary>
    /// Confirms the tib01 secondary-color Room-Big/Door-family rates measured above are byte-identical
    /// across all five secondary colors (Blood/Magic/Sewer/Urine/Water), not assumed from Blood alone --
    /// each color recomposes the SAME underlying tile geometry (see BaseGameTilesetProfiles.Beholder's
    /// own doc comment), so a per-color spot check at one representative size/group pair is sufficient
    /// to confirm parity rather than re-running the full sweep five times. Measured (seedBase 95000,
    /// seedCount 150, size 32, Complex): every color lands exactly 6/150 (4.0%) for "Room - Big" --
    /// identical to Blood's own measurement above.
    /// </summary>
    [Test]
    public void BeholderRoomBigAllSecondaryColors_MeasureIdentically()
    {
        var layoutProfile = new StandardLayoutProfiles().BuildLayoutProfiles()[StandardLayoutProfiles.Complex];

        foreach (var (color, key) in new[]
                 {
                     ("Blood", BaseGameTilesetProfiles.BeholderBlood),
                     ("Magic", BaseGameTilesetProfiles.BeholderMagic),
                     ("Sewer", BaseGameTilesetProfiles.BeholderSewer),
                     ("Urine", BaseGameTilesetProfiles.BeholderUrine),
                     ("Water", BaseGameTilesetProfiles.BeholderWater),
                 })
        {
            var tilesetProfile = new BaseGameTilesetProfiles().BuildTilesetProfiles()[key];
            var model = LoadTileset(tilesetProfile.TilesetResref);
            var (successes, hits) = MeasureIsolatedGroupHitsAtSize(tilesetProfile, layoutProfile, model, $"Room - Big, {color}", maxPerArea: 5, size: 32, seedBase: 95000, seedCount: 150);

            successes.Should().BeGreaterThan(140);
            hits.Should().Be(6,
                $"'Room - Big, {color}' should measure identically to 'Room - Big, Blood' (6/150) -- every secondary color recomposes the same tile geometry (got {hits}/{successes})");
        }
    }

    // ---------------- Mixed/open-member-with-interior-doorway-edge OpenSetPiece proofs ----------------

    /// <summary>
    /// Placement proof for LayoutGroupStamper.TryClassify's mixed/open-member tolerance (see that
    /// method's own doc comment): a group pairing an all-solid member with an open-cornered member
    /// whose sole door-family edge faces its own group-mate (interior, never the group's own perimeter)
    /// now classifies as OpenSetPiece instead of being rejected outright. udp2's "Office_Vinyl_Entry
    /// 2x1" is the flagship case -- verified directly against the raw .set data that its "Door" edge is
    /// interior-only.
    /// </summary>
    [Test]
    public void OfficeVinylEntryOnOfficeInteriors_NowPlacesInIsolation()
    {
        var tilesetProfile = new BaseGameTilesetProfiles().BuildTilesetProfiles()[BaseGameTilesetProfiles.OfficeInteriors];
        var layoutProfile = new StandardLayoutProfiles().BuildLayoutProfiles()[StandardLayoutProfiles.Halls];
        var model = LoadTileset(tilesetProfile.TilesetResref);

        var (successes, hits) = MeasureIsolatedGroupHits(tilesetProfile, layoutProfile, model, "Office_Vinyl_Entry 2x1", maxPerArea: 5, seedBase: 95000, seedCount: 150);

        successes.Should().BeGreaterThan(140);
        // Measured 96.7% (145/150) isolated -- a 1x2 footprint (no CenterTile-relocation dependency at
        // all, unlike the 2x2+ groups above) is trivial for TryPlaceOpenSetPiece's site search to place
        // in almost any open-terrain room. Safety margin well under the measured rate.
        hits.Should().BeGreaterOrEqualTo((int)(successes * 0.5),
            $"'Office_Vinyl_Entry 2x1' must place on a meaningful share of the {successes} successful seeds now that the mixed/open-member tolerance classifies it as OpenSetPiece (got {hits})");
    }

    /// <summary>
    /// Same mechanism as OfficeVinylEntryOnOfficeInteriors_NowPlacesInIsolation above, on tbx78's
    /// "elevator" group (Rows=1/Columns=2 -- a "wall"/"facility" split tile whose "doorway2" edge faces
    /// its own group-mate, interior-only, verified directly against the raw .set data).
    /// </summary>
    [Test]
    public void ElevatorOnModernFacility_NowPlacesInIsolation()
    {
        var tilesetProfile = new BaseGameTilesetProfiles().BuildTilesetProfiles()[BaseGameTilesetProfiles.ModernFacility];
        var layoutProfile = new StandardLayoutProfiles().BuildLayoutProfiles()[StandardLayoutProfiles.Halls];
        var model = LoadTileset(tilesetProfile.TilesetResref);

        var (successes, hits) = MeasureIsolatedGroupHits(tilesetProfile, layoutProfile, model, "elevator", maxPerArea: 5, seedBase: 95000, seedCount: 150);

        successes.Should().BeGreaterThan(140);
        // Measured 100% (150/150) isolated -- same trivial 1x2-footprint reasoning as
        // OfficeVinylEntryOnOfficeInteriors_NowPlacesInIsolation above. Safety margin well under the
        // measured rate.
        hits.Should().BeGreaterOrEqualTo((int)(successes * 0.5),
            $"'elevator' must place on a meaningful share of the {successes} successful seeds now that the mixed/open-member tolerance classifies it as OpenSetPiece (got {hits})");
    }

    // ---------------- zin01 Window-crossered WallRoom proofs ----------------

    /// <summary>
    /// Placement proof for declaring "Window" as a DoorSlotCrosser on CepCityInterior (see that
    /// profile's own doc comment): "Window" is a genuine .set CROSSER TYPE (not a terrain name), but
    /// unlike Doorway/ElvenHallway/SigilHallway it was never declared anywhere, so IsAllowedMemberEdge
    /// rejected every group carrying a Window edge outright regardless of shape. Six previously-exempt
    /// groups are all-Wall-cornered WallRoom shapes whose Window edge sits on a genuine perimeter face
    /// (a "this room has a window on its far wall" pattern, opposite a real Doorway-ported entrance on
    /// the SAME group for the five room pairs) -- once Window is recognized, they classify exactly like
    /// any ordinary WallRoom with a real Doorway port, and TryPlaceWallRoom's OpenLane-boundary site
    /// search (zin01 already supports it -- Elven/Sigil's own door-ported rooms prove that) treats the
    /// Window edge identically to a Doorway edge for site validity. Verified directly against every
    /// tile's raw .set data that none of these six groups mixes Window with a body crosser (Corridor) on
    /// the same member -- "[City] Window - Porthole 1/2" DO mix Window with Corridor edges and stay
    /// correctly rejected (see BaseGameTilesetProfiles.CepCityInterior's own doc comment).
    /// All six are ALREADY registered as SetPieces on the base profile (BaseGameTilesetProfiles.
    /// CepCityInterior) from before this closure -- they were silently dead weight (classify-reject, so
    /// Stamp never even tried them) until this declaration made them reachable.
    /// </summary>
    [Test]
    public void WindowCrosseredGroupsOnCepCityInterior_NowPlaceInIsolation()
    {
        var tilesetProfile = new BaseGameTilesetProfiles().BuildTilesetProfiles()[BaseGameTilesetProfiles.CepCityInterior];
        var layoutProfile = new StandardLayoutProfiles().BuildLayoutProfiles()[StandardLayoutProfiles.Halls];
        var model = LoadTileset(tilesetProfile.TilesetResref);

        // Measured (seedBase 95000, 150 seeds each, all successes=150): Porthole 3 73 (48.7%), Living
        // Room 1 65 (43.3%), Living Room 2 48 (32.0%), Kitchen 1 65 (43.3%), Kitchen 2 53 (35.3%),
        // Inn 2 42 (28.0%) -- every group clears a healthy, well-above-noise floor. Threshold set well
        // under the lowest measured rate (Inn 2's 28.0%) for safety margin.
        foreach (var name in new[]
                 {
                     "[City] Window - Porthole 3",
                     "[City] Room - Living Room 1, Window (1x2)",
                     "[City] Room - Living Room 2, Window (1x2)",
                     "[City] Room - Kitchen 1, Window (1x2)",
                     "[City] Room - Kitchen 2, Window (1x2)",
                     "[City] Room - Inn 2, Window (1x2)",
                 })
        {
            var (successes, hits) = MeasureIsolatedGroupHits(tilesetProfile, layoutProfile, model, name, maxPerArea: 5, seedBase: 95000, seedCount: 150);

            successes.Should().BeGreaterThan(140);
            hits.Should().BeGreaterOrEqualTo((int)(successes * 0.15),
                $"'{name}' must place on a meaningful share of the {successes} successful seeds now that Window is a declared DoorSlotCrosser (got {hits})");
        }
    }

    // ---------------- tqq01 Livingroom/Kitchen/Shop district registration proofs ----------------

    /// <summary>
    /// Placement proof for the tqq01 district registration (see BaseGameTilesetProfiles.LabStorage's
    /// own doc comment): TileCoverageCensusTests already read 305/305 (100%) for tqq01 purely
    /// structurally, but no profile registered a single Livingroom/Kitchen/Shop group as a SetPiece, so
    /// LayoutGroupStamper.Stamp (which only iterates parameters.SetPieces.Keys) never actually placed
    /// any of them. One WallRoom-shape group (the district's own "Room"-family 1x1, all-Wall corners
    /// with an ordinary Doorway port -- classify-eligible even without PrimaryOpenTerrain, since WallRoom
    /// never checks terrain) and one OpenSetPiece-shape group (the district's own "Door*01" 1x1, mixed
    /// Wall/<District> corners with a door slot and no crosser -- genuinely NEEDS this variant's
    /// PrimaryOpenTerrain declaration to resolve) are measured per district; Livingroom/Kitchen also get
    /// their own "CornerExit1" (same OpenSetPiece shape as Door*01, this district's own stairs/exit
    /// family) since Shop has no equivalent group to compare against.
    /// </summary>
    [Test]
    public void LabStorageDistrictGroups_PlaceInIsolation()
    {
        var layoutProfile = new StandardLayoutProfiles().BuildLayoutProfiles()[StandardLayoutProfiles.Halls];
        var profiles = new BaseGameTilesetProfiles().BuildTilesetProfiles();

        // Measured (seedBase 95000, 150 seeds each): every group below placed on 100% of its successful
        // seeds (Livingroom/DoorLivingroom01/KitchenRoom/DoorKitchen01/ShopRoom/DoorShop01 all 150/150;
        // LivingroomCornerExit1/KitchenCornerExit1 both 142/142, successes=142 -- these two draw one
        // extra RNG shuffle before the flagship groups in the same isolated SetPieces dictionary probe,
        // shifting which seeds reach a successful LayoutSolver attempt, not a placement failure). All
        // single-tile footprints, trivial for TryPlaceWallRoom/TryPlaceOpenSetPiece's site search.
        // Threshold set well under the measured 94.7%+ floor for safety margin.
        foreach (var (profileKey, groupName) in new[]
                 {
                     (BaseGameTilesetProfiles.LabStorageLivingroom, "Livingroom"),
                     (BaseGameTilesetProfiles.LabStorageLivingroom, "DoorLivingroom01"),
                     (BaseGameTilesetProfiles.LabStorageLivingroom, "LivingroomCornerExit1"),
                     (BaseGameTilesetProfiles.LabStorageKitchen, "KitchenRoom"),
                     (BaseGameTilesetProfiles.LabStorageKitchen, "DoorKitchen01"),
                     (BaseGameTilesetProfiles.LabStorageKitchen, "KitchenCornerExit1"),
                     (BaseGameTilesetProfiles.LabStorageShop, "ShopRoom"),
                     (BaseGameTilesetProfiles.LabStorageShop, "DoorShop01"),
                 })
        {
            var tilesetProfile = profiles[profileKey];
            var model = LoadTileset(tilesetProfile.TilesetResref);
            var (successes, hits) = MeasureIsolatedGroupHits(tilesetProfile, layoutProfile, model, groupName, maxPerArea: 5, seedBase: 95000, seedCount: 150);

            successes.Should().BeGreaterThan(130);
            hits.Should().BeGreaterOrEqualTo((int)(successes * 0.5),
                $"'{groupName}' on '{profileKey}' must place on a meaningful share of the {successes} successful seeds (got {hits})");
        }
    }

    // ---------------- ttf01 Good/Evil Castle ExitGroup placement proofs ----------------

    /// <summary>
    /// Isolates one named 1x1 ExitGroup group as the composition's ONLY configured exit-group
    /// candidate (mirroring <see cref="MeasureIsolatedGroupHits"/>'s in-place-mutation isolation
    /// technique, but for <see cref="DungeonTilesetProfile.ExitGroups"/> rather than SetPieces) and
    /// returns how many of <paramref name="seedCount"/> single-attempt seeds pin it via
    /// GroupExitPlanner (the same PinnedTiles signal MeasureIsolatedGroupHits reads -- GroupExitPlanner
    /// writes its placement into MacroLayout.PinnedTiles exactly like LayoutGroupStamper does, see
    /// GroupExitPlanner.ApplyGroupExits).
    /// </summary>
    private static (int Successes, int Hits) MeasureIsolatedExitGroupHits(
        DungeonTilesetProfile tilesetProfile, DungeonLayoutProfile layoutProfile, TilesetModel model,
        string groupName, int seedBase, int seedCount, int seedStride = 13)
    {
        tilesetProfile.SetPieces = new Dictionary<string, int>();
        tilesetProfile.ExitGroups = new List<string> { groupName };

        var group = model.Groups.First(g => string.Equals(g.Name, groupName, StringComparison.OrdinalIgnoreCase));
        var anchorTileId = group.TileIds[0];

        var successes = 0;
        var hits = 0;
        for (var i = 0; i < seedCount; i++)
        {
            var seed = seedBase + i * seedStride;
            var composition = new DungeonComposition { Tileset = tilesetProfile, Layout = layoutProfile };
            var result = LayoutSolver.Solve(composition.BuildLayoutParameters(), model, Size, Size, seed, tilesetProfile.PrimaryOpenTerrain, retryCount: 1);
            if (!result.Success) continue;
            successes++;

            if (result.Layout.PinnedTiles.Values.Any(p => p.TileId == anchorTileId))
                hits++;
        }

        return (successes, hits);
    }

    /// <summary>
    /// Placement proof for BaseGameTilesetProfiles.ForestGoodCastle/ForestEvilCastle (see those
    /// profiles' own doc comments): under the base Forest profile's Solid=Cliff/Open=Forest pair,
    /// GoodCastle/EvilCastle corners never appear in a real corner grid, so GroupExitPlanner's exact
    /// corner-match requirement could never place these groups even though the census's
    /// vocab-independent IsExitGroupEligible structural check already counted them as reachable. Each
    /// variant's own SolidTerrainOverride(&lt;faction&gt;Castle) makes the castle terrain a real wall
    /// material, so it genuinely appears in the grid and these groups can actually place. Measured
    /// (seedBase 95000, 150 seeds each, all successes=150): every one of the six groups places
    /// 150/150 (100%) -- a single 1x1 footprint on the composed castle-terrain wall is trivial for
    /// GroupExitPlanner's ring search once the wall's own corner terrain actually matches (unlike the
    /// base Forest profile, where it structurally never can). Threshold set well under the measured
    /// floor for safety margin.
    /// </summary>
    [Test]
    public void GoodEvilCastleDoorGroups_PlaceAsGroupExits()
    {
        var layoutProfile = new StandardLayoutProfiles().BuildLayoutProfiles()[StandardLayoutProfiles.Halls];
        var profiles = new BaseGameTilesetProfiles().BuildTilesetProfiles();

        foreach (var (profileKey, groupName) in new[]
                 {
                     (BaseGameTilesetProfiles.ForestGoodCastle, "Castle - Main Door, Good"),
                     (BaseGameTilesetProfiles.ForestGoodCastle, "Castle - Small Door, Good"),
                     (BaseGameTilesetProfiles.ForestGoodCastle, "Castle - Breach, Good"),
                     (BaseGameTilesetProfiles.ForestEvilCastle, "Castle - Main Door, Evil"),
                     (BaseGameTilesetProfiles.ForestEvilCastle, "Castle - Small Door, Evil"),
                     (BaseGameTilesetProfiles.ForestEvilCastle, "Castle - Breach, Evil"),
                 })
        {
            var tilesetProfile = profiles[profileKey];
            var model = LoadTileset(tilesetProfile.TilesetResref);
            var (successes, hits) = MeasureIsolatedExitGroupHits(tilesetProfile, layoutProfile, model, groupName, seedBase: 95000, seedCount: 150);

            successes.Should().BeGreaterThan(130);
            hits.Should().BeGreaterOrEqualTo((int)(successes * 0.1),
                $"'{groupName}' on '{profileKey}' must place as a GroupExit on a meaningful share of the {successes} successful seeds (got {hits})");
        }
    }

    // ---------------- ttr01 Rural Grass placement proofs ----------------

    /// <summary>
    /// Placement proof for BaseGameTilesetProfiles.RuralGrassGoodCastle/RuralGrassEvilCastle, the same
    /// shape as GoodEvilCastleDoorGroups_PlaceAsGroupExits above (ttr01's own Castle - Main/Small
    /// Door/Breach groups only ever appear in a real corner grid once each variant's own
    /// SolidTerrainOverride(&lt;faction&gt;Castle) composes the castle terrain as a genuine wall
    /// material). Measured (seedBase 95000, 150 seeds each, all successes=150): every one of the six
    /// groups places 150/150 (100%). Threshold set well under the measured floor for safety margin.
    /// </summary>
    [Test]
    public void RuralGrassCastleDoorGroups_PlaceAsGroupExits()
    {
        var layoutProfile = new StandardLayoutProfiles().BuildLayoutProfiles()[StandardLayoutProfiles.Halls];
        var profiles = new BaseGameTilesetProfiles().BuildTilesetProfiles();

        foreach (var (profileKey, groupName) in new[]
                 {
                     (BaseGameTilesetProfiles.RuralGrassGoodCastle, "Castle - Main Door, Good"),
                     (BaseGameTilesetProfiles.RuralGrassGoodCastle, "Castle - Small Door, Good"),
                     (BaseGameTilesetProfiles.RuralGrassGoodCastle, "Castle - Breach, Good"),
                     (BaseGameTilesetProfiles.RuralGrassEvilCastle, "Castle - Main Door, Evil"),
                     (BaseGameTilesetProfiles.RuralGrassEvilCastle, "Castle - Small Door, Evil"),
                     (BaseGameTilesetProfiles.RuralGrassEvilCastle, "Castle - Breach, Evil"),
                 })
        {
            var tilesetProfile = profiles[profileKey];
            var model = LoadTileset(tilesetProfile.TilesetResref);
            var (successes, hits) = MeasureIsolatedExitGroupHits(tilesetProfile, layoutProfile, model, groupName, seedBase: 95000, seedCount: 150);

            successes.Should().BeGreaterThan(130);
            hits.Should().BeGreaterOrEqualTo((int)(successes * 0.5),
                $"'{groupName}' on '{profileKey}' must place as a GroupExit on a meaningful share of the {successes} successful seeds (got {hits})");
        }
    }

    /// <summary>
    /// Placement proof for BaseGameTilesetProfiles.RuralGrass's own all-Grass OpenSetPiece family (see
    /// that profile's own doc comment): three representative multi-tile groups spanning the 2x2/3x3
    /// footprint range. Measured (seedBase 95000, 150 seeds each, all successes=150): all three place
    /// 150/150 (100%) -- an ordinary flat all-open-terrain footprint is trivial for
    /// TryPlaceOpenSetPiece's site search on an open field with no wall competition at all. Threshold
    /// set well under the measured floor for safety margin.
    /// </summary>
    [Test]
    public void RuralGrassOpenSetPieces_PlaceInIsolation()
    {
        var tilesetProfile = new BaseGameTilesetProfiles().BuildTilesetProfiles()[BaseGameTilesetProfiles.RuralGrass];
        var layoutProfile = new StandardLayoutProfiles().BuildLayoutProfiles()[StandardLayoutProfiles.Halls];
        var model = LoadTileset(tilesetProfile.TilesetResref);

        foreach (var name in new[] { "Barn 1 (2x2)", "Temple - Good (3x3)", "Windmill (2x2)" })
        {
            var (successes, hits) = MeasureIsolatedGroupHits(tilesetProfile, layoutProfile, model, name, maxPerArea: 5, seedBase: 95000, seedCount: 150);

            successes.Should().BeGreaterThan(140);
            hits.Should().BeGreaterOrEqualTo((int)(successes * 0.5),
                $"'{name}' must place on a meaningful share of the {successes} successful seeds (got {hits})");
        }
    }

    /// <summary>
    /// Placement proof for BaseGameTilesetProfiles.RuralGrassWater's Grass+Water mixed OpenSetPiece
    /// family (see that profile's own doc comment): "Ship - Docked 1 (2x2)" (flat) places at a healthy
    /// rate, but "Cave - Sea" and "Pier" (both NONFLAT -- a baked height-1 bank edge) measure 0/150 in
    /// isolation -- TryPlaceOpenSetPiece's site search only ever finds FLAT open-room interiors under
    /// the currently-supported layouts, and neither group's exact raised-bank corner/height pattern
    /// ever spontaneously occurs in a generated room. This is a genuine, separate geometric ceiling
    /// (the same "documented, not silently regressed" shape as
    /// CavePlatform1OnMinesAndCavernsComplex_StillDoesNotPlace_DocumentedRoomSizeCeiling above), locked
    /// in here so a future change to relief/room generation that starts placing them is a deliberate,
    /// visible decision rather than a silent behavior drift.
    /// </summary>
    [Test]
    public void RuralGrassWaterOpenSetPieces_ShipDockedPlacesButNonflatBankPiecesDoNot()
    {
        var tilesetProfile = new BaseGameTilesetProfiles().BuildTilesetProfiles()[BaseGameTilesetProfiles.RuralGrassWater];
        var layoutProfile = new StandardLayoutProfiles().BuildLayoutProfiles()[StandardLayoutProfiles.Halls];
        var model = LoadTileset(tilesetProfile.TilesetResref);

        var (dockedSuccesses, dockedHits) = MeasureIsolatedGroupHits(tilesetProfile, layoutProfile, model, "Ship - Docked 1 (2x2)", maxPerArea: 5, seedBase: 95000, seedCount: 150);
        dockedSuccesses.Should().BeGreaterThan(140);
        // Measured 40.7% (61/150). Safety margin under the measured rate.
        dockedHits.Should().BeGreaterOrEqualTo((int)(dockedSuccesses * 0.2),
            $"'Ship - Docked 1 (2x2)' must place on a meaningful share of the {dockedSuccesses} successful seeds (got {dockedHits})");

        foreach (var name in new[] { "Cave - Sea", "Pier" })
        {
            var (successes, hits) = MeasureIsolatedGroupHits(tilesetProfile, layoutProfile, model, name, maxPerArea: 5, seedBase: 95000, seedCount: 150);
            successes.Should().BeGreaterThan(140);
            hits.Should().Be(0,
                $"'{name}' is a nonflat bank footprint with no flat open-room site TryPlaceOpenSetPiece can ever match -- " +
                "if this ever starts placing, room/relief generation changed and this test (and its doc comment) should be revisited, not silently deleted");
        }
    }

    // ---------------- tts01 Rural Winter* placement proofs ----------------

    /// <summary>
    /// Placement proof for BaseGameTilesetProfiles.RuralWinterGoodCastle/RuralWinterEvilCastle, the
    /// same shape as RuralGrassCastleDoorGroups_PlaceAsGroupExits above (tts01's own Castle - Main/
    /// Small Door/Breach groups only ever appear in a real corner grid once each variant's own
    /// SolidTerrainOverride(&lt;faction&gt;Castle) composes the castle terrain as a genuine wall
    /// material). Measured (seedBase 95000, 150 seeds each, all successes=150): every one of the six
    /// groups places 150/150 (100%). Threshold set well under the measured floor for safety margin.
    /// </summary>
    [Test]
    public void RuralWinterCastleDoorGroups_PlaceAsGroupExits()
    {
        var layoutProfile = new StandardLayoutProfiles().BuildLayoutProfiles()[StandardLayoutProfiles.Halls];
        var profiles = new BaseGameTilesetProfiles().BuildTilesetProfiles();

        foreach (var (profileKey, groupName) in new[]
                 {
                     (BaseGameTilesetProfiles.RuralWinterGoodCastle, "Castle - Main Door, Good"),
                     (BaseGameTilesetProfiles.RuralWinterGoodCastle, "Castle - Small Door, Good"),
                     (BaseGameTilesetProfiles.RuralWinterGoodCastle, "Castle - Breach, Good"),
                     (BaseGameTilesetProfiles.RuralWinterEvilCastle, "Castle - Main Door, Evil"),
                     (BaseGameTilesetProfiles.RuralWinterEvilCastle, "Castle - Small Door, Evil"),
                     (BaseGameTilesetProfiles.RuralWinterEvilCastle, "Castle - Breach, Evil"),
                 })
        {
            var tilesetProfile = profiles[profileKey];
            var model = LoadTileset(tilesetProfile.TilesetResref);
            var (successes, hits) = MeasureIsolatedExitGroupHits(tilesetProfile, layoutProfile, model, groupName, seedBase: 95000, seedCount: 150);

            successes.Should().BeGreaterThan(130);
            hits.Should().BeGreaterOrEqualTo((int)(successes * 0.5),
                $"'{groupName}' on '{profileKey}' must place as a GroupExit on a meaningful share of the {successes} successful seeds (got {hits})");
        }
    }

    /// <summary>
    /// Placement proof for BaseGameTilesetProfiles.RuralWinter's own all-Snow OpenSetPiece family (see
    /// that profile's own doc comment): three representative multi-tile groups plus "Turf House (2x2)"
    /// (the tts01-only addition with no ttr01 counterpart). Measured (seedBase 95000, 150 seeds each,
    /// all successes=150): all four place 150/150 (100%) -- the same trivial open-field result
    /// RuralGrassOpenSetPieces_PlaceInIsolation measures on ttr01. Threshold set well under the measured
    /// floor for safety margin.
    /// </summary>
    [Test]
    public void RuralWinterOpenSetPieces_PlaceInIsolation()
    {
        var tilesetProfile = new BaseGameTilesetProfiles().BuildTilesetProfiles()[BaseGameTilesetProfiles.RuralWinter];
        var layoutProfile = new StandardLayoutProfiles().BuildLayoutProfiles()[StandardLayoutProfiles.Halls];
        var model = LoadTileset(tilesetProfile.TilesetResref);

        foreach (var name in new[] { "Barn 1 (2x2)", "Temple - Good (3x3)", "Windmill (2x2)", "Turf House (2x2)" })
        {
            var (successes, hits) = MeasureIsolatedGroupHits(tilesetProfile, layoutProfile, model, name, maxPerArea: 5, seedBase: 95000, seedCount: 150);

            successes.Should().BeGreaterThan(140);
            hits.Should().BeGreaterOrEqualTo((int)(successes * 0.5),
                $"'{name}' must place on a meaningful share of the {successes} successful seeds (got {hits})");
        }
    }

    /// <summary>
    /// Placement proof for BaseGameTilesetProfiles.RuralWinter's door-bearing ExitGroups: "House 1"
    /// (carried over unchanged from ttr01) and "Turf House" (the tts01-only delta -- ttr01's own copy
    /// is doorless and lives as a FeatureTile there, but tts01's carries a real door, so it belongs
    /// here instead; see BaseGameTilesetProfiles.RuralWinter's own doc comment). Measured (seedBase
    /// 95000, 150 seeds, successes=150): both place 150/150 (100%).
    /// </summary>
    [Test]
    public void RuralWinterDoorBearingExitGroups_PlaceAsGroupExits()
    {
        var tilesetProfile = new BaseGameTilesetProfiles().BuildTilesetProfiles()[BaseGameTilesetProfiles.RuralWinter];
        var layoutProfile = new StandardLayoutProfiles().BuildLayoutProfiles()[StandardLayoutProfiles.Halls];
        var model = LoadTileset(tilesetProfile.TilesetResref);

        foreach (var groupName in new[] { "House 1", "Turf House" })
        {
            var (successes, hits) = MeasureIsolatedExitGroupHits(tilesetProfile, layoutProfile, model, groupName, seedBase: 95000, seedCount: 150);

            successes.Should().BeGreaterThan(140);
            hits.Should().BeGreaterOrEqualTo((int)(successes * 0.5),
                $"'{groupName}' must place as a GroupExit on a meaningful share of the {successes} successful seeds (got {hits})");
        }
    }

    /// <summary>
    /// Placement proof for BaseGameTilesetProfiles.RuralWinterWater's Snow+Water mixed OpenSetPiece
    /// family: "Ship - Docked 1 (2x2)" (flat) and "Ship - Air, Above Water (3x1)" (all-Water, WallAlcove
    /// via a real door on TILE557). Measured (seedBase 95000, 150 seeds each, successes=150):
    /// "Ship - Docked 1 (2x2)" places at 40.7% (61/150) -- the identical rate
    /// RuralGrassWaterOpenSetPieces_ShipDockedPlacesButNonflatBankPiecesDoNot measures on ttr01's own
    /// copy (same TileIds, same footprint) -- and "Ship - Air, Above Water (3x1)" places 150/150
    /// (100%). Unlike RuralGrassWater, tts01 has no "Cave - Sea"/"Pier" nonflat-bank counterpart at all
    /// (see BaseGameTilesetProfiles.RuralWinter's own doc comment), so there is no matching 0/150
    /// ceiling test to carry over here.
    /// </summary>
    [Test]
    public void RuralWinterWaterOpenSetPieces_PlaceInIsolation()
    {
        var tilesetProfile = new BaseGameTilesetProfiles().BuildTilesetProfiles()[BaseGameTilesetProfiles.RuralWinterWater];
        var layoutProfile = new StandardLayoutProfiles().BuildLayoutProfiles()[StandardLayoutProfiles.Halls];
        var model = LoadTileset(tilesetProfile.TilesetResref);

        var (dockedSuccesses, dockedHits) = MeasureIsolatedGroupHits(tilesetProfile, layoutProfile, model, "Ship - Docked 1 (2x2)", maxPerArea: 5, seedBase: 95000, seedCount: 150);
        dockedSuccesses.Should().BeGreaterThan(140);
        // Measured 40.7% (61/150). Safety margin under the measured rate.
        dockedHits.Should().BeGreaterOrEqualTo((int)(dockedSuccesses * 0.2),
            $"'Ship - Docked 1 (2x2)' must place on a meaningful share of the {dockedSuccesses} successful seeds (got {dockedHits})");

        var (airSuccesses, airHits) = MeasureIsolatedGroupHits(tilesetProfile, layoutProfile, model, "Ship - Air, Above Water (3x1)", maxPerArea: 5, seedBase: 95000, seedCount: 150);
        airSuccesses.Should().BeGreaterThan(140);
        airHits.Should().BeGreaterOrEqualTo((int)(airSuccesses * 0.5),
            $"'Ship - Air, Above Water (3x1)' must place on a meaningful share of the {airSuccesses} successful seeds (got {airHits})");
    }

    // ---------------- tts02 Rural Winter - Facelift placement proofs ----------------

    /// <summary>
    /// Placement proof for BaseGameTilesetProfiles.RuralWinterFacelift's own all-Snow OpenSetPiece
    /// family: three groups carried over from RuralWinter's own already-verified roster plus
    /// "Turfhouse_2x2" (the vanilla-underscored name for RuralWinter's "Turf House (2x2)"). Measured
    /// (ProbeTool, seedBase 95000, 150 seeds each, all successes=150): all four place 150/150 (100%) --
    /// the same trivial open-field result RuralWinterOpenSetPieces_PlaceInIsolation measures on tts01.
    /// Threshold set well under the measured floor for safety margin.
    /// </summary>
    [Test]
    public void RuralWinterFaceliftOpenSetPieces_PlaceInIsolation()
    {
        var tilesetProfile = new BaseGameTilesetProfiles().BuildTilesetProfiles()[BaseGameTilesetProfiles.RuralWinterFacelift];
        var layoutProfile = new StandardLayoutProfiles().BuildLayoutProfiles()[StandardLayoutProfiles.Halls];
        var model = LoadTileset(tilesetProfile.TilesetResref);

        foreach (var name in new[] { "Barn01_2x2", "GoodTemple_3x3", "Windmill_2x2", "Turfhouse_2x2" })
        {
            var (successes, hits) = MeasureIsolatedGroupHits(tilesetProfile, layoutProfile, model, name, maxPerArea: 5, seedBase: 95000, seedCount: 150);

            successes.Should().BeGreaterThan(140);
            hits.Should().BeGreaterOrEqualTo((int)(successes * 0.5),
                $"'{name}' must place on a meaningful share of the {successes} successful seeds (got {hits})");
        }
    }

    /// <summary>
    /// Placement proof for BaseGameTilesetProfiles.RuralWinterFacelift's door-bearing ExitGroups:
    /// "House01" (carried over from RuralWinter's own roster) and "HouseV2"/"HouseV3" (tts02-only
    /// additions with no RuralWinter counterpart -- two more house-model variants sharing the identical
    /// flat/crosser-free/uniform-Snow/real-door shape). Measured (seedBase 95000, 150 seeds,
    /// successes=150): all three place 150/150 (100%).
    /// </summary>
    [Test]
    public void RuralWinterFaceliftDoorBearingExitGroups_PlaceAsGroupExits()
    {
        var tilesetProfile = new BaseGameTilesetProfiles().BuildTilesetProfiles()[BaseGameTilesetProfiles.RuralWinterFacelift];
        var layoutProfile = new StandardLayoutProfiles().BuildLayoutProfiles()[StandardLayoutProfiles.Halls];
        var model = LoadTileset(tilesetProfile.TilesetResref);

        foreach (var groupName in new[] { "House01", "HouseV2", "HouseV3" })
        {
            var (successes, hits) = MeasureIsolatedExitGroupHits(tilesetProfile, layoutProfile, model, groupName, seedBase: 95000, seedCount: 150);

            successes.Should().BeGreaterThan(140);
            hits.Should().BeGreaterOrEqualTo((int)(successes * 0.5),
                $"'{groupName}' must place as a GroupExit on a meaningful share of the {successes} successful seeds (got {hits})");
        }
    }

    /// <summary>
    /// Placement proof for BaseGameTilesetProfiles.RuralWinterFaceliftWater's Snow+Water mixed
    /// OpenSetPiece "ShipDocked01_2x2" (flat, the vanilla-underscored name for RuralWinter's "Ship -
    /// Docked 1 (2x2)"). Measured (seedBase 95000, 150 seeds, successes=150): places at 40.7% (61/150)
    /// -- the IDENTICAL rate RuralWinterWaterOpenSetPieces_PlaceInIsolation measures on tts01's own copy,
    /// confirmed independently against tts02's own (different) TileIds, not assumed from the shared
    /// vanilla lineage. tts02 has no "Ship - Air, Above Water" counterpart (see
    /// BaseGameTilesetProfiles.RuralWinterFaceliftWater's own doc comment), so there is no matching
    /// second group to measure here.
    /// </summary>
    [Test]
    public void RuralWinterFaceliftWaterOpenSetPieces_PlaceInIsolation()
    {
        var tilesetProfile = new BaseGameTilesetProfiles().BuildTilesetProfiles()[BaseGameTilesetProfiles.RuralWinterFaceliftWater];
        var layoutProfile = new StandardLayoutProfiles().BuildLayoutProfiles()[StandardLayoutProfiles.Halls];
        var model = LoadTileset(tilesetProfile.TilesetResref);

        var (dockedSuccesses, dockedHits) = MeasureIsolatedGroupHits(tilesetProfile, layoutProfile, model, "ShipDocked01_2x2", maxPerArea: 5, seedBase: 95000, seedCount: 150);
        dockedSuccesses.Should().BeGreaterThan(140);
        // Measured 40.7% (61/150). Safety margin under the measured rate.
        dockedHits.Should().BeGreaterOrEqualTo((int)(dockedSuccesses * 0.2),
            $"'ShipDocked01_2x2' must place on a meaningful share of the {dockedSuccesses} successful seeds (got {dockedHits})");
    }

    /// <summary>
    /// Placement proof for BaseGameTilesetProfiles.RuralWinterFaceliftFort's sole ExitGroup,
    /// "WallGate3" (Fort+Snow mixed corners, one real door -- only ever a candidate once
    /// SolidTerrainOverride("Fort") composes Fort as a genuine wall material, the same mechanism as
    /// RuralWinterCastleDoorGroups_PlaceAsGroupExits above). Measured (seedBase 95000, 150 seeds,
    /// successes=150): places 150/150 (100%).
    /// </summary>
    [Test]
    public void RuralWinterFaceliftFortDoorGroup_PlacesAsGroupExit()
    {
        var tilesetProfile = new BaseGameTilesetProfiles().BuildTilesetProfiles()[BaseGameTilesetProfiles.RuralWinterFaceliftFort];
        var layoutProfile = new StandardLayoutProfiles().BuildLayoutProfiles()[StandardLayoutProfiles.Halls];
        var model = LoadTileset(tilesetProfile.TilesetResref);

        var (successes, hits) = MeasureIsolatedExitGroupHits(tilesetProfile, layoutProfile, model, "WallGate3", seedBase: 95000, seedCount: 150);
        successes.Should().BeGreaterThan(140);
        hits.Should().BeGreaterOrEqualTo((int)(successes * 0.5),
            $"'WallGate3' must place as a GroupExit on a meaningful share of the {successes} successful seeds (got {hits})");
    }

    /// <summary>
    /// Placement proof for BaseGameTilesetProfiles.RuralWinterFaceliftFort's two doorless Fort+Snow
    /// boundary OpenSetPieces, "WallBreach" and "WatchTower" -- only ever candidates once
    /// SolidTerrainOverride("Fort") composes Fort as a genuine Solid. Measured (ProbeTool "fortprobe",
    /// seedBase 95000, 100 seeds each, all successes=100): 89/100 each on Halls, 90/100 on Complex,
    /// 100/100 on Organic. Threshold set well under the measured floor for safety margin.
    /// </summary>
    [Test]
    public void RuralWinterFaceliftFortBoundaryPieces_PlaceInIsolation()
    {
        var tilesetProfile = new BaseGameTilesetProfiles().BuildTilesetProfiles()[BaseGameTilesetProfiles.RuralWinterFaceliftFort];
        var layoutProfile = new StandardLayoutProfiles().BuildLayoutProfiles()[StandardLayoutProfiles.Halls];
        var model = LoadTileset(tilesetProfile.TilesetResref);

        foreach (var name in new[] { "WallBreach", "WatchTower" })
        {
            var (successes, hits) = MeasureIsolatedGroupHits(tilesetProfile, layoutProfile, model, name, maxPerArea: 5, seedBase: 95000, seedCount: 150);

            successes.Should().BeGreaterThan(140);
            hits.Should().BeGreaterOrEqualTo((int)(successes * 0.5),
                $"'{name}' must place on a meaningful share of the {successes} successful seeds (got {hits})");
        }
    }

    // ---------------- tno01 Castle Exterior, Rural* placement proofs ----------------

    /// <summary>
    /// Placement proof for tno01's wall-material door groups, the same shape as
    /// RuralGrassCastleDoorGroups_PlaceAsGroupExits above: each group's mixed solid/open corner tile
    /// only ever appears in a real corner grid once its own variant composes the wall material
    /// (castlewall or keep) as a genuine Solid terrain, and "CliffStairs" (cliff+grass) matches the
    /// base profile's own cliff walls directly. Measured (ProbeTool, seedBase 95000, 150 seeds each,
    /// successes=150): all five place 150/150 (100%). KeepDoor_Dirt and KeepTop_Stairs are
    /// DELIBERATELY NOT wired or measured here -- 0/150 each (dirt never composes in the Keep
    /// variant; an ALL-keep door tile can never corner-match GroupExitPlanner's wall-ring candidates,
    /// which always carry open-facing corners) -- see BaseGameTilesetProfiles.CastleExteriorRuralKeep's
    /// own doc comment.
    /// </summary>
    [Test]
    public void CastleExteriorRuralWallDoorGroups_PlaceAsGroupExits()
    {
        var layoutProfile = new StandardLayoutProfiles().BuildLayoutProfiles()[StandardLayoutProfiles.Halls];
        var profiles = new BaseGameTilesetProfiles().BuildTilesetProfiles();

        foreach (var (profileKey, groupName) in new[]
                 {
                     (BaseGameTilesetProfiles.CastleExteriorRuralCastleWall, "OuterWallDoor2"),
                     (BaseGameTilesetProfiles.CastleExteriorRuralCastleWall, "OuterWallDoor3"),
                     (BaseGameTilesetProfiles.CastleExteriorRuralCastleWall, "WallRaiseGate"),
                     (BaseGameTilesetProfiles.CastleExteriorRuralKeep, "KeepDoor_Grass"),
                     (BaseGameTilesetProfiles.CastleExteriorRural, "CliffStairs"),
                 })
        {
            var tilesetProfile = profiles[profileKey];
            var model = LoadTileset(tilesetProfile.TilesetResref);
            var (successes, hits) = MeasureIsolatedExitGroupHits(tilesetProfile, layoutProfile, model, groupName, seedBase: 95000, seedCount: 150);

            successes.Should().BeGreaterThan(130);
            hits.Should().BeGreaterOrEqualTo((int)(successes * 0.5),
                $"'{groupName}' on '{profileKey}' must place as a GroupExit on a meaningful share of the {successes} successful seeds (got {hits})");
        }
    }

    /// <summary>
    /// Placement proof for tno01's ALL-open-cornered house/tent door groups: on the base profile
    /// (grass copies of the halfling/tent family) and the Village variant (dirt copies of the
    /// duplicated house family -- FindGroup's first-match-by-name rule always resolves a duplicated
    /// name to its dirt copy, see BaseGameTilesetProfiles.CastleExteriorRural's own doc comment).
    /// Unlike the mixed-corner wall doors above, an all-open door tile only corner-matches an
    /// interstitial ring cell whose every corner happens to be open (a cell BETWEEN two open rooms),
    /// so the rate sits near 40%, not 100%. Measured (ProbeTool, seedBase 95000, 150 seeds each,
    /// successes=150): base grass exits 61/150 (40.7%), Village dirt exits 59/150 (39.3%). Threshold
    /// set well under the measured floor for safety margin.
    /// </summary>
    [Test]
    public void CastleExteriorRuralOpenCorneredExitGroups_PlaceAsGroupExits()
    {
        var layoutProfile = new StandardLayoutProfiles().BuildLayoutProfiles()[StandardLayoutProfiles.Halls];
        var profiles = new BaseGameTilesetProfiles().BuildTilesetProfiles();

        foreach (var (profileKey, groupName) in new[]
                 {
                     (BaseGameTilesetProfiles.CastleExteriorRural, "Halfling Burrow"),
                     (BaseGameTilesetProfiles.CastleExteriorRural, "Thatch_House_1"),
                     (BaseGameTilesetProfiles.CastleExteriorRural, "Ice_Cellar"),
                     (BaseGameTilesetProfiles.CastleExteriorRuralVillage, "house 1x1 m61"),
                     (BaseGameTilesetProfiles.CastleExteriorRuralVillage, "City_House_1x1_Tower_1"),
                     (BaseGameTilesetProfiles.CastleExteriorRuralVillage, "Crypt_Dirt"),
                 })
        {
            var tilesetProfile = profiles[profileKey];
            var model = LoadTileset(tilesetProfile.TilesetResref);
            var (successes, hits) = MeasureIsolatedExitGroupHits(tilesetProfile, layoutProfile, model, groupName, seedBase: 95000, seedCount: 150);

            successes.Should().BeGreaterThan(130);
            hits.Should().BeGreaterOrEqualTo((int)(successes * 0.2),
                $"'{groupName}' on '{profileKey}' must place as a GroupExit on a meaningful share of the {successes} successful seeds (got {hits})");
        }
    }

    /// <summary>
    /// Placement proof for tno01's building set pieces across the base (grass), Village (dirt), Water
    /// (grass-shoreline), and Harbor (dirt-waterfront) compositions. Measured rates (ProbeTool,
    /// seedBase 95000, 150 seeds each, successes=150): Cog_3x1 and Ship_4x1_cliffs 150/150 (WallAlcove
    /// hulls embedded in the cliff mass); Range 56.0%; Tent 1 29.3%; Village CoachInn/Inn 2x2/
    /// house_2x2_m40 all 29.3%; Water Grass_docks 89.3%, Ship_3x1_water and Ship_4x1_water 150/150
    /// (WallAlcove in the water mass); Harbor Docks_City 67.3%, City_boat_docked 40.7%,
    /// Ship_3x1_Docked 11.3%. Thresholds set well under each measured rate for safety margin.
    /// </summary>
    [Test]
    public void CastleExteriorRuralSetPieces_PlaceInIsolation()
    {
        var layoutProfile = new StandardLayoutProfiles().BuildLayoutProfiles()[StandardLayoutProfiles.Halls];
        var profiles = new BaseGameTilesetProfiles().BuildTilesetProfiles();

        foreach (var (profileKey, groupName, minShare) in new[]
                 {
                     (BaseGameTilesetProfiles.CastleExteriorRural, "Cog_3x1", 0.5),
                     (BaseGameTilesetProfiles.CastleExteriorRural, "Ship_4x1_cliffs", 0.5),
                     (BaseGameTilesetProfiles.CastleExteriorRural, "Range", 0.25),
                     (BaseGameTilesetProfiles.CastleExteriorRural, "Tent 1", 0.1),
                     (BaseGameTilesetProfiles.CastleExteriorRuralVillage, "CoachInn", 0.1),
                     (BaseGameTilesetProfiles.CastleExteriorRuralVillage, "Inn 2x2", 0.1),
                     (BaseGameTilesetProfiles.CastleExteriorRuralVillage, "house_2x2_m40", 0.1),
                     (BaseGameTilesetProfiles.CastleExteriorRuralWater, "Grass_docks", 0.5),
                     (BaseGameTilesetProfiles.CastleExteriorRuralWater, "Ship_3x1_water", 0.5),
                     (BaseGameTilesetProfiles.CastleExteriorRuralWater, "Ship_4x1_water", 0.5),
                     (BaseGameTilesetProfiles.CastleExteriorRuralHarbor, "Docks_City", 0.3),
                     (BaseGameTilesetProfiles.CastleExteriorRuralHarbor, "City_boat_docked", 0.2),
                     (BaseGameTilesetProfiles.CastleExteriorRuralHarbor, "Ship_3x1_Docked", 0.04),
                 })
        {
            var tilesetProfile = profiles[profileKey];
            var model = LoadTileset(tilesetProfile.TilesetResref);
            var (successes, hits) = MeasureIsolatedGroupHits(tilesetProfile, layoutProfile, model, groupName, maxPerArea: 5, seedBase: 95000, seedCount: 150);

            successes.Should().BeGreaterThan(130);
            hits.Should().BeGreaterOrEqualTo((int)(successes * minShare),
                $"'{groupName}' on '{profileKey}' must place on a meaningful share of the {successes} successful seeds (got {hits})");
        }
    }

    /// <summary>
    /// Documented placement ceilings for tno01's four wired-but-currently-unplaceable pieces (the
    /// same "documented, not silently regressed" shape as
    /// RuralGrassWaterOpenSetPieces_ShipDockedPlacesButNonflatBankPiecesDoNot above): "FantasyTower
    /// 4x4" and "Tower3 m69 3x3" need a room with a larger contiguous open interior than a 20x20
    /// area's Halls rooms ever produce (the ttd01 palais_jabba/Astroport footprint precedent); "Cave"
    /// (ReliefPiece) needs a painted raised rim edge whose exact corner field Halls' relief budget
    /// rarely produces at this size; "DockedShip_City" (4x2, 6 members) needs a water/dirt shoreline
    /// pattern that never spontaneously occurs inside a generated room. All measured 0/150 in
    /// isolation. If any starts placing, room/relief generation changed and the affected test (and
    /// its doc comment) should be revisited deliberately, not silently deleted.
    /// </summary>
    [Test]
    public void CastleExteriorRuralLargeFootprintPieces_StillDoNotPlace_DocumentedCeilings()
    {
        var layoutProfile = new StandardLayoutProfiles().BuildLayoutProfiles()[StandardLayoutProfiles.Halls];
        var profiles = new BaseGameTilesetProfiles().BuildTilesetProfiles();

        foreach (var (profileKey, groupName) in new[]
                 {
                     (BaseGameTilesetProfiles.CastleExteriorRural, "FantasyTower 4x4"),
                     (BaseGameTilesetProfiles.CastleExteriorRural, "Tower3 m69 3x3"),
                     (BaseGameTilesetProfiles.CastleExteriorRural, "Cave"),
                     (BaseGameTilesetProfiles.CastleExteriorRuralHarbor, "DockedShip_City"),
                 })
        {
            var tilesetProfile = profiles[profileKey];
            var model = LoadTileset(tilesetProfile.TilesetResref);
            var (successes, hits) = MeasureIsolatedGroupHits(tilesetProfile, layoutProfile, model, groupName, maxPerArea: 5, seedBase: 95000, seedCount: 150);

            successes.Should().BeGreaterThan(130);
            hits.Should().Be(0,
                $"'{groupName}' on '{profileKey}' has a documented placement ceiling at this size -- " +
                "if it ever starts placing, generation changed and this test (and its doc comment) should be revisited, not silently deleted");
        }
    }

    // ---------------- tti01 Frozen Wastes* placement proofs ----------------

    /// <summary>
    /// Placement proof for BaseGameTilesetProfiles.FrozenWastes' all-Floor OpenSetPiece family plus its
    /// one Solid(Pit)-anchored group. Measured (seedBase 95000, 150 seeds each, Halls, all
    /// successes=150): "Dragon Skeleton (1x2)" 68.7% (103/150), "Temple - Evil 1 (2x3)" 11.3% (17/150),
    /// "Temple - Neutral (2x2)" 40.7% (61/150), "Temple - Evil 2 (2x3)" 11.3% (17/150), "Ship - Air,
    /// Docked (3x1)" 29.3% (44/150), "Tower - Ice" 40.7% (61/150). "Ship - Air, Above Pit (3x1)"
    /// (all-Pit, door-bearing) measures 100% (150/150) -- unlike RuralGrass's own "Ship - Air, Above
    /// Trees (3x1)" (exempt there because Trees is never composed at all), Pit here IS this profile's
    /// own composed Solid/wall terrain, so an all-solid-cornered 3x1 door group anchors trivially
    /// against it. Thresholds set well under each measured floor for safety margin.
    /// </summary>
    [Test]
    public void FrozenWastesOpenSetPieces_PlaceInIsolation()
    {
        var tilesetProfile = new BaseGameTilesetProfiles().BuildTilesetProfiles()[BaseGameTilesetProfiles.FrozenWastes];
        var layoutProfile = new StandardLayoutProfiles().BuildLayoutProfiles()[StandardLayoutProfiles.Halls];
        var model = LoadTileset(tilesetProfile.TilesetResref);

        foreach (var (groupName, minShare) in new[]
                 {
                     ("Dragon Skeleton (1x2)", 0.4),
                     ("Temple - Evil 1 (2x3)", 0.05),
                     ("Temple - Neutral (2x2)", 0.2),
                     ("Temple - Evil 2 (2x3)", 0.05),
                     ("Ship - Air, Above Pit (3x1)", 0.5),
                     ("Ship - Air, Docked (3x1)", 0.15),
                     ("Tower - Ice", 0.2),
                 })
        {
            var (successes, hits) = MeasureIsolatedGroupHits(tilesetProfile, layoutProfile, model, groupName, maxPerArea: 5, seedBase: 95000, seedCount: 150);

            successes.Should().BeGreaterThan(140);
            hits.Should().BeGreaterOrEqualTo((int)(successes * minShare),
                $"'{groupName}' must place on a meaningful share of the {successes} successful seeds (got {hits})");
        }
    }

    /// <summary>
    /// Placement proof for BaseGameTilesetProfiles.FrozenWastes' "Entrance - Evil" ExitGroup (base
    /// profile, pure Floor corners) and FrozenWastesEvilCastle's own "Castle - Main Door/Breach/Small
    /// Door, Evil" trio (only ever appear in a real corner grid once the EvilCastle PaletteVariant's
    /// own SolidTerrainOverride("EvilCastle") composes that terrain as a genuine wall material, the
    /// same shape as RuralGrassCastleDoorGroups_PlaceAsGroupExits above). Measured (seedBase 95000, 150
    /// seeds each, all successes=150): "Entrance - Evil" 98.7% (148/150); all three castle groups 100%
    /// (150/150). Thresholds set well under the measured floor for safety margin.
    /// </summary>
    [Test]
    public void FrozenWastesExitGroups_PlaceAsGroupExits()
    {
        var layoutProfile = new StandardLayoutProfiles().BuildLayoutProfiles()[StandardLayoutProfiles.Halls];
        var profiles = new BaseGameTilesetProfiles().BuildTilesetProfiles();

        foreach (var (profileKey, groupName, minShare) in new[]
                 {
                     (BaseGameTilesetProfiles.FrozenWastes, "Entrance - Evil", 0.5),
                     (BaseGameTilesetProfiles.FrozenWastesEvilCastle, "Castle - Main Door, Evil", 0.5),
                     (BaseGameTilesetProfiles.FrozenWastesEvilCastle, "Castle - Breach, Evil", 0.5),
                     (BaseGameTilesetProfiles.FrozenWastesEvilCastle, "Castle - Small Door, Evil", 0.5),
                 })
        {
            var tilesetProfile = profiles[profileKey];
            var model = LoadTileset(tilesetProfile.TilesetResref);
            var (successes, hits) = MeasureIsolatedExitGroupHits(tilesetProfile, layoutProfile, model, groupName, seedBase: 95000, seedCount: 150);

            successes.Should().BeGreaterThan(140);
            hits.Should().BeGreaterOrEqualTo((int)(successes * minShare),
                $"'{groupName}' on '{profileKey}' must place as a GroupExit on a meaningful share of the {successes} successful seeds (got {hits})");
        }
    }

    // ---------------- ttz01 Tropical* placement proofs ----------------

    /// <summary>
    /// Placement proof for BaseGameTilesetProfiles.Tropical's non-door all-grass OpenSetPiece family.
    /// The nine DOOR-bearing siblings (Barn01_2x2, Barn02_1x2, Barn03_1x2, Inn_1x2, Farm01_2x2,
    /// Farm02_1x2, Farm03_1x2, Barracks_1x2, Windmill_2x2) route SetPieceWallAlcove instead and are
    /// wired on the profile since the terrain-label case unification resolved their measured
    /// Organic-disconnection gap -- see the profile's own "RESOLVED GAP" doc comment and
    /// TerrainLabelCaseUnificationTests. Measured post-unification (seedBase 95000, 150 seeds each,
    /// Halls, all successes=150): "DragSkel_1x2" 62.7% (94/150), "Field01_2x2" 28.7% (43/150),
    /// "Field02_2x2" 28.7% (43/150), "Field03_2x1" 62.7% (94/150), "Tower_1x2" 62.7% (94/150),
    /// "Warzone_1x2" 62.7% (94/150), "Temple03_3x2" 6.7% (10/150), "Temple02_2x2" 28.7% (43/150),
    /// "Temple01_3x2" 6.7% (10/150) -- within a hit or two of the pre-unification measurements (the
    /// unified spelling changes the composition's internal label regime, not its open-field shape).
    /// Thresholds set well under each measured floor for safety margin.
    /// </summary>
    [Test]
    public void TropicalOpenSetPieces_PlaceInIsolation()
    {
        var tilesetProfile = new BaseGameTilesetProfiles().BuildTilesetProfiles()[BaseGameTilesetProfiles.Tropical];
        var layoutProfile = new StandardLayoutProfiles().BuildLayoutProfiles()[StandardLayoutProfiles.Halls];
        var model = LoadTileset(tilesetProfile.TilesetResref);

        foreach (var (groupName, minShare) in new[]
                 {
                     ("DragSkel_1x2", 0.4),
                     ("Field01_2x2", 0.15),
                     ("Field02_2x2", 0.15),
                     ("Field03_2x1", 0.4),
                     ("Tower_1x2", 0.4),
                     ("Warzone_1x2", 0.4),
                     ("Temple03_3x2", 0.03),
                     ("Temple02_2x2", 0.15),
                     ("Temple01_3x2", 0.03),
                 })
        {
            var (successes, hits) = MeasureIsolatedGroupHits(tilesetProfile, layoutProfile, model, groupName, maxPerArea: 5, seedBase: 95000, seedCount: 150);

            successes.Should().BeGreaterThan(140);
            hits.Should().BeGreaterOrEqualTo((int)(successes * minShare),
                $"'{groupName}' must place on a meaningful share of the {successes} successful seeds (got {hits})");
        }
    }

    /// <summary>
    /// Placement proof for BaseGameTilesetProfiles.Tropical's nine door-bearing 1x2/2x2 groups, which
    /// route SetPieceWallAlcove on this Solid==Open composition (allCornersSolid is trivially true)
    /// and are wired since the terrain-label case unification resolved their measured Organic
    /// disconnection gap (see the profile's own "RESOLVED GAP" doc comment and
    /// TerrainLabelCaseUnificationTests, which pins the disconnection side). Measured (seedBase 95000,
    /// 150 seeds each, Halls, all successes=150): every one of the nine places on 100% (150/150) of
    /// seeds -- on an open-field composition a WallAlcove site (fully "solid" by the case-insensitive
    /// check, flat, crosser-free, unpinned, door member touching open space) exists practically
    /// everywhere. Threshold 0.5 for safety margin.
    /// </summary>
    [Test]
    public void TropicalWallAlcoves_PlaceInIsolation()
    {
        var tilesetProfile = new BaseGameTilesetProfiles().BuildTilesetProfiles()[BaseGameTilesetProfiles.Tropical];
        var layoutProfile = new StandardLayoutProfiles().BuildLayoutProfiles()[StandardLayoutProfiles.Halls];
        var model = LoadTileset(tilesetProfile.TilesetResref);

        foreach (var groupName in new[]
                 {
                     "Barn01_2x2", "Barn02_1x2", "Barn03_1x2", "Inn_1x2", "Farm01_2x2",
                     "Farm02_1x2", "Farm03_1x2", "Barracks_1x2", "Windmill_2x2",
                 })
        {
            var (successes, hits) = MeasureIsolatedGroupHits(tilesetProfile, layoutProfile, model, groupName, maxPerArea: 1, seedBase: 95000, seedCount: 150);

            successes.Should().BeGreaterThan(140);
            hits.Should().BeGreaterOrEqualTo((int)(successes * 0.5),
                $"'{groupName}' must place as a WallAlcove on a meaningful share of the {successes} successful seeds (got {hits})");
        }
    }

    /// <summary>
    /// Placement proof for BaseGameTilesetProfiles.TropicalSand's own OpenSetPiece family (all sand-
    /// only groups, sand's own open-field composition). "Barracks_1x2(sand)" IS door-bearing but
    /// measured 0/300 Organic disconnections (ProbeTool) even before the terrain-label case
    /// unification fix: its explicit SolidTerrainOverride("sand")==PrimaryOpenTerrain("sand") pair is
    /// one string, so it never had grass-open's Solid/Open case split (see the Tropical profile's own
    /// "RESOLVED GAP" doc comment and TerrainLabelCaseUnificationTests) --
    /// so it stays wired. Measured (seedBase 95000, 150 seeds each, Halls, all successes=150):
    /// "DragSkel_1x2(sand)" 68.7% (103/150), "Temple01_3x2(sand)" 11.3% (17/150), "Temple02_2x2(sand)"
    /// 40.7% (61/150), "Temple03_3x2(sand)" 11.3% (17/150), "Warzone_1x2(sand)" 68.7% (103/150),
    /// "Barracks_1x2(sand)" 100% (150/150), "Tower_1x2(sand)" 68.7% (103/150).
    /// </summary>
    [Test]
    public void TropicalSandOpenSetPieces_PlaceInIsolation()
    {
        var tilesetProfile = new BaseGameTilesetProfiles().BuildTilesetProfiles()[BaseGameTilesetProfiles.TropicalSand];
        var layoutProfile = new StandardLayoutProfiles().BuildLayoutProfiles()[StandardLayoutProfiles.Halls];
        var model = LoadTileset(tilesetProfile.TilesetResref);

        foreach (var (groupName, minShare) in new[]
                 {
                     ("DragSkel_1x2(sand)", 0.4),
                     ("Temple01_3x2(sand)", 0.05),
                     ("Temple02_2x2(sand)", 0.2),
                     ("Temple03_3x2(sand)", 0.05),
                     ("Warzone_1x2(sand)", 0.4),
                     ("Barracks_1x2(sand)", 0.5),
                     ("Tower_1x2(sand)", 0.4),
                 })
        {
            var (successes, hits) = MeasureIsolatedGroupHits(tilesetProfile, layoutProfile, model, groupName, maxPerArea: 5, seedBase: 95000, seedCount: 150);

            successes.Should().BeGreaterThan(140);
            hits.Should().BeGreaterOrEqualTo((int)(successes * minShare),
                $"'{groupName}' must place on a meaningful share of the {successes} successful seeds (got {hits})");
        }
    }

    /// <summary>
    /// Placement proof for BaseGameTilesetProfiles.TropicalWater's grass+water and all-Water
    /// (Solid-anchored) OpenSetPiece family. Measured (seedBase 95000, 150 seeds each, Halls, all
    /// successes=150): "ShipDocked01_2x2" 40.7% (61/150), "MerchantDocked01_3x2" 11.3% (17/150),
    /// "WeatheredDocked01_3x2" 11.3% (17/150), "MerchantFloating_3x1" 100% (150/150), "MerchantWeathered"
    /// 100% (150/150), "Lighthouse" 89.3% (134/150).
    /// </summary>
    [Test]
    public void TropicalWaterOpenSetPieces_PlaceInIsolation()
    {
        var tilesetProfile = new BaseGameTilesetProfiles().BuildTilesetProfiles()[BaseGameTilesetProfiles.TropicalWater];
        var layoutProfile = new StandardLayoutProfiles().BuildLayoutProfiles()[StandardLayoutProfiles.Halls];
        var model = LoadTileset(tilesetProfile.TilesetResref);

        foreach (var (groupName, minShare) in new[]
                 {
                     ("ShipDocked01_2x2", 0.2),
                     ("MerchantDocked01_3x2", 0.05),
                     ("WeatheredDocked01_3x2", 0.05),
                     ("MerchantFloating_3x1", 0.5),
                     ("MerchantWeathered", 0.5),
                     ("Lighthouse", 0.5),
                 })
        {
            var (successes, hits) = MeasureIsolatedGroupHits(tilesetProfile, layoutProfile, model, groupName, maxPerArea: 5, seedBase: 95000, seedCount: 150);

            successes.Should().BeGreaterThan(140);
            hits.Should().BeGreaterOrEqualTo((int)(successes * minShare),
                $"'{groupName}' must place on a meaningful share of the {successes} successful seeds (got {hits})");
        }
    }

    /// <summary>
    /// Placement proof for BaseGameTilesetProfiles.TropicalSandWater's sand+water OpenSetPiece family.
    /// Measured (seedBase 95000, 150 seeds each, Halls, all successes=150): "ShipDocked03_2x2" 40.7%
    /// (61/150), "MerchantDocked03_3x2" 11.3% (17/150), "WeatheredDocked03_3x2" 11.3% (17/150).
    /// "Shipwreck" (3x3) measures 0/150 on BOTH Halls and Complex -- see
    /// TropicalSandWaterShipwreck_StillDoesNotPlace_DocumentedCeiling below, the same "documented,
    /// not silently regressed" shape as CastleExteriorRuralLargeFootprintPieces_StillDoNotPlace_
    /// DocumentedCeilings above.
    /// </summary>
    [Test]
    public void TropicalSandWaterOpenSetPieces_PlaceInIsolation()
    {
        var tilesetProfile = new BaseGameTilesetProfiles().BuildTilesetProfiles()[BaseGameTilesetProfiles.TropicalSandWater];
        var layoutProfile = new StandardLayoutProfiles().BuildLayoutProfiles()[StandardLayoutProfiles.Halls];
        var model = LoadTileset(tilesetProfile.TilesetResref);

        foreach (var (groupName, minShare) in new[]
                 {
                     ("ShipDocked03_2x2", 0.2),
                     ("MerchantDocked03_3x2", 0.05),
                     ("WeatheredDocked03_3x2", 0.05),
                 })
        {
            var (successes, hits) = MeasureIsolatedGroupHits(tilesetProfile, layoutProfile, model, groupName, maxPerArea: 5, seedBase: 95000, seedCount: 150);

            successes.Should().BeGreaterThan(140);
            hits.Should().BeGreaterOrEqualTo((int)(successes * minShare),
                $"'{groupName}' must place on a meaningful share of the {successes} successful seeds (got {hits})");
        }
    }

    /// <summary>
    /// Documented placement ceiling for TropicalSandWater's "Shipwreck" (3x3, the largest footprint in
    /// this tileset family): measured 0/150 in isolation on BOTH Halls and Complex (ProbeTool) -- the
    /// same "needs a larger contiguous open interior than a 20x20 area's rooms ever produce at this
    /// size" shape CastleExteriorRuralLargeFootprintPieces_StillDoNotPlace_DocumentedCeilings documents
    /// for tno01's "FantasyTower 4x4"/"Tower3 m69 3x3". If it ever starts placing, room generation
    /// changed and this test (and its doc comment) should be revisited, not silently deleted.
    /// </summary>
    [Test]
    public void TropicalSandWaterShipwreck_StillDoesNotPlace_DocumentedCeiling()
    {
        var tilesetProfile = new BaseGameTilesetProfiles().BuildTilesetProfiles()[BaseGameTilesetProfiles.TropicalSandWater];
        var model = LoadTileset(tilesetProfile.TilesetResref);

        foreach (var layoutKey in new[] { StandardLayoutProfiles.Halls, StandardLayoutProfiles.Complex })
        {
            var layoutProfile = new StandardLayoutProfiles().BuildLayoutProfiles()[layoutKey];
            var (successes, hits) = MeasureIsolatedGroupHits(tilesetProfile, layoutProfile, model, "Shipwreck", maxPerArea: 5, seedBase: 95000, seedCount: 150);

            successes.Should().BeGreaterThan(140);
            hits.Should().Be(0,
                "'Shipwreck' has a documented placement ceiling at this size -- " +
                "if it ever starts placing, generation changed and this test (and its doc comment) should be revisited, not silently deleted");
        }
    }

    /// <summary>
    /// Placement proof for BaseGameTilesetProfiles.Tropical's "House01"/"House02"/"Mausoleum01"/
    /// "Mausoleum02" ExitGroups (base profile, all-grass, no crosser). Measured (seedBase 95000, 150
    /// seeds each, all successes=150): all four place 150/150 (100%) -- the same trivial-on-an-open-
    /// field-with-no-wall-competition result RuralGrassCastleDoorGroups_PlaceAsGroupExits' own precedent
    /// shows.
    /// </summary>
    [Test]
    public void TropicalExitGroups_PlaceAsGroupExits()
    {
        var tilesetProfile = new BaseGameTilesetProfiles().BuildTilesetProfiles()[BaseGameTilesetProfiles.Tropical];
        var layoutProfile = new StandardLayoutProfiles().BuildLayoutProfiles()[StandardLayoutProfiles.Halls];
        var model = LoadTileset(tilesetProfile.TilesetResref);

        foreach (var groupName in new[] { "House01", "House02", "Mausoleum01", "Mausoleum02" })
        {
            var (successes, hits) = MeasureIsolatedExitGroupHits(tilesetProfile, layoutProfile, model, groupName, seedBase: 95000, seedCount: 150);

            successes.Should().BeGreaterThan(140);
            hits.Should().BeGreaterOrEqualTo((int)(successes * 0.5),
                $"'{groupName}' must place as a GroupExit on a meaningful share of the {successes} successful seeds (got {hits})");
        }
    }

    // ---------------- ttu01 Underdark* placement proofs ----------------

    /// <summary>
    /// Placement proof for BaseGameTilesetProfiles.Underdark's OpenSetPiece family (ProbeTool
    /// "placeundk", seedBase 95000, 150 seeds each, Halls, isolation technique -- "successes" varies
    /// per group since isolating a different single SetPiece perturbs the RNG draw sequence, the same
    /// effect this file's other per-tileset proofs already show for pieces with differing successes
    /// counts). Measured: "Stairs - Down (2x2)"/"Stairs - Up (2x2)"/"Slave Trade Post (2x2)"/
    /// "Building - Illithid 1 (2x2)"/"Building - Drow (2x2)"/"Building - Illithid 2 (2x2)"/
    /// "Building - Svirfneblin 1 (2x2)"/"Rock Formation (2x2)"/"Temple - Drow (2x2)"/
    /// "Slave Huts (2x2)" (all-Floor, successes=135) each 23.7% (32/135); "Building - Svirfneblin 2
    /// (2x3)" (successes=135) 4.4% (6/135); "Door - Bridge, Water" (successes=135, the CorridorInsert
    /// shape) 44.4% (60/135); "Ramp - Up"/"Ramp - Down"/"Entrance - Beholder"/"Door - Rock" (the
    /// Floor/Rock diagonal-split shape, successes=65) each 72.3% (47/65); "Gates (2x3)" (successes=134,
    /// mixed Floor/Rock) 3.7% (5/134); "Entrance - Dungeon (1x2)" (successes=95, Floor/Rock) 40.0%
    /// (38/95); "Ship - Air, Docked (3x1)" (successes=135, pure Floor) 23.0% (31/135).
    /// </summary>
    [Test]
    public void UnderdarkOpenSetPieces_PlaceInIsolation()
    {
        var tilesetProfile = new BaseGameTilesetProfiles().BuildTilesetProfiles()[BaseGameTilesetProfiles.Underdark];
        var layoutProfile = new StandardLayoutProfiles().BuildLayoutProfiles()[StandardLayoutProfiles.Halls];
        var model = LoadTileset(tilesetProfile.TilesetResref);

        foreach (var (groupName, minSuccesses, minShare) in new[]
                 {
                     ("Stairs - Down (2x2)", 120, 0.15),
                     ("Stairs - Up (2x2)", 120, 0.15),
                     ("Door - Bridge, Water", 120, 0.3),
                     ("Slave Trade Post (2x2)", 120, 0.15),
                     ("Building - Illithid 1 (2x2)", 120, 0.15),
                     ("Building - Drow (2x2)", 120, 0.15),
                     ("Building - Illithid 2 (2x2)", 120, 0.15),
                     ("Building - Svirfneblin 1 (2x2)", 120, 0.15),
                     ("Building - Svirfneblin 2 (2x3)", 120, 0.02),
                     ("Rock Formation (2x2)", 120, 0.15),
                     ("Temple - Drow (2x2)", 120, 0.15),
                     ("Slave Huts (2x2)", 120, 0.15),
                     ("Ramp - Up", 50, 0.5),
                     ("Ramp - Down", 50, 0.5),
                     ("Entrance - Beholder", 50, 0.5),
                     ("Door - Rock", 50, 0.5),
                     ("Gates (2x3)", 120, 0.02),
                     ("Entrance - Dungeon (1x2)", 80, 0.3),
                     ("Ship - Air, Docked (3x1)", 120, 0.15),
                 })
        {
            var (successes, hits) = MeasureIsolatedGroupHits(tilesetProfile, layoutProfile, model, groupName, maxPerArea: 5, seedBase: 95000, seedCount: 150);

            successes.Should().BeGreaterThan(minSuccesses);
            hits.Should().BeGreaterOrEqualTo((int)(successes * minShare),
                $"'{groupName}' must place on a meaningful share of the {successes} successful seeds (got {hits})");
        }
    }

    /// <summary>
    /// Placement proof for BaseGameTilesetProfiles.Underdark's ExitGroup family (all 1x1, all-Floor,
    /// crosser-free doorways). Measured (ProbeTool "placeundk", seedBase 95000, 150 seeds, successes=135
    /// uniformly): all eight place identically at 34.8% (47/135).
    /// </summary>
    [Test]
    public void UnderdarkExitGroups_PlaceAsGroupExits()
    {
        var tilesetProfile = new BaseGameTilesetProfiles().BuildTilesetProfiles()[BaseGameTilesetProfiles.Underdark];
        var layoutProfile = new StandardLayoutProfiles().BuildLayoutProfiles()[StandardLayoutProfiles.Halls];
        var model = LoadTileset(tilesetProfile.TilesetResref);

        foreach (var groupName in new[]
                 {
                     "Building - Duergar", "Door - Dome", "Entrance - Catacombs", "Ruin - Cellar 1",
                     "Ruin - Cellar 2", "Ruin - House 4", "Tower - Square", "Tower - Round",
                 })
        {
            var (successes, hits) = MeasureIsolatedExitGroupHits(tilesetProfile, layoutProfile, model, groupName, seedBase: 95000, seedCount: 150);

            successes.Should().BeGreaterThan(120);
            hits.Should().BeGreaterOrEqualTo((int)(successes * 0.25),
                $"'{groupName}' must place as a GroupExit on a meaningful share of the {successes} successful seeds (got {hits})");
        }
    }

    /// <summary>
    /// Documented placement ceiling for Underdark's three 3x3 OpenSetPieces (the largest footprint in
    /// this tileset, all pure-Floor): measured 0/150 in isolation on BOTH Halls and Complex (ProbeTool
    /// "placeundk") -- the same "needs a larger contiguous open interior than a 20x20 area's rooms ever
    /// produce at this size" shape CastleExteriorRuralLargeFootprintPieces_StillDoNotPlace_
    /// DocumentedCeilings/TropicalSandWaterShipwreck_StillDoesNotPlace_DocumentedCeiling document. If
    /// any of these ever start placing, room generation changed and this test (and its doc comment)
    /// should be revisited, not silently deleted.
    /// </summary>
    [Test]
    public void UnderdarkLargeFootprintPieces_StillDoNotPlace_DocumentedCeiling()
    {
        var tilesetProfile = new BaseGameTilesetProfiles().BuildTilesetProfiles()[BaseGameTilesetProfiles.Underdark];
        var model = LoadTileset(tilesetProfile.TilesetResref);

        foreach (var groupName in new[] { "Tower - Drow (3x3)", "Illithid Grand Lair (3x3)", "Observation Dome (3x3)" })
        foreach (var layoutKey in new[] { StandardLayoutProfiles.Halls, StandardLayoutProfiles.Complex })
        {
            var layoutProfile = new StandardLayoutProfiles().BuildLayoutProfiles()[layoutKey];
            var (successes, hits) = MeasureIsolatedGroupHits(tilesetProfile, layoutProfile, model, groupName, maxPerArea: 5, seedBase: 95000, seedCount: 150);

            successes.Should().BeGreaterThan(120);
            hits.Should().Be(0,
                $"'{groupName}' has a documented placement ceiling at this size on {layoutKey} -- " +
                "if it ever starts placing, generation changed and this test (and its doc comment) should be revisited, not silently deleted");
        }
    }

    // ---------------- trs02 Early Winter 2 placement proofs ----------------

    /// <summary>
    /// Placement proof for BaseGameTilesetProfiles.EarlyWinter's pure-Grass OpenSetPiece/WallAlcove
    /// family and its two ExitGroups (ProbeTool "placeew", seedBase 95000, 150 seeds each, Halls, all
    /// successes=150). Measured: "DragonSkeleton"/"Field3" 62.7% (94/150); "Field1"/"Field2" 28.7%
    /// (43/150); "CabbagePatch"/"GoblinHut1" 100% (150/150, WallAlcove); "GoblinHut2"/"PenGate" 100%
    /// (150/150, ExitGroup).
    /// </summary>
    [Test]
    public void EarlyWinterOpenSetPieces_PlaceInIsolation()
    {
        var tilesetProfile = new BaseGameTilesetProfiles().BuildTilesetProfiles()[BaseGameTilesetProfiles.EarlyWinter];
        var layoutProfile = new StandardLayoutProfiles().BuildLayoutProfiles()[StandardLayoutProfiles.Halls];
        var model = LoadTileset(tilesetProfile.TilesetResref);

        foreach (var (groupName, minShare) in new[]
                 {
                     ("DragonSkeleton", 0.4),
                     ("Field1", 0.15),
                     ("Field2", 0.15),
                     ("Field3", 0.4),
                     ("CabbagePatch", 0.5),
                     ("GoblinHut1", 0.5),
                 })
        {
            var (successes, hits) = MeasureIsolatedGroupHits(tilesetProfile, layoutProfile, model, groupName, maxPerArea: 5, seedBase: 95000, seedCount: 150);

            successes.Should().BeGreaterThan(140);
            hits.Should().BeGreaterOrEqualTo((int)(successes * minShare),
                $"'{groupName}' must place on a meaningful share of the {successes} successful seeds (got {hits})");
        }

        foreach (var groupName in new[] { "GoblinHut2", "PenGate" })
        {
            var (successes, hits) = MeasureIsolatedExitGroupHits(tilesetProfile, layoutProfile, model, groupName, seedBase: 95000, seedCount: 150);

            successes.Should().BeGreaterThan(140);
            hits.Should().BeGreaterOrEqualTo((int)(successes * 0.5),
                $"'{groupName}' must place as a GroupExit on a meaningful share of the {successes} successful seeds (got {hits})");
        }
    }

    /// <summary>
    /// Placement proof for BaseGameTilesetProfiles.EarlyWinterMountain's pure Mountain+Grass
    /// ExitGroups -- the corner shape that IS this profile's own real Solid/Open pair (ProbeTool
    /// "placeew", seedBase 95000, 150 seeds, Halls, all successes=150). Measured: "MountainCave2" 97.3%
    /// (146/150); "MountainCave3"/"Mine1"/"Mine2" 100% (150/150).
    /// </summary>
    [Test]
    public void EarlyWinterMountainExitGroups_PlaceAsGroupExits()
    {
        var tilesetProfile = new BaseGameTilesetProfiles().BuildTilesetProfiles()[BaseGameTilesetProfiles.EarlyWinterMountain];
        var layoutProfile = new StandardLayoutProfiles().BuildLayoutProfiles()[StandardLayoutProfiles.Halls];
        var model = LoadTileset(tilesetProfile.TilesetResref);

        foreach (var groupName in new[] { "MountainCave2", "MountainCave3", "Mine1", "Mine2" })
        {
            var (successes, hits) = MeasureIsolatedExitGroupHits(tilesetProfile, layoutProfile, model, groupName, seedBase: 95000, seedCount: 150);

            successes.Should().BeGreaterThan(140);
            hits.Should().BeGreaterOrEqualTo((int)(successes * 0.5),
                $"'{groupName}' must place as a GroupExit on a meaningful share of the {successes} successful seeds (got {hits})");
        }
    }

    /// <summary>
    /// Documented placement ceiling for EarlyWinter's five Chasm-touching pieces (CliffCaveEntry/
    /// CliffPath2/CliffBottomCave1/CliffBottomCave2/CliffTopCave1): measured 0/150 in isolation
    /// (ProbeTool "placeew", Halls). Root cause verified directly in RoomsAndCorridorsLayout.Generate:
    /// SecondaryOpenTerrain districts only paint when CorridorMode is Tunnel, and this composition has
    /// NO Tunnel vocabulary at all (Complex downgrades to OpenLane, see TunnelVocabularyCheckTests'
    /// own trs02 entry), so Chasm never actually paints under any of this project's three supported
    /// layouts -- structurally reachable (real census credit via matchesSecondary) but never placeable
    /// today. If any of these ever start placing, district painting changed and this test (and its doc
    /// comment) should be revisited, not silently deleted.
    /// </summary>
    [Test]
    public void EarlyWinterChasmDistrictPieces_StillDoNotPlace_DocumentedCeiling()
    {
        var tilesetProfile = new BaseGameTilesetProfiles().BuildTilesetProfiles()[BaseGameTilesetProfiles.EarlyWinter];
        var layoutProfile = new StandardLayoutProfiles().BuildLayoutProfiles()[StandardLayoutProfiles.Halls];
        var model = LoadTileset(tilesetProfile.TilesetResref);

        foreach (var groupName in new[] { "CliffCaveEntry", "CliffPath2" })
        {
            var (successes, hits) = MeasureIsolatedGroupHits(tilesetProfile, layoutProfile, model, groupName, maxPerArea: 5, seedBase: 95000, seedCount: 150);

            successes.Should().BeGreaterThan(140);
            hits.Should().Be(0,
                $"'{groupName}' has a documented placement ceiling (SecondaryOpenTerrain never paints without Tunnel-mode CorridorMode) -- " +
                "if it ever starts placing, district painting changed and this test (and its doc comment) should be revisited, not silently deleted");
        }

        foreach (var groupName in new[] { "CliffBottomCave1", "CliffBottomCave2", "CliffTopCave1" })
        {
            var (successes, hits) = MeasureIsolatedExitGroupHits(tilesetProfile, layoutProfile, model, groupName, seedBase: 95000, seedCount: 150);

            successes.Should().BeGreaterThan(140);
            hits.Should().Be(0,
                $"'{groupName}' has a documented placement ceiling (SecondaryOpenTerrain never paints without Tunnel-mode CorridorMode) -- " +
                "if it ever starts placing, district painting changed and this test (and its doc comment) should be revisited, not silently deleted");
        }
    }

    /// <summary>
    /// Documented placement ceiling for EarlyWinterMountain's four third-terrain pieces
    /// (MountainCave1/CornerCave1/SeaCave1 mix in an unwired grass2/water corner that never appears in
    /// a grid painted only Grass/Mountain; InnerCornerCave1/MountainCave4 need a concave inner-corner
    /// boundary cell BSP rectangle room carving never produces): measured 0/150 in isolation (ProbeTool
    /// "placeew", Halls). See BaseGameTilesetProfiles.EarlyWinterMountain's own doc comment for the full
    /// per-group writeup. If any of these ever start placing, room generation changed and this test
    /// (and its doc comment) should be revisited, not silently deleted.
    /// </summary>
    [Test]
    public void EarlyWinterMountainThirdTerrainPieces_StillDoNotPlace_DocumentedCeiling()
    {
        var tilesetProfile = new BaseGameTilesetProfiles().BuildTilesetProfiles()[BaseGameTilesetProfiles.EarlyWinterMountain];
        var layoutProfile = new StandardLayoutProfiles().BuildLayoutProfiles()[StandardLayoutProfiles.Halls];
        var model = LoadTileset(tilesetProfile.TilesetResref);

        foreach (var groupName in new[] { "MountainCave1", "MountainCave4", "CornerCave1", "InnerCornerCave1", "InnerCornerCave3", "SeaCave1" })
        {
            var (successes, hits) = MeasureIsolatedExitGroupHits(tilesetProfile, layoutProfile, model, groupName, seedBase: 95000, seedCount: 150);

            successes.Should().BeGreaterThan(140);
            hits.Should().Be(0,
                $"'{groupName}' has a documented placement ceiling -- " +
                "if it ever starts placing, room generation changed and this test (and its doc comment) should be revisited, not silently deleted");
        }
    }

    // ---------------- trm02 Medieval Rural 2 placement proofs ----------------

    /// <summary>
    /// Placement proof for BaseGameTilesetProfiles.MedievalRural's pure-Grass OpenSetPiece/WallAlcove
    /// family and its ExitGroups (ProbeTool "placetrm", seedBase 95000, 150 seeds each, Halls, all
    /// successes=150). Measured: "DragonSkeleton"/"Field3" 62.7% (94/150) -- identical rates to trs02's
    /// own DragonSkeleton/Field3 (same tile geometry); "Field1"/"Field2" 28.7% (43/150, matching trs02's
    /// own Field1/Field2 too); "CabbagePatch"/"GoblinHut1"/"HobbitHome1"/"HobbitHome4"/"ElfHouse1"/
    /// "Farm2x2" 100% (150/150, WallAlcove); "GoblinHut2"/"PenGate"/"HobbitHome3"/"HobbitHome5"/
    /// "TnoHouse1"/"TnoHouse2"/"SmallFarm2"/"SmallFarm3"/"Windmill"/"FarmShed" 100% (150/150, ExitGroup).
    /// </summary>
    [Test]
    public void MedievalRuralOpenSetPieces_PlaceInIsolation()
    {
        var tilesetProfile = new BaseGameTilesetProfiles().BuildTilesetProfiles()[BaseGameTilesetProfiles.MedievalRural];
        var layoutProfile = new StandardLayoutProfiles().BuildLayoutProfiles()[StandardLayoutProfiles.Halls];
        var model = LoadTileset(tilesetProfile.TilesetResref);

        foreach (var (groupName, minShare) in new[]
                 {
                     ("DragonSkeleton", 0.4),
                     ("Field1", 0.15),
                     ("Field2", 0.15),
                     ("Field3", 0.4),
                     ("CabbagePatch", 0.5),
                     ("GoblinHut1", 0.5),
                     ("HobbitHome1", 0.5),
                     ("HobbitHome4", 0.5),
                     ("ElfHouse1", 0.5),
                     ("Farm2x2", 0.5),
                 })
        {
            var (successes, hits) = MeasureIsolatedGroupHits(tilesetProfile, layoutProfile, model, groupName, maxPerArea: 5, seedBase: 95000, seedCount: 150);

            successes.Should().BeGreaterThan(140);
            hits.Should().BeGreaterOrEqualTo((int)(successes * minShare),
                $"'{groupName}' must place on a meaningful share of the {successes} successful seeds (got {hits})");
        }

        foreach (var groupName in new[]
                 {
                     "GoblinHut2", "PenGate", "HobbitHome3", "HobbitHome5", "TnoHouse1", "TnoHouse2",
                     "SmallFarm2", "SmallFarm3", "Windmill", "FarmShed",
                 })
        {
            var (successes, hits) = MeasureIsolatedExitGroupHits(tilesetProfile, layoutProfile, model, groupName, seedBase: 95000, seedCount: 150);

            successes.Should().BeGreaterThan(140);
            hits.Should().BeGreaterOrEqualTo((int)(successes * 0.5),
                $"'{groupName}' must place as a GroupExit on a meaningful share of the {successes} successful seeds (got {hits})");
        }
    }

    /// <summary>
    /// Placement proof for BaseGameTilesetProfiles.MedievalRuralMountain's pure Mountain+Grass
    /// ExitGroups -- the corner shape that IS this profile's own real Solid/Open pair (ProbeTool
    /// "placetrm", seedBase 95000, 150 seeds, Halls, all successes=150). Measured: "MountainCave2" 97.3%
    /// (146/150); "MountainCave3"/"Mine1"/"Mine2" 100% (150/150) -- the exact same rates
    /// EarlyWinterMountain's own identically-shaped ExitGroup family measures.
    /// </summary>
    [Test]
    public void MedievalRuralMountainExitGroups_PlaceAsGroupExits()
    {
        var tilesetProfile = new BaseGameTilesetProfiles().BuildTilesetProfiles()[BaseGameTilesetProfiles.MedievalRuralMountain];
        var layoutProfile = new StandardLayoutProfiles().BuildLayoutProfiles()[StandardLayoutProfiles.Halls];
        var model = LoadTileset(tilesetProfile.TilesetResref);

        foreach (var groupName in new[] { "MountainCave2", "MountainCave3", "Mine1", "Mine2" })
        {
            var (successes, hits) = MeasureIsolatedExitGroupHits(tilesetProfile, layoutProfile, model, groupName, seedBase: 95000, seedCount: 150);

            successes.Should().BeGreaterThan(140);
            hits.Should().BeGreaterOrEqualTo((int)(successes * 0.5),
                $"'{groupName}' must place as a GroupExit on a meaningful share of the {successes} successful seeds (got {hits})");
        }
    }

    /// <summary>
    /// Documented placement ceiling for MedievalRural's Chasm-touching pieces (CliffCaveEntry/CliffPath2/
    /// CliffRockFormation/CliffBottomCave1/CliffBottomCave2/CliffTopCave1): measured 0/150 in isolation
    /// (ProbeTool "placetrm", Halls). Root cause identical to EarlyWinterChasmDistrictPieces_
    /// StillDoNotPlace_DocumentedCeiling: SecondaryOpenTerrain districts only paint when CorridorMode is
    /// Tunnel, and this composition has NO Tunnel vocabulary at all (Complex downgrades to OpenLane, see
    /// TunnelVocabularyCheckTests' own trm02 entry), so Chasm never actually paints under any of this
    /// project's three supported layouts -- structurally reachable (real census credit) but never
    /// placeable today. If any of these ever start placing, district painting changed and this test (and
    /// its doc comment) should be revisited, not silently deleted.
    /// </summary>
    [Test]
    public void MedievalRuralChasmDistrictPieces_StillDoNotPlace_DocumentedCeiling()
    {
        var tilesetProfile = new BaseGameTilesetProfiles().BuildTilesetProfiles()[BaseGameTilesetProfiles.MedievalRural];
        var layoutProfile = new StandardLayoutProfiles().BuildLayoutProfiles()[StandardLayoutProfiles.Halls];
        var model = LoadTileset(tilesetProfile.TilesetResref);

        foreach (var groupName in new[] { "CliffCaveEntry", "CliffPath2", "CliffRockFormation" })
        {
            var (successes, hits) = MeasureIsolatedGroupHits(tilesetProfile, layoutProfile, model, groupName, maxPerArea: 5, seedBase: 95000, seedCount: 150);

            successes.Should().BeGreaterThan(140);
            hits.Should().Be(0,
                $"'{groupName}' has a documented placement ceiling (SecondaryOpenTerrain never paints without Tunnel-mode CorridorMode) -- " +
                "if it ever starts placing, district painting changed and this test (and its doc comment) should be revisited, not silently deleted");
        }

        foreach (var groupName in new[] { "CliffBottomCave1", "CliffBottomCave2", "CliffTopCave1" })
        {
            var (successes, hits) = MeasureIsolatedExitGroupHits(tilesetProfile, layoutProfile, model, groupName, seedBase: 95000, seedCount: 150);

            successes.Should().BeGreaterThan(140);
            hits.Should().Be(0,
                $"'{groupName}' has a documented placement ceiling (SecondaryOpenTerrain never paints without Tunnel-mode CorridorMode) -- " +
                "if it ever starts placing, district painting changed and this test (and its doc comment) should be revisited, not silently deleted");
        }
    }

    /// <summary>
    /// Documented placement ceiling for MedievalRural's two off-vocab single-tile ExitGroups: "Lighthouse"
    /// (Grass+Water corners) and "SmallFarm1" (pure Grass2) are each ExitGroup-eligible regardless of
    /// vocabulary (IsExitGroupEligible is structural/terrain-agnostic) and are wired as real GroupExits,
    /// but Water/Grass2 are never painted anywhere on this profile's Grass-only grid (neither is
    /// PrimaryOpenTerrain/SecondaryOpenTerrain/AccentTerrain), so no real boundary cell matching either
    /// piece's own corner terrain ever exists for GroupExitPlanner's site search to attach to. Measured
    /// 0/150 in isolation (ProbeTool "placetrm", Halls). If either ever starts placing, terrain painting
    /// changed and this test (and its doc comment) should be revisited, not silently deleted.
    /// </summary>
    [Test]
    public void MedievalRuralWaterAndOffVocabSinglePieces_StillDoNotPlace_DocumentedCeiling()
    {
        var tilesetProfile = new BaseGameTilesetProfiles().BuildTilesetProfiles()[BaseGameTilesetProfiles.MedievalRural];
        var layoutProfile = new StandardLayoutProfiles().BuildLayoutProfiles()[StandardLayoutProfiles.Halls];
        var model = LoadTileset(tilesetProfile.TilesetResref);

        foreach (var groupName in new[] { "Lighthouse", "SmallFarm1" })
        {
            var (successes, hits) = MeasureIsolatedExitGroupHits(tilesetProfile, layoutProfile, model, groupName, seedBase: 95000, seedCount: 150);

            successes.Should().BeGreaterThan(140);
            hits.Should().Be(0,
                $"'{groupName}' has a documented placement ceiling (its own corner terrain is never painted on this grid) -- " +
                "if it ever starts placing, terrain painting changed and this test (and its doc comment) should be revisited, not silently deleted");
        }
    }

    /// <summary>
    /// Documented placement ceiling for MedievalRuralMountain's six third-terrain/concave-corner pieces
    /// (MountainCave1/MountainCave4/CornerCave1/InnerCornerCave3 mix in an unwired grass2 corner;
    /// SeaCave1 mixes in unwired water; InnerCornerCave1 needs a concave inner-corner boundary cell BSP
    /// rectangle room carving never produces, despite using only wired Mountain/Grass terrain): measured
    /// 0/150 in isolation (ProbeTool "placetrm", Halls) -- the exact same six-piece shape
    /// EarlyWinterMountainThirdTerrainPieces_StillDoNotPlace_DocumentedCeiling documents for trs02. See
    /// BaseGameTilesetProfiles.MedievalRuralMountain's own doc comment for the full per-group writeup. If
    /// any of these ever start placing, room generation changed and this test (and its doc comment)
    /// should be revisited, not silently deleted.
    /// </summary>
    [Test]
    public void MedievalRuralMountainThirdTerrainPieces_StillDoNotPlace_DocumentedCeiling()
    {
        var tilesetProfile = new BaseGameTilesetProfiles().BuildTilesetProfiles()[BaseGameTilesetProfiles.MedievalRuralMountain];
        var layoutProfile = new StandardLayoutProfiles().BuildLayoutProfiles()[StandardLayoutProfiles.Halls];
        var model = LoadTileset(tilesetProfile.TilesetResref);

        foreach (var groupName in new[] { "MountainCave1", "MountainCave4", "CornerCave1", "InnerCornerCave1", "InnerCornerCave3", "SeaCave1" })
        {
            var (successes, hits) = MeasureIsolatedExitGroupHits(tilesetProfile, layoutProfile, model, groupName, seedBase: 95000, seedCount: 150);

            successes.Should().BeGreaterThan(140);
            hits.Should().Be(0,
                $"'{groupName}' has a documented placement ceiling -- " +
                "if it ever starts placing, room generation changed and this test (and its doc comment) should be revisited, not silently deleted");
        }
    }

    // ---------------- tss13 Sea Ships placement proofs ----------------

    /// <summary>
    /// Placement proof for BaseGameTilesetProfiles.SeaShips' own all-Castle SetPiece family (see that
    /// profile's own doc comment): "Boat 4" and "Lifeboat 1" (no door slot -- classify OpenSetPiece
    /// under the open-field corner-match rule, the same shape RuralGrass's own family uses) and "Boat
    /// 1" (one door-slot member -- classifies WallAlcove; allCornersSolid is trivially true the instant
    /// SolidTerrainOverride(t) == PrimaryOpenTerrain(t), and IsWallAlcoveSiteValid's open-terrain touch
    /// tolerance is satisfied by literally any neighbor cell since there is no separate wall mass to
    /// fail against here, unlike vmr01's own more constrained real wall-ring boundary). Measured
    /// (seedBase 95000, 150 seeds each, successes=150): "Boat 4" 67.3% (101/150), "Lifeboat 1" 89.3%
    /// (134/150), "Boat 1" 100% (150/150 -- the WallAlcove kind's near-certain site validity, exactly
    /// as predicted -- no comparable placement risk to Tropical's own measured 60.3% Organic-specific
    /// WallAlcove disconnection gap is present here). Thresholds set well under the lowest measured
    /// rate for safety margin. Halls is used below, matching this profile's own default composition;
    /// Complex/Organic are covered by the full pipeline sweep in OnboardedTilesetPipelineTests instead.
    /// </summary>
    [Test]
    public void SeaShipsOpenFieldSetPieces_PlaceInIsolation()
    {
        var tilesetProfile = new BaseGameTilesetProfiles().BuildTilesetProfiles()[BaseGameTilesetProfiles.SeaShips];
        var layoutProfile = new StandardLayoutProfiles().BuildLayoutProfiles()[StandardLayoutProfiles.Halls];
        var model = LoadTileset(tilesetProfile.TilesetResref);

        foreach (var name in new[] { "Boat 4", "Lifeboat 1", "Boat 1" })
        {
            var (successes, hits) = MeasureIsolatedGroupHits(tilesetProfile, layoutProfile, model, name, maxPerArea: 5, seedBase: 95000, seedCount: 150);

            successes.Should().BeGreaterThan(140);
            hits.Should().BeGreaterOrEqualTo((int)(successes * 0.3),
                $"'{name}' must place on a meaningful share of the {successes} successful seeds (got {hits})");
        }
    }


    // ---------------- tcm02 Medieval City 2 placement proofs ----------------

    /// <summary>
    /// Placement proof for BaseGameTilesetProfiles.MedievalCity's Water/Cobble OpenSetPiece/WallAlcove
    /// family (ProbeTool "place", seedBase 95000, 150 seeds each, Halls, all successes=150). Measured:
    /// "House1_2x2" 40.7% (61/150); "Docks_City" 67.3% (101/150); "Jetty" 89.3% (134/150);
    /// "Ship_floating_1" 100% (150/150, WallAlcove); "Ship_3x1_Docked" 11.3% (17/150).
    /// </summary>
    [Test]
    public void MedievalCityOpenSetPieces_PlaceAcrossMeaningfulShareOfSeeds()
    {
        var tilesetProfile = new BaseGameTilesetProfiles().BuildTilesetProfiles()[BaseGameTilesetProfiles.MedievalCity];
        var layoutProfile = new StandardLayoutProfiles().BuildLayoutProfiles()[StandardLayoutProfiles.Halls];
        var model = LoadTileset(tilesetProfile.TilesetResref);

        foreach (var (groupName, minShare) in new[]
                 {
                     ("House1_2x2", 0.3),
                     ("Docks_City", 0.5),
                     ("Jetty", 0.7),
                     ("Ship_floating_1", 0.9),
                     ("Ship_3x1_Docked", 0.05),
                 })
        {
            var (successes, hits) = MeasureIsolatedGroupHits(tilesetProfile, layoutProfile, model, groupName, maxPerArea: 1, seedBase: 95000, seedCount: 150);

            successes.Should().BeGreaterThan(140);
            hits.Should().BeGreaterOrEqualTo((int)(successes * minShare),
                $"'{groupName}' must place on a meaningful share of the {successes} successful seeds (got {hits})");
        }
    }

    /// <summary>
    /// Placement proof for BaseGameTilesetProfiles.MedievalCity's pure-Cobble/Cobble+Water ExitGroups
    /// (ProbeTool "place", seedBase 95000, 150 seeds each, Halls, all successes=150). Measured:
    /// "House1_1x1"/"House4_1x1"/"House8"/"Watertower" 100% (150/150); "SewerEntrance03"/
    /// "SewerEntrance04" 100% (150/150).
    /// </summary>
    [Test]
    public void MedievalCityExitGroups_PlaceAsGroupExits()
    {
        var tilesetProfile = new BaseGameTilesetProfiles().BuildTilesetProfiles()[BaseGameTilesetProfiles.MedievalCity];
        var layoutProfile = new StandardLayoutProfiles().BuildLayoutProfiles()[StandardLayoutProfiles.Halls];
        var model = LoadTileset(tilesetProfile.TilesetResref);

        foreach (var groupName in new[] { "House1_1x1", "House4_1x1", "House8", "Watertower", "SewerEntrance03", "SewerEntrance04" })
        {
            var (successes, hits) = MeasureIsolatedExitGroupHits(tilesetProfile, layoutProfile, model, groupName, seedBase: 95000, seedCount: 150);

            successes.Should().BeGreaterThan(140);
            hits.Should().BeGreaterOrEqualTo((int)(successes * 0.5),
                $"'{groupName}' must place as a GroupExit on a meaningful share of the {successes} successful seeds (got {hits})");
        }
    }

    /// <summary>
    /// Documented placement ceiling for MedievalCity's three 3x3+/4x2 pieces ("DockedShip_City" 4x2,
    /// "Arena" 3x3, "Temple3x3" 3x3): measured 0/150 in isolation under BOTH Halls and Complex (ProbeTool
    /// "place"). The same room-size ceiling as this project's other oversized-set-piece families (a 3x3
    /// footprint + 1-cell margin + one spare relocation tile needs a room strictly larger than 5x5
    /// tiles -- corner size 7+ -- which neither Halls (corner ceiling 6) nor Complex (corner ceiling 5)
    /// ever produces at this project's 20x20 tuning baseline). If any of these ever start placing, room
    /// size scaling changed and this test (and its doc comment) should be revisited, not silently
    /// deleted.
    /// </summary>
    [Test]
    public void MedievalCityLargeRoomPieces_StillDoNotPlace_DocumentedCeiling()
    {
        var tilesetProfile = new BaseGameTilesetProfiles().BuildTilesetProfiles()[BaseGameTilesetProfiles.MedievalCity];
        var layoutProfiles = new StandardLayoutProfiles().BuildLayoutProfiles();
        var model = LoadTileset(tilesetProfile.TilesetResref);

        foreach (var layoutKey in new[] { StandardLayoutProfiles.Halls, StandardLayoutProfiles.Complex })
        foreach (var groupName in new[] { "DockedShip_City", "Arena", "Temple3x3" })
        {
            var (successes, hits) = MeasureIsolatedGroupHits(tilesetProfile, layoutProfiles[layoutKey], model, groupName, maxPerArea: 1, seedBase: 95000, seedCount: 150);

            successes.Should().BeGreaterThan(140);
            hits.Should().Be(0,
                $"'{groupName}' on '{layoutKey}' has a documented room-size placement ceiling -- " +
                "if it ever starts placing, room size scaling changed and this test (and its doc comment) should be revisited, not silently deleted");
        }
    }

    /// <summary>
    /// Documented placement ceiling for EVERY MedievalCity ExitGroup whose corners mix a terrain the
    /// base profile's Water/Cobble composition never paints (each classifies via IsExitGroupEligible's
    /// terrain-agnostic rule -- real census credit -- but GroupExitPlanner's exact corner-match
    /// requirement can never find a real site for a corner terrain that never appears in the grid).
    /// Measured 0/150 in isolation for ALL 21 groups (ProbeTool "place", Halls, seedBase 95000, all
    /// successes=150), split by unpainted terrain family: Building+Cobble ("BuildingBad1"/"Shop1"/
    /// "Shop2"/"Bakery"/"Museum"/"PatriciansHouse"/"Smithy"/"StairHouse"/"CornerShop1"/"CornerShop2"/
    /// "CornerPub"/"BurntHouse1"/"BurntHouse2"/"CornerBTower1"/"CornerBTower2a"), Castle+Grass
    /// ("CastleSmallDoor2"/"CastleHugeGateGrass"), Chasm+Grass ("CliffBottomCave1"/"CliffBottomCave2"/
    /// "CliffTopCave1"), and Water+Grass ("Lighthouse"). By contrast every pure-Cobble/Cobble+Water
    /// ExitGroup measured 100% (see MedievalCityExitGroups_PlaceAsGroupExits, plus House2_1x1/
    /// House3_1x1/House5_1x1/House9/House10, each independently measured 150/150). The five
    /// Castle+COBBLE groups this same gap used to apply to (CastleSmallDoor/CastleHugeGate/
    /// CastleTowerGate1-2/PrisonTower) were moved OFF this profile onto MedievalCityCastle instead (see
    /// that profile's own doc comment) precisely because Castle can be composed as a real Solid
    /// elsewhere -- the same "recompose onto a variant where the terrain is real" fix
    /// ForestGoodCastle/RuralGrassGoodCastle already document. Building can NOT get the same treatment
    /// (it fails every 16-combo pairing as Solid or Open -- see BaseGameTilesetProfiles.MedievalCity),
    /// and CliffBottomCave1-2/CliffTopCave1 pair Chasm with GRASS, not the Cliffs variant's own painted
    /// boundary shape (they need a Chasm/Grass room BOUNDARY, which the Cliffs variant does produce --
    /// they are wired there too and measured separately). All stay wired per this project's "keep it
    /// wired, document the ceiling" convention (EarlyWinter's own Chasm-district precedent). If any
    /// ever starts placing, terrain painting changed and this test (and its doc comment) should be
    /// revisited, not silently deleted.
    /// </summary>
    [Test]
    public void MedievalCityUnpaintedTerrainExitGroups_StillDoNotPlace_DocumentedCeiling()
    {
        var tilesetProfile = new BaseGameTilesetProfiles().BuildTilesetProfiles()[BaseGameTilesetProfiles.MedievalCity];
        var layoutProfile = new StandardLayoutProfiles().BuildLayoutProfiles()[StandardLayoutProfiles.Halls];
        var model = LoadTileset(tilesetProfile.TilesetResref);

        foreach (var groupName in new[]
                 {
                     "BuildingBad1", "Shop1", "Shop2", "Bakery", "Museum", "PatriciansHouse", "Smithy",
                     "StairHouse", "CornerShop1", "CornerShop2", "CornerPub", "BurntHouse1", "BurntHouse2",
                     "CornerBTower1", "CornerBTower2a",
                     "CastleSmallDoor2", "CastleHugeGateGrass",
                     "CliffBottomCave1", "CliffBottomCave2", "CliffTopCave1",
                     "Lighthouse",
                 })
        {
            var (successes, hits) = MeasureIsolatedExitGroupHits(tilesetProfile, layoutProfile, model, groupName, seedBase: 95000, seedCount: 150);

            successes.Should().BeGreaterThan(140);
            hits.Should().Be(0,
                $"'{groupName}' has a documented placement ceiling (its own corner terrain never paints under this composition) -- " +
                "if it ever starts placing, terrain painting changed and this test (and its doc comment) should be revisited, not silently deleted");
        }
    }

    /// <summary>
    /// Placement proof for BaseGameTilesetProfiles.MedievalCityCastle's Castle+Cobble door family
    /// (ProbeTool "place", seedBase 95000, 150 seeds, Halls, all successes=150): every one of the five
    /// groups now places at a real, measured rate now that Castle is composed as this variant's own
    /// Solid terrain (the identical fix ForestGoodCastle/RuralGrassGoodCastle already document).
    /// Measured: "CastleSmallDoor"/"CastleHugeGate"/"CastleTowerGate1"/"CastleTowerGate2" 100%
    /// (150/150); "PrisonTower" 97.3% (146/150).
    /// </summary>
    [Test]
    public void MedievalCityCastleDoorGroups_PlaceAsGroupExits()
    {
        var tilesetProfile = new BaseGameTilesetProfiles().BuildTilesetProfiles()[BaseGameTilesetProfiles.MedievalCityCastle];
        var layoutProfile = new StandardLayoutProfiles().BuildLayoutProfiles()[StandardLayoutProfiles.Halls];
        var model = LoadTileset(tilesetProfile.TilesetResref);

        foreach (var groupName in new[] { "CastleSmallDoor", "CastleHugeGate", "CastleTowerGate1", "CastleTowerGate2", "PrisonTower" })
        {
            var (successes, hits) = MeasureIsolatedExitGroupHits(tilesetProfile, layoutProfile, model, groupName, seedBase: 95000, seedCount: 150);

            successes.Should().BeGreaterThan(140);
            hits.Should().BeGreaterOrEqualTo((int)(successes * 0.5),
                $"'{groupName}' must place as a GroupExit on a meaningful share of the {successes} successful seeds (got {hits})");
        }
    }

    /// <summary>
    /// Placement proof for BaseGameTilesetProfiles.MedievalCityCliffs' Chasm+Grass OpenSetPiece family
    /// and its ChasmBridgeWB1 CorridorStub (ProbeTool "place", seedBase 95000, 150 seeds, all
    /// successes=150). Measured: "CliffCaveEntry" 68.7% (103/150, Halls, OpenSetPiece); "ChasmBridgeWB1"
    /// 0% under Halls (OpenLane carves no wall mass to stub off of) but 96.7% (145/150) under Complex
    /// (Tunnel mode) -- the expected CorridorStub shape, only ever placeable in Tunnel-mode corridors.
    /// </summary>
    [Test]
    public void MedievalCityCliffsOpenSetPieces_PlaceAcrossMeaningfulShareOfSeeds()
    {
        var tilesetProfile = new BaseGameTilesetProfiles().BuildTilesetProfiles()[BaseGameTilesetProfiles.MedievalCityCliffs];
        var layoutProfiles = new StandardLayoutProfiles().BuildLayoutProfiles();
        var model = LoadTileset(tilesetProfile.TilesetResref);

        {
            var (successes, hits) = MeasureIsolatedGroupHits(tilesetProfile, layoutProfiles[StandardLayoutProfiles.Halls], model, "CliffCaveEntry", maxPerArea: 1, seedBase: 95000, seedCount: 150);
            successes.Should().BeGreaterThan(140);
            hits.Should().BeGreaterOrEqualTo((int)(successes * 0.4),
                $"'CliffCaveEntry' must place on a meaningful share of the {successes} successful Halls seeds (got {hits})");
        }
        {
            var (successes, hits) = MeasureIsolatedGroupHits(tilesetProfile, layoutProfiles[StandardLayoutProfiles.Complex], model, "ChasmBridgeWB1", maxPerArea: 1, seedBase: 95000, seedCount: 150);
            successes.Should().BeGreaterThan(140);
            hits.Should().BeGreaterOrEqualTo((int)(successes * 0.8),
                $"'ChasmBridgeWB1' must place on a meaningful share of the {successes} successful Complex/Tunnel seeds (got {hits})");
        }
    }

    /// <summary>
    /// Placement proof for MedievalCityCliffs' Chasm+Grass cave-door ExitGroups (the same three groups
    /// the base profile documents a 0% ceiling for -- see
    /// MedievalCityUnpaintedTerrainExitGroups_StillDoNotPlace_DocumentedCeiling): on THIS variant the
    /// Chasm/Grass pair is the composition's own Solid/Open boundary, so GroupExitPlanner finds real
    /// sites. Measured (ProbeTool "place", seedBase 95000, 150 seeds, Halls, all successes=150):
    /// "CliffBottomCave1"/"CliffBottomCave2"/"CliffTopCave1" all 100% (150/150).
    /// </summary>
    [Test]
    public void MedievalCityCliffsCaveDoorGroups_PlaceAsGroupExits()
    {
        var tilesetProfile = new BaseGameTilesetProfiles().BuildTilesetProfiles()[BaseGameTilesetProfiles.MedievalCityCliffs];
        var layoutProfile = new StandardLayoutProfiles().BuildLayoutProfiles()[StandardLayoutProfiles.Halls];
        var model = LoadTileset(tilesetProfile.TilesetResref);

        foreach (var groupName in new[] { "CliffBottomCave1", "CliffBottomCave2", "CliffTopCave1" })
        {
            var (successes, hits) = MeasureIsolatedExitGroupHits(tilesetProfile, layoutProfile, model, groupName, seedBase: 95000, seedCount: 150);

            successes.Should().BeGreaterThan(140);
            hits.Should().BeGreaterOrEqualTo((int)(successes * 0.5),
                $"'{groupName}' must place as a GroupExit on a meaningful share of the {successes} successful seeds (got {hits})");
        }
    }

    /// <summary>
    /// Placement proof for MedievalCityCliffs' MaxReliefRegions(2) declaration: "HillCave1" (a raised,
    /// SetPieceReliefPiece-classified 1x1 group) measured 0/150 under BOTH Halls and Complex before this
    /// cap was declared (LayoutReliefPainter never paints without a composition actually requesting
    /// ReliefRegions, and DungeonComposition.BuildLayoutParameters clamps every request to the tileset's
    /// own cap). With MaxReliefRegions(2) declared, Complex (which requests relief regions -- Halls does
    /// not) now places it at a real, measured rate: 0% (Halls, unchanged) but 74.7% (112/150, Complex).
    /// </summary>
    [Test]
    public void MedievalCityCliffsHillCave_PlacesOnceReliefIsRequested()
    {
        var tilesetProfile = new BaseGameTilesetProfiles().BuildTilesetProfiles()[BaseGameTilesetProfiles.MedievalCityCliffs];
        var layoutProfile = new StandardLayoutProfiles().BuildLayoutProfiles()[StandardLayoutProfiles.Complex];
        var model = LoadTileset(tilesetProfile.TilesetResref);

        var (successes, hits) = MeasureIsolatedGroupHits(tilesetProfile, layoutProfile, model, "HillCave1", maxPerArea: 1, seedBase: 95000, seedCount: 150);

        successes.Should().BeGreaterThan(140);
        hits.Should().BeGreaterOrEqualTo((int)(successes * 0.5),
            $"'HillCave1' must place on a meaningful share of the {successes} successful Complex seeds now that MaxReliefRegions is declared (got {hits})");
    }

    /// <summary>
    /// Documented placement ceiling for MedievalCityCliffs' bare-Solid multi-tile fillers
    /// ("CliffRockFormation", all-Chasm, no door/crosser) and its 3x3 room-size-ceiling piece
    /// ("CliffPath2"): measured 0/150 in isolation under BOTH Halls and Complex (ProbeTool "place").
    /// "CliffRockFormation" classifies via matchesPrimary's trivial "every corner already equals Solid"
    /// allowance but LayoutGroupStamper's real OpenSetPiece site search needs a genuine Open-terrain
    /// anchor to relocate the room center onto, which an all-Solid footprint never offers -- the same
    /// bare-Solid gap "Small_Cog"/tcn01's own "[City] Boat" family documents. "CliffPath2" is a 3x3
    /// piece hitting the identical room-size ceiling as MedievalCity's own Arena/Temple3x3. If either
    /// ever starts placing, the relevant mechanism changed and this test (and its doc comment) should be
    /// revisited, not silently deleted.
    /// </summary>
    [Test]
    public void MedievalCityCliffsBareCornerPieces_StillDoNotPlace_DocumentedCeiling()
    {
        var tilesetProfile = new BaseGameTilesetProfiles().BuildTilesetProfiles()[BaseGameTilesetProfiles.MedievalCityCliffs];
        var layoutProfiles = new StandardLayoutProfiles().BuildLayoutProfiles();
        var model = LoadTileset(tilesetProfile.TilesetResref);

        foreach (var layoutKey in new[] { StandardLayoutProfiles.Halls, StandardLayoutProfiles.Complex })
        foreach (var groupName in new[] { "CliffRockFormation", "CliffPath2" })
        {
            var (successes, hits) = MeasureIsolatedGroupHits(tilesetProfile, layoutProfiles[layoutKey], model, groupName, maxPerArea: 1, seedBase: 95000, seedCount: 150);

            successes.Should().BeGreaterThan(140);
            hits.Should().Be(0,
                $"'{groupName}' on '{layoutKey}' has a documented placement ceiling -- " +
                "if it ever starts placing, the relevant mechanism changed and this test (and its doc comment) should be revisited, not silently deleted");
        }
    }
}
