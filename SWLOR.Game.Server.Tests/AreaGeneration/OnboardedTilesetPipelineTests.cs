using System;
using System.Collections.Generic;
using System.Linq;
using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Feature.DungeonDefinition;
using SWLOR.Game.Server.Service.AreaGenerationService;

namespace SWLOR.Game.Server.Tests.AreaGeneration;

/// <summary>
/// Acceptance gate for every base-game tileset profile onboarded so far via BaseGameTilesetProfiles:
/// the pilot wave (Crypt/tdc01, Dungeon/tde01, City Interior/tin01) plus Wave-2 (Barrows/tbw01, Mines
/// and Caverns/tdm01, Ruins/tdr01, Castle Interior/tic01, Castle Interior 2/tni02, Drow Interior/tid01,
/// Illithid Interior/tii01, City Interior 2/tni01, Steamworks/tsw01, Fort Interior/twc03). Runs the
/// SAME production pipeline SWLOR.ProcgenReview and SWLOR.ContentBuilder use
/// (DungeonComposition.BuildLayoutParameters + LayoutSolver.Solve's seed-derived retry loop) across
/// many seeds for each tileset paired with Complex/Tunnel, Halls/OpenLane, and Organic layouts,
/// asserting 100% generation+resolution success and per-tile edge self-consistency, mirroring
/// TunnelCorridorTests' AssertEdgeAgreement pattern.
///
/// (Formerly PilotFullPipelineTests, covering only the three pilots -- renamed here since Wave-2
/// broadened this to every onboarded base-game tileset. Test method names are unchanged so existing
/// filters/CI references keep working.)
/// </summary>
public class OnboardedTilesetPipelineTests
{
    private static TilesetModel LoadTileset(string tilesetResref) => TilesetTestSource.LoadTileset(tilesetResref);

    private static readonly Dictionary<string, DungeonTilesetProfile> TilesetProfiles =
        new BaseGameTilesetProfiles().BuildTilesetProfiles();

    private static readonly Dictionary<string, DungeonLayoutProfile> LayoutProfiles =
        new StandardLayoutProfiles().BuildLayoutProfiles();

    /// <summary>Every onboarded base-game tileset profile key, pilot wave plus Wave-2.</summary>
    public static IEnumerable<string> OnboardedTilesetKeys => new[]
    {
        BaseGameTilesetProfiles.Crypt,
        BaseGameTilesetProfiles.Dungeon,
        BaseGameTilesetProfiles.CityInterior,
        BaseGameTilesetProfiles.Barrows,
        BaseGameTilesetProfiles.MinesAndCaverns,
        BaseGameTilesetProfiles.Ruins,
        BaseGameTilesetProfiles.CastleInterior,
        BaseGameTilesetProfiles.CastleInterior2,
        BaseGameTilesetProfiles.DrowInterior,
        BaseGameTilesetProfiles.IllithidInterior,
        BaseGameTilesetProfiles.CityInterior2,
        BaseGameTilesetProfiles.Steamworks,
        BaseGameTilesetProfiles.FortInterior,
        // Palette-variant profiles (same TilesetResref as an existing entry above, different terrain
        // composition -- see DungeonTilesetProfile.IsPaletteVariant and the tile-coverage census's
        // multi-profile reachability rule in TileCoverageCensusTests). These get the SAME full-pipeline
        // gate as any other onboarded profile: nothing about being a palette variant relaxes generation
        // correctness, it only exempts the profile from SWLOR.ProcgenReview's --matrix cross-product.
        BaseGameTilesetProfiles.CryptGrey,
        BaseGameTilesetProfiles.CryptDwarven,
        BaseGameTilesetProfiles.MinesAndCavernsDesert,
        BaseGameTilesetProfiles.MinesAndCavernsOrganic,
        BaseGameTilesetProfiles.RuinsPlaza,
    };

