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
        string groupName, int maxPerArea, int seedBase, int seedCount, int seedStride = 13)
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
            var result = LayoutSolver.Solve(composition.BuildLayoutParameters(), model, Size, Size, seed, tilesetProfile.PrimaryOpenTerrain, retryCount: 1);
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
}
