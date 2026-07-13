using System;
using SWLOR.Game.Server.Service.AreaGenerationService;

namespace SWLOR.Game.Server.Tests.AreaGeneration;

/// <summary>
/// Reusable "zsf01-style" pathnode audit for deciding a tileset profile's MinimumOpeningWidth: zsf01
/// needed width 2 because EVERY partially-open TL/TR/BR/BL corner combination among its real,
/// crosser-free, ungrouped, flat tiles carries a movement-restricted (non-'A') pathnode -- only its
/// fully-open tile is pathnode 'A' -- so a 1-wide door gap/corridor fails the engine's path check
/// (see StandardTilesetProfiles.Facility). This walks the same eligibility rule
/// TileCoverageCensusTests.IsCornerEdgeResolverReachable uses (flat, ungrouped, crosser-free), across
/// all 14 partially-open combos (excluding all-solid and all-open), checking each combo's candidates
/// for a pathnode-'A' tile -- something TileResolver's public HasCandidate hook doesn't expose.
/// Intended to be reused by every future base-game tileset onboarding wave, not just the pilots.
/// </summary>
internal static class PathNodeOpeningWidthAudit
{
    private static bool Eq(string a, string b) => string.Equals(a ?? string.Empty, b ?? string.Empty, StringComparison.OrdinalIgnoreCase);

    /// <summary>
    /// Returns 2 if every partially-open corner combo's candidates lack a pathnode-'A' tile (a 1-wide
    /// opening can never path through), otherwise 1 (some partial combo is fully pathable, so 1-wide
    /// openings work). <paramref name="openTerrain"/> defaults to the tileset's declared floor terrain
    /// when empty, matching DungeonTilesetProfile.PrimaryOpenTerrain's own empty-means-floor default.
    /// </summary>
    public static int DetermineMinimumOpeningWidth(TilesetModel model, string openTerrain = null)
    {
        if (model == null) throw new ArgumentNullException(nameof(model));

        var solid = model.DefaultTerrain;
        var open = string.IsNullOrEmpty(openTerrain) ? model.FloorTerrain : openTerrain;

        // Bit 0=TopLeft, 1=TopRight, 2=BottomRight, 3=BottomLeft; 1 = open, 0 = solid.
        // combo 0 (all solid) and combo 15 (all open) are not "partially open" -- skip them.
        for (var combo = 1; combo < 15; combo++)
        {
            var wantTl = (combo & 1) != 0 ? open : solid;
            var wantTr = (combo & 2) != 0 ? open : solid;
            var wantBr = (combo & 4) != 0 ? open : solid;
            var wantBl = (combo & 8) != 0 ? open : solid;

            if (ComboHasPathNodeACandidate(model, wantTl, wantTr, wantBr, wantBl))
                return 1;
        }

        return 2;
    }

    private static bool ComboHasPathNodeACandidate(TilesetModel model, string wantTl, string wantTr, string wantBr, string wantBl)
    {
        foreach (var tile in model.Tiles)
        {
            if (tile.GroupIndex != -1) continue;
            if (tile.CornerHeights[0] != 0 || tile.CornerHeights[1] != 0 ||
                tile.CornerHeights[2] != 0 || tile.CornerHeights[3] != 0) continue;
            if (tile.HasAnyCrosser) continue;
            if (tile.Doors.Count != 0) continue;
            if (!Eq(tile.PathNode, "A")) continue;

            for (var orientation = 0; orientation < 4; orientation++)
            {
                var tl = tile.GetCornerAt(orientation, CornerSlot.TopLeft);
                var tr = tile.GetCornerAt(orientation, CornerSlot.TopRight);
                var br = tile.GetCornerAt(orientation, CornerSlot.BottomRight);
                var bl = tile.GetCornerAt(orientation, CornerSlot.BottomLeft);

                if (Eq(tl, wantTl) && Eq(tr, wantTr) && Eq(br, wantBr) && Eq(bl, wantBl))
                    return true;
            }
        }

        return false;
    }
}