    // Every onboarded tileset lacks the Alley crosser vocabulary EXCEPT Ruins (tdr01, which has a
    // verified Alley crosser -- see BaseGameTilesetProfiles.Ruins) -- Streets layout pairing is still
    // out of scope for this wave's assignment (Complex/Halls/Organic only) and left for a future wave.
    // Complex (Tunnel mode), Halls (OpenLane), and Organic are the combos both onboarding waves target
    // as "where coverage allows".
    //
    // Barrows (tbw01) / Complex used to downgrade to OpenLane here: tbw01 has NO canonical "Doorway"
    // crosser at all, only "corridor"/"door_barrow"/"door_corridor", and every tile carrying any of
    // those three also carries a door slot -- TileResolver's crosser+door admission gate used to
    // hardcode literal "Doorway"/"Bridge" and silently excluded all of them from candidate lookup, so
    // TunnelVocabularyCheck.SupportsTunnels always read false for this tileset no matter what body/port
    // pair was probed. BaseGameTilesetProfiles.Barrows now declares TunnelCrossers("corridor",
    // "door_corridor") + DoorSlotCrossers("door_corridor"), which closes that gate -- Barrows/Complex
    // now genuinely carves wall-embedded Tunnel-mode corridors under its own renamed vocabulary instead
    // of downgrading; see CustomCrosserProfile_ComplexActuallyCarvesTheDeclaredBodyCrosser below (which
    // now covers Barrows alongside the Grey/Desert/Organic families) rather than a dedicated downgrade
    // test. "door_barrow" (SideChamber1's 1x1 group and its ungrouped boundary partner) stays
    // unreachable: MacroLayoutParameters only carries one Tunnel port crosser slot per composition
    // (already claimed by "door_corridor"), and no carver ever writes a "door_barrow" edge for
    // LayoutGroupStamper's CorridorStub site search to attach to -- see
    // BaseGameTilesetProfiles.Barrows' own doc comment and TileCoverageCensusTests.PilotExpectedExemptions.
    //
    // Ruins (tdr01) / Organic used to be excluded here too: Organic's OrganicCave style carves its
    // single accent channel crossing (AccentChannels = 1, see StandardLayoutProfiles.Organic) directly
    // through OPEN floor space, which needs a crosser-free tile blending Floor and Chasm corners --
    // verified zero such tiles exist in tdr01 (see BaseGameTilesetProfiles.Ruins' ChannelTerrain
    // comment). Complex/Halls never actually hit this gap in practice -- their room-and-corridor open
    // space is thin/fragmented enough that LayoutAccentChannelCarver's own ValidateBand geometry check
    // fails on every random attempt, so no channel is ever placed there (a silent no-op both before and
    // after this fix) -- but Organic's blobbier, larger open regions let ValidateBand succeed often
    // enough that the carver used to commit a band no physical tile could ever resolve. it now runs a
    // whole-tileset capability probe (CanCarve, using TileResolver.HasCandidate against the exact
    // bank/span shapes a channel needs) before ever attempting to carve, and skips channel carving
    // entirely (falls through with zero channels, generation proceeds normally) when the tileset can't
    // resolve them -- included below like any other pairing.
    //
    // Illithid Interior (tii01) / Complex used to be excluded here: it fails intermittently on a
    // Corridor+Corridor+Doorway three-way junction (an all-solid-corner adapter tile joining a corridor
    // bend to a room doorway) that this small, 10-group tileset's tile inventory doesn't cover for
    // every junction orientation. The wave-2 onboarding note that verified "3/50 seeds" (~6%) failing
    // single-attempt, implying the pipeline's 6-attempt seed-derived retry (LayoutSolver.DefaultRetryCount)
    // would make the overall failure rate negligible, did NOT hold up under a wider re-verification: a
    // direct probe (single-attempt retryCount:1 across 200 seeds, 9000-9199) measured 58.5% single-seed
    // failures, and even the full default 6-attempt retry only reached 98.5% (197/200) -- a second probe
    // over this test's own seed range (6000-6049, 50 seeds) measured 66% single-attempt failures and
    // 94% (47/50) with retry, still short of the 100% this pipeline gate requires. This was a genuine
    // tile-inventory gap that a bare Doorway/Corridor crosser-NAME presence check couldn't catch (Illithid
    // has both crossers -- it was simply missing one specific junction tile shape).
    // MacroLayoutGenerator now checks TunnelVocabularyCheck.SupportsTunnels (the exact tile-SHAPE
    // inventory the tunnel carver can emit, not just crosser names) before dispatch and downgrades
    // CorridorMode from Tunnel to OpenLane whenever a tileset can't resolve every shape -- Illithid fails
    // that check purely because of the missing T-with-port junction shape, so Complex now downgrades to
    // OpenLane for this tileset instead of being excluded outright. See
    // IllithidComplexDowngradesToOpenLaneWithNoTunnelCrossers below, which
    // locks in that the downgrade actually took effect.
    public static IEnumerable<object[]> OnboardedLayoutCases()
    {
        foreach (var tilesetKey in OnboardedTilesetKeys)
        foreach (var layoutKey in new[] { StandardLayoutProfiles.Complex, StandardLayoutProfiles.Halls, StandardLayoutProfiles.Organic })
        {
            yield return new object[] { tilesetKey, layoutKey };
        }
    }

