using System.Linq;
using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Feature.DungeonDefinition;
using SWLOR.Game.Server.Service.AreaGenerationService;
using SWLOR.Game.Server.Service.AreaGenerationService.Tileset;

namespace SWLOR.Game.Server.Tests.AreaGeneration;

/// <summary>
/// Explicitly tests the tss13 (Sea Ships) onboarding pass' own flagged hypothesis (see
/// BaseGameTilesetProfiles.SeaShips' own doc comment): would declaring "gangplank" as
/// DungeonTilesetProfile.TunnelBodyCrosser under the same-terrain (SolidTerrainOverride ==
/// PrimaryOpenTerrain) composition let a gangplank-bearing "Boat N" group classify and PLACE as a
/// CorridorStubChain, unlocking the 88 gangplank-bearing groups this profile otherwise leaves exempt?
///
/// Result: it CLASSIFIES (see GangplankAsBodyCrosser_ClassifiesAsCorridorStubChain below -- once
/// "gangplank" is a recognized body crosser and Solid == Open, hasAnyBodyCrosser + allCornersSolid +
/// a perimeter body-crosser edge are all trivially satisfied, the exact shape
/// LayoutGroupStamper.TryClassify's CorridorStubChain branch accepts), but it NEVER PLACES (see
/// GangplankAsBodyCrosser_NeverPlacesAsCorridorStubChain below -- 0/150 across Complex, the only
/// layout that ever enters real Tunnel mode). TryPlaceCorridorStubChain only ever splices onto an
/// EXISTING Tunnel-mode chain network LayoutTunnelCarver wove through solid wall space, and this
/// composition's SolidTerrainOverride == PrimaryOpenTerrain means there is no wall mass anywhere for
/// MacroLayoutGenerator to ever carve a real Tunnel-mode chain through in the first place (the same
/// reason every other open-field profile in this file declares no Tunnel vocabulary at all) -- a
/// gangplank-bearing group's own tiles are excluded from ordinary chain-network candidacy the moment
/// they're claimed by a GROUP (LayoutTunnelCarver only ever carves through ungrouped cells), so there
/// is never a live chain for TryPlaceCorridorStubChain to find regardless of how the group itself
/// classifies. This is the same "documented, not silently regressed" shape as
/// OpenSetPiecePlacementRateTests' own room-size-ceiling/nonflat-bank-ceiling tests -- confirmed here
/// rather than merely assumed, per this onboarding pass' own verification requirement. Kept undeclared
/// on every shipped tss13 profile; the shipped gangplank exemption in TileCoverageCensusTests is not a
/// missed unlock.
/// </summary>
public class SeaShipsGangplankHypothesisTests
{
    private const int Size = 20;

    private static TilesetModel LoadTileset() => TilesetTestSource.LoadTileset("tss13");

    private static DungeonTilesetProfile BuildGangplankBodyCrosserProfile(string groupName, int maxPerArea)
    {
        return new DungeonTilesetProfile
        {
            Key = "seaships_gangplank_hypothesis",
            DisplayName = "Sea Ships (gangplank hypothesis)",
            TilesetResref = "tss13",
            PlaceholderResref = "gen_placeholder1",
            PrimaryOpenTerrain = "Castle",
            TunnelBodyCrosser = "gangplank",
            TunnelPortCrosser = "Doorway",
            SetPieces = { [groupName] = maxPerArea },
        };
    }

    [Test]
    public void GangplankAsBodyCrosser_ClassifiesAsCorridorStubChain()
    {
        var model = LoadTileset();
        // Castle terrain's first gangplank-bearing "Boat 1" instance (wave A: TILE34/35/36, TILE35
        // carries a single "gangplank" edge in slot 3 -- see BaseGameTilesetProfiles.SeaShips' own
        // doc comment for the full tile-id-range writeup).
        var group = model.Groups.First(g => g.Name == "Boat 1" && g.TileIds.Contains(35));

        var kind = OpenSetPieceClassificationMirror.Classify(
            group, model, solidTerrain: "Castle", openTerrain: "Castle", secondaryOpenTerrain: null,
            customBodyCrosser: "gangplank");

        kind.Should().Be(MirroredGroupKind.CorridorStubChain,
            "declaring gangplank as a body crosser under this same-terrain composition makes hasAnyBodyCrosser/" +
            "allCornersSolid/perimeter-body-crosser all trivially true, the exact CorridorStubChain shape");
    }

    [Test]
    public void GangplankAsBodyCrosser_NeverPlacesAsCorridorStubChain()
    {
        var model = LoadTileset();
        var tilesetProfile = BuildGangplankBodyCrosserProfile("Boat 1", maxPerArea: 5);
        var layoutProfile = new StandardLayoutProfiles().BuildLayoutProfiles()[StandardLayoutProfiles.Complex];

        // Anchor on TILE34 (the gangplank-wave "Boat 1" group's own first member) -- a hit means
        // LayoutSolver's PinnedTiles wrote this exact physical group somewhere.
        var anchorTileId = 34;

        var successes = 0;
        var hits = 0;
        const int seedCount = 150;
        for (var i = 0; i < seedCount; i++)
        {
            var seed = 95000 + i * 13;
            var composition = new DungeonComposition { Tileset = tilesetProfile, Layout = layoutProfile };
            var result = LayoutSolver.Solve(composition.BuildLayoutParameters(), model, Size, Size, seed, tilesetProfile.PrimaryOpenTerrain, retryCount: 1);
            if (!result.Success) continue;
            successes++;

            if (result.Layout.PinnedTiles.Values.Any(p => p.TileId == anchorTileId))
                hits++;
        }

        successes.Should().BeGreaterThan(0, "at least some seeds must generate successfully for this measurement to be meaningful");
        hits.Should().Be(0,
            "no Tunnel-mode chain network ever exists to splice onto under an open-field (Solid==Open) composition -- " +
            "if this ever starts placing, Tunnel-mode carving on open-field compositions changed and this test " +
            "(and its class-level doc comment) should be revisited deliberately, not silently deleted");
    }
}