    /// <summary>
    /// Locks in the pathnode-opening-width audit result (PathNodeOpeningWidthAudit.cs) against the
    /// REAL SWLOR_Haks-resolved tileset data for each onboarded tileset: BaseGameTilesetProfiles'
    /// configured MinimumOpeningWidth (all left at the default 1) must match what the audit computes
    /// fresh from the .set data, so a future SWLOR_Haks edit to these tilesets' pathnodes can't silently
    /// invalidate the profile's assumption.
    /// </summary>
    [TestCaseSource(nameof(OnboardedTilesetKeys))]
    public void MinimumOpeningWidth_MatchesFreshPathNodeAudit(string tilesetKey)
    {
        var profile = TilesetProfiles[tilesetKey];
        var model = LoadTileset(profile.TilesetResref);

        var audited = PathNodeOpeningWidthAudit.DetermineMinimumOpeningWidth(model, profile.PrimaryOpenTerrain);

        audited.Should().Be(profile.MinimumOpeningWidth,
            $"{tilesetKey}'s configured MinimumOpeningWidth must match the pathnode audit computed fresh from '{profile.TilesetResref}'");
    }

    [TestCaseSource(nameof(OnboardedLayoutCases))]
    public void FullPipelineSucceedsAcrossManySeeds(string tilesetKey, string layoutKey)
    {
        var tilesetProfile = TilesetProfiles[tilesetKey];
        var layoutProfile = LayoutProfiles[layoutKey];
        var model = LoadTileset(tilesetProfile.TilesetResref);

        var composition = new DungeonComposition { Content = null, Tileset = tilesetProfile, Layout = layoutProfile };
        var failures = new List<string>();

        const int size = 20;
        for (var seed = 6000; seed < 6015; seed++)
        {
            var parameters = composition.BuildLayoutParameters();
            parameters.EntranceCount = 1;
            parameters.ExitCount = 1;
            parameters.DoorTransitions = true;

            var solved = LayoutSolver.Solve(parameters, model, size, size, seed, tilesetProfile.PrimaryOpenTerrain);
            if (!solved.Success)
            {
                failures.Add($"seed {seed}: generation failed -- {solved.FailureReason}");
                continue;
            }

            AssertEdgeSelfConsistency(model, solved.Layout, solved.Resolved, seed, failures);
        }

        failures.Should().BeEmpty(
            $"{tilesetKey}/{layoutKey} must generate+resolve successfully with self-consistent edges across every seed");
    }

    /// <summary>
    /// Barrows (tbw01) used to have no "Doorway" crosser in its own declared vocabulary at all, only
    /// "corridor"/"door_barrow"/"door_corridor", every one of which carries a door slot on every real
    /// tile -- Complex/Tunnel used to downgrade to OpenLane here (TileResolver's crosser+door admission
    /// gate hardcoded literal "Doorway"/"Bridge" and excluded all of them). BaseGameTilesetProfiles.
    /// Barrows now declares TunnelCrossers("corridor", "door_corridor") + DoorSlotCrossers
    /// ("door_corridor"), and CustomCrosserProfile_ComplexActuallyCarvesTheDeclaredBodyCrosser below
    /// (which now covers Barrows) proves the downgrade no longer triggers and the "corridor" body
    /// crosser is actually carved. This test additionally locks in the door-slot half specifically:
    /// a resolved Barrows/Complex layout actually places at least one "door_corridor" port-crosser edge
    /// (not merely the body), confirming the door-slot gate generalization is what unlocked this family
    /// rather than some other path.
    /// </summary>
    [Test]
    public void BarrowsComplexCarvesDoorCorridorPortCrosser()
    {
        var tilesetProfile = TilesetProfiles[BaseGameTilesetProfiles.Barrows];
        var layoutProfile = LayoutProfiles[StandardLayoutProfiles.Complex];
        var model = LoadTileset(tilesetProfile.TilesetResref);
        var composition = new DungeonComposition { Content = null, Tileset = tilesetProfile, Layout = layoutProfile };

        const int size = 20;
        var seedCount = 0;
        var sawPortCrosser = false;

        for (var seed = 6000; seed < 6015; seed++)
        {
            seedCount++;
            var parameters = composition.BuildLayoutParameters();
            parameters.EntranceCount = 1;
            parameters.ExitCount = 1;
            parameters.DoorTransitions = true;

            var solved = LayoutSolver.Solve(parameters, model, size, size, seed, tilesetProfile.PrimaryOpenTerrain);
            solved.Success.Should().BeTrue($"seed {seed}: Barrows/Complex must succeed with its own renamed Tunnel vocabulary -- {solved.FailureReason}");

            for (var y = 0; y < solved.Layout.Corners.Height && !sawPortCrosser; y++)
            for (var x = 0; x < solved.Layout.Corners.Width && !sawPortCrosser; x++)
            for (var slot = 0; slot < 4; slot++)
            {
                if (string.Equals(solved.Layout.Crossers.GetEdge(x, y, slot), "door_corridor", StringComparison.OrdinalIgnoreCase))
                    sawPortCrosser = true;
            }
        }

        seedCount.Should().Be(15, "the seed loop must actually have run");
        sawPortCrosser.Should().BeTrue(
            "Barrows/Complex must actually carve a 'door_corridor' port edge somewhere across the seed range -- the door-slot gate generalization is what makes this family resolvable at all");
    }

    /// <summary>
    /// Illithid Interior (tii01) declares both "Doorway" and "Corridor" crossers, so the old bare
    /// crosser-presence check passed, but it has no tile for a solid cell carrying a Doorway edge
    /// together with two Corridor edges (a "T-with-port" junction: a corridor bend merging directly
    /// into a room's doorway port) -- confirmed via TunnelVocabularyCheck.SupportsTunnels returning
    /// false for tii01 purely because of that one shape (every other required shape resolves).
    /// MacroLayoutGenerator downgrades CorridorMode to OpenLane for this pairing before dispatch, the
    /// same mechanism BarrowsComplexDowngradesToOpenLaneWithNoTunnelCrossers locks in -- this proves the
    /// downgrade actually took effect (no Tunnel-only crosser ever appears, no TunnelLinks recorded) and
    /// that generation succeeds on every seed in the pipeline gate's own seed range.
    /// </summary>
    [Test]
    public void IllithidComplexDowngradesToOpenLaneWithNoTunnelCrossers()
    {
        var tilesetProfile = TilesetProfiles[BaseGameTilesetProfiles.IllithidInterior];
        var layoutProfile = LayoutProfiles[StandardLayoutProfiles.Complex];
        var model = LoadTileset(tilesetProfile.TilesetResref);
        var composition = new DungeonComposition { Content = null, Tileset = tilesetProfile, Layout = layoutProfile };

        const int size = 20;
        var seedCount = 0;

        for (var seed = 6000; seed < 6015; seed++)
        {
            seedCount++;
            var parameters = composition.BuildLayoutParameters();
            parameters.EntranceCount = 1;
            parameters.ExitCount = 1;
            parameters.DoorTransitions = true;

            var solved = LayoutSolver.Solve(parameters, model, size, size, seed, tilesetProfile.PrimaryOpenTerrain);
            solved.Success.Should().BeTrue($"seed {seed}: Illithid/Complex must succeed via the OpenLane downgrade -- {solved.FailureReason}");

            for (var y = 0; y < solved.Layout.Corners.Height; y++)
            for (var x = 0; x < solved.Layout.Corners.Width; x++)
            for (var slot = 0; slot < 4; slot++)
            {
                var edge = solved.Layout.Crossers.GetEdge(x, y, slot);
                edge.Should().NotBe("Corridor", $"seed {seed}: downgraded Illithid/Complex must never carve a Tunnel-mode Corridor edge");
                edge.Should().NotBe("Doorway", $"seed {seed}: downgraded Illithid/Complex must never carve a Tunnel-mode Doorway edge");
            }

            solved.Layout.TunnelLinks.Should().BeEmpty(
                $"seed {seed}: downgraded Illithid/Complex must carve plain open lanes (no TunnelLinks), not wall-embedded tunnels");
        }

        seedCount.Should().Be(15, "the seed loop must actually have run");
    }

    /// <summary>
    /// Ruins (tdr01) has no crosser-free tile blending Floor and Chasm corners, so an open-space
    /// channel crossing (what Organic's AccentChannels=1 carves) can never resolve there.
    /// LayoutAccentChannelCarver now runs a whole-tileset capability probe before ever attempting to
    /// carve and skips gracefully when it can't -- this locks in that Ruins/Organic both succeeds
    /// end-to-end AND never actually emits a Bridge channel edge (the graceful-skip path, not a lucky
    /// ValidateBand failure), across the same seed range the pipeline gate uses.
    /// </summary>
    [Test]
    public void RuinsOrganicSkipsChannelGracefully()
    {
        var tilesetProfile = TilesetProfiles[BaseGameTilesetProfiles.Ruins];
        var layoutProfile = LayoutProfiles[StandardLayoutProfiles.Organic];
        var model = LoadTileset(tilesetProfile.TilesetResref);
        var composition = new DungeonComposition { Content = null, Tileset = tilesetProfile, Layout = layoutProfile };

        composition.BuildLayoutParameters().AccentChannels.Should().BeGreaterThan(0,
            "Organic's template must still request a channel -- this test proves the carver skips it gracefully, not that the knob is off");

        const int size = 20;
        var sawBridgeEdge = false;

        for (var seed = 6000; seed < 6015; seed++)
        {
            var parameters = composition.BuildLayoutParameters();
            parameters.EntranceCount = 1;
            parameters.ExitCount = 1;
            parameters.DoorTransitions = true;

            var solved = LayoutSolver.Solve(parameters, model, size, size, seed, tilesetProfile.PrimaryOpenTerrain);
            solved.Success.Should().BeTrue($"seed {seed}: Ruins/Organic must succeed via the graceful channel skip -- {solved.FailureReason}");

            for (var y = 0; y < solved.Layout.Corners.Height && !sawBridgeEdge; y++)
            for (var x = 0; x < solved.Layout.Corners.Width && !sawBridgeEdge; x++)
            for (var slot = 0; slot < 4; slot++)
            {
                if (string.Equals(solved.Layout.Crossers.GetEdge(x, y, slot), "Bridge", System.StringComparison.OrdinalIgnoreCase))
                    sawBridgeEdge = true;
            }
        }

        sawBridgeEdge.Should().BeFalse(
            "tdr01 has no resolvable bank/span shape for Chasm-vs-Floor -- CanCarve must keep every Ruins/Organic layout channel-free");
    }

    /// <summary>
    /// Locks in that a palette-variant profile declaring TunnelCrossers (see
    /// DungeonTilesetProfile.TunnelBodyCrosser/TunnelPortCrosser) actually composes into
    /// CorridorCrosserType.Custom and carves ITS OWN district-scoped body crosser under Complex --
    /// not merely that generation succeeds (FullPipelineSucceedsAcrossManySeeds already proves that),
    /// but that the Custom-mode downgrade path (MacroLayoutGenerator's shape re-probe) never actually
    /// triggers for these three verified-good families, the positive-case mirror of
    /// BarrowsComplexDowngradesToOpenLaneWithNoTunnelCrossers/IllithidComplexDowngradesToOpenLaneWithNoTunnelCrossers
    /// above.
    /// </summary>
    [TestCase(BaseGameTilesetProfiles.CryptGrey, "GreyCorridor")]
    [TestCase(BaseGameTilesetProfiles.MinesAndCavernsDesert, "DesertCorridor")]
    [TestCase(BaseGameTilesetProfiles.MinesAndCavernsOrganic, "OrganicCorridor")]
    [TestCase(BaseGameTilesetProfiles.Barrows, "corridor")]
    public void CustomCrosserProfile_ComplexActuallyCarvesTheDeclaredBodyCrosser(string tilesetKey, string expectedBodyCrosser)
    {
        var tilesetProfile = TilesetProfiles[tilesetKey];
        var layoutProfile = LayoutProfiles[StandardLayoutProfiles.Complex];
        var model = LoadTileset(tilesetProfile.TilesetResref);
        var composition = new DungeonComposition { Content = null, Tileset = tilesetProfile, Layout = layoutProfile };

        composition.BuildLayoutParameters().CorridorCrosserType.Should().Be(CorridorCrosserType.Custom,
            $"{tilesetKey} declares TunnelCrossers, so composing with a Corridor-type Tunnel layout must switch to Custom mode");

        const int size = 20;
        var sawBodyCrosser = false;
        var seedCount = 0;

        for (var seed = 6000; seed < 6015; seed++)
        {
            seedCount++;
            var parameters = composition.BuildLayoutParameters();
            parameters.EntranceCount = 1;
            parameters.ExitCount = 1;
            parameters.DoorTransitions = true;

            var solved = LayoutSolver.Solve(parameters, model, size, size, seed, tilesetProfile.PrimaryOpenTerrain);
            solved.Success.Should().BeTrue($"seed {seed}: {tilesetKey}/Complex must succeed with its Custom crosser vocabulary -- {solved.FailureReason}");

            for (var y = 0; y < solved.Layout.Corners.Height && !sawBodyCrosser; y++)
            for (var x = 0; x < solved.Layout.Corners.Width && !sawBodyCrosser; x++)
            for (var slot = 0; slot < 4; slot++)
            {
                if (string.Equals(solved.Layout.Crossers.GetEdge(x, y, slot), expectedBodyCrosser, StringComparison.OrdinalIgnoreCase))
                    sawBodyCrosser = true;
            }
        }

        seedCount.Should().Be(15, "the seed loop must actually have run");
        sawBodyCrosser.Should().BeTrue(
            $"{tilesetKey}/Complex must actually carve '{expectedBodyCrosser}' edges somewhere across the seed range -- the downgrade-to-OpenLane path must never trigger for this verified-good family");
    }

    /// <summary>
    /// Locks in that LayoutGroupStamper.TryClassify's WallRoom door-slot relaxation (a door-entrance
    /// group -- blank wall tile paired with a tile carrying BOTH a perimeter Doorway edge AND a door
    /// slot -- now classifies as SetPieceWallRoom instead of failing outright) actually places real
    /// tiles in a generated layout, not merely that TileCoverageCensusTests' structural probe agrees.
    /// Checks a representative member tile id from EVERY newly-wired family per tileset (any one
    /// placing proves that whole group stamped, since LayoutGroupStamper.StampWallRoom writes every
    /// member together or none at all) rather than a single id -- Fort Interior alone wired ten
    /// competing families, so any individual one's per-seed placement odds are low (confirmed via
    /// direct 200-seed isolated probe: ~10% for StoreRoom_2x2L alone, lower still with nine siblings
    /// competing for the same limited room-perimeter sites in a real composition) even though the
    /// mechanism itself is unconditionally correct. Illithid Interior is deliberately NOT covered here:
    /// see BaseGameTilesetProfiles.IllithidInterior's own doc comment for why its five newly-wired
    /// WallRoom groups can never actually place under Complex today (a separate, pre-existing Tunnel-
    /// vocabulary gap, not anything the door-slot relaxation itself could fix).
    /// </summary>
    [TestCase(BaseGameTilesetProfiles.CityInterior, new[] { 24, 26, 268 })]
    [TestCase(BaseGameTilesetProfiles.CastleInterior, new[] { 24, 42, 60, 78, 80, 96, 98, 521, 676 })]
    [TestCase(BaseGameTilesetProfiles.CastleInterior2, new[] { 24, 42, 60, 78, 96, 200 })]
    [TestCase(BaseGameTilesetProfiles.CityInterior2, new[] { 24, 26, 268 })]
    [TestCase(BaseGameTilesetProfiles.FortInterior, new[] { 195, 199, 202, 205, 207, 211, 213, 215, 217, 219, 222, 224 })]
    public void DoorSlotWallRoomFamily_ComplexActuallyPlacesTheGroup(string tilesetKey, int[] candidateTileIds)
    {
        var tilesetProfile = TilesetProfiles[tilesetKey];
        var layoutProfile = LayoutProfiles[StandardLayoutProfiles.Complex];
        var model = LoadTileset(tilesetProfile.TilesetResref);
        var composition = new DungeonComposition { Content = null, Tileset = tilesetProfile, Layout = layoutProfile };

        const int size = 20;
        var seedCount = 0;
        var sawAnyCandidateTile = false;

        for (var seed = 6000; seed < 6030; seed++)
        {
            seedCount++;
            var parameters = composition.BuildLayoutParameters();
            parameters.EntranceCount = 1;
            parameters.ExitCount = 1;
            parameters.DoorTransitions = true;

            var solved = LayoutSolver.Solve(parameters, model, size, size, seed, tilesetProfile.PrimaryOpenTerrain);
            solved.Success.Should().BeTrue($"seed {seed}: {tilesetKey}/Complex must succeed -- {solved.FailureReason}");

            if (solved.Resolved.Tiles.Any(t => candidateTileIds.Contains(t.TileId)))
                sawAnyCandidateTile = true;
        }

        seedCount.Should().Be(30, "the seed loop must actually have run");
        sawAnyCandidateTile.Should().BeTrue(
            $"{tilesetKey}/Complex must actually place at least one of [{string.Join(",", candidateTileIds)}] (members of the newly-wired door-slot WallRoom families) somewhere across the seed range");
    }

    /// <summary>
    /// Per-tile edge self-consistency: every resolved tile's oriented edge must equal the oriented
    /// edge its neighbor presents back across the shared boundary (both directions), and a tile's own
    /// declared edge must be internally well-formed (empty or a name present in this tileset's crosser
    /// vocabulary). This doesn't require the original macro crosser plan (LayoutSolver's public surface
    /// only exposes the resolved layout) -- unlike TunnelCorridorTests.AssertEdgeAgreement, which
    /// diffs against the plan directly, this checks the resolved tiles agree with EACH OTHER, which is
    /// exactly the invariant that makes seams render and path correctly in the engine.
    ///
    /// One documented exception, taken directly from LayoutGroupStamper.WriteMember's own doc comment:
    /// a multi-cell WallRoom/OpenSetPiece group's INTERIOR shared boundary may legitimately disagree
    /// between its two flanking members (e.g. a 2x1 "Room (2x1)"/"Room - Bedroom (2x1)" WallRoom whose
    /// southern member's raw Top edge is its perimeter Doorway value re-used verbatim on the internal
    /// seam, while the northern member's raw Bottom edge is blank) -- this is harmless in production
    /// because both flanking cells are PINNED (bypass corner/edge candidate lookup entirely), so a
    /// boundary where BOTH sides are pinned tiles is excluded from this check rather than flagged as a
    /// false-positive mismatch.
    /// </summary>
    private static void AssertEdgeSelfConsistency(TilesetModel model, MacroLayout layout, ResolvedLayout resolved, int seed, List<string> failures)
    {
        var tilesById = model.Tiles.ToDictionary(t => t.TileId);
        var knownCrossers = new HashSet<string>(model.Crossers, StringComparer.OrdinalIgnoreCase);

        for (var y = 0; y < resolved.Height; y++)
        {
            for (var x = 0; x < resolved.Width; x++)
            {
                var tile = resolved.GetTile(x, y);
                var record = tilesById[tile.TileId];

                var top = record.GetEdgeAt(tile.Orientation, EdgeSlot.Top) ?? string.Empty;
                var right = record.GetEdgeAt(tile.Orientation, EdgeSlot.Right) ?? string.Empty;
                var bottom = record.GetEdgeAt(tile.Orientation, EdgeSlot.Bottom) ?? string.Empty;
                var left = record.GetEdgeAt(tile.Orientation, EdgeSlot.Left) ?? string.Empty;

                foreach (var edge in new[] { top, right, bottom, left })
                {
                    if (edge.Length != 0 && !knownCrossers.Contains(edge))
                        failures.Add($"seed {seed}: cell ({x},{y}) TILE{tile.TileId} o={tile.Orientation} has unknown crosser '{edge}'");
                }

                var bothPinnedHere = layout.PinnedTiles.ContainsKey((x, y));

                if (x + 1 < resolved.Width)
                {
                    var neighbor = resolved.GetTile(x + 1, y);
                    var neighborRecord = tilesById[neighbor.TileId];
                    var neighborLeft = neighborRecord.GetEdgeAt(neighbor.Orientation, EdgeSlot.Left) ?? string.Empty;
                    var bothPinned = bothPinnedHere && layout.PinnedTiles.ContainsKey((x + 1, y));
                    if (!bothPinned && !string.Equals(right, neighborLeft, StringComparison.OrdinalIgnoreCase))
                        failures.Add($"seed {seed}: cell ({x},{y})|({x + 1},{y}) edge mismatch: '{right}' vs '{neighborLeft}'");
                }

                if (y + 1 < resolved.Height)
                {
                    var neighbor = resolved.GetTile(x, y + 1);
                    var neighborRecord = tilesById[neighbor.TileId];
                    var neighborBottom = neighborRecord.GetEdgeAt(neighbor.Orientation, EdgeSlot.Bottom) ?? string.Empty;
                    var bothPinned = bothPinnedHere && layout.PinnedTiles.ContainsKey((x, y + 1));
                    if (!bothPinned && !string.Equals(top, neighborBottom, StringComparison.OrdinalIgnoreCase))
                        failures.Add($"seed {seed}: cell ({x},{y})|({x},{y + 1}) edge mismatch: '{top}' vs '{neighborBottom}'");
                }
            }
        }
    }
}
