using System;
using System.Linq;
using SWLOR.Game.Server.Service.AreaGenerationService;

namespace SWLOR.Game.Server.Tests.AreaGeneration;

/// <summary>
/// Test-side mirror of LayoutGroupStamper.TryClassify's multi-tile (Rows&gt;=2 or Columns&gt;=2)
/// branch, since that method is internal and the test project has no InternalsVisibleTo access.
/// Used ONLY to filter which configured 2x2+ groups are genuine OpenSetPiece candidates for the
/// placement-rate measurement/regression tests -- not a replacement for the production classifier,
/// and deliberately narrower (ignores 1x1 CorridorInsert/CorridorStub/ReliefPiece paths, which never
/// apply to a Rows&gt;=2/Columns&gt;=2 group). Keep in sync with LayoutGroupStamper.TryClassify if that
/// method's multi-tile rules ever change.
/// </summary>
internal enum MirroredGroupKind { Invalid, WallRoom, WallAlcove, OpenSetPiece, CorridorStubChain }

internal static class OpenSetPieceClassificationMirror
{
    private const string DoorwayCrosser = "Doorway";
    private const string CorridorCrosser = "Corridor";
    private const string AlleyCrosser = "Alley";

    private static bool Eq(string a, string b) => string.Equals(a ?? string.Empty, b ?? string.Empty, StringComparison.OrdinalIgnoreCase);

    public static MirroredGroupKind Classify(
        TileGroupRecord group, TilesetModel tileset, string solidTerrain, string openTerrain, string secondaryOpenTerrain,
        string customBodyCrosser = null)
    {
        if (group.Rows <= 0 || group.Columns <= 0) return MirroredGroupKind.Invalid;
        if (group.TileIds.Count != group.Rows * group.Columns) return MirroredGroupKind.Invalid;

        var stubCrossers = string.IsNullOrEmpty(customBodyCrosser)
            ? new[] { CorridorCrosser, AlleyCrosser }
            : new[] { CorridorCrosser, AlleyCrosser, customBodyCrosser };

        bool IsAllowedMemberEdge(string edge) =>
            string.IsNullOrEmpty(edge) || Eq(edge, DoorwayCrosser) || stubCrossers.Any(c => Eq(edge, c));

        var members = new System.Collections.Generic.List<TileRecord>();
        for (var row = 0; row < group.Rows; row++)
        {
            for (var col = 0; col < group.Columns; col++)
            {
                var tileId = group.TileIds[row * group.Columns + col];
                if (tileId < 0) continue;
                if (tileId >= tileset.Tiles.Count) return MirroredGroupKind.Invalid;

                var tile = tileset.Tiles[tileId];
                if (tile.CornerHeights[0] != 0 || tile.CornerHeights[1] != 0 ||
                    tile.CornerHeights[2] != 0 || tile.CornerHeights[3] != 0) return MirroredGroupKind.Invalid;

                foreach (var edge in tile.Edges)
                {
                    if (!IsAllowedMemberEdge(edge)) return MirroredGroupKind.Invalid;
                }

                members.Add(tile);
            }
        }
        if (members.Count == 0) return MirroredGroupKind.Invalid;

        var hasAnyDoorway = members.Any(m => m.Edges.Any(e => Eq(e, DoorwayCrosser)));
        var hasAnyBodyCrosser = members.Any(m => m.Edges.Any(e => !Eq(e, DoorwayCrosser) && stubCrossers.Any(c => Eq(e, c))));
        var allCornersSolid = members.All(m => m.Corners.All(c => Eq(c, solidTerrain)));
        var hasAnyDoor = members.Any(m => m.Doors.Count != 0);

        if (hasAnyBodyCrosser)
        {
            return hasAnyDoorway || !allCornersSolid ? MirroredGroupKind.Invalid : MirroredGroupKind.CorridorStubChain;
        }

        if (hasAnyDoorway)
        {
            return !allCornersSolid ? MirroredGroupKind.Invalid : MirroredGroupKind.WallRoom;
        }

        if (allCornersSolid && hasAnyDoor)
        {
            return MirroredGroupKind.WallAlcove;
        }

        var matchesPrimary = members.All(m => m.Corners.All(c => Eq(c, solidTerrain) || Eq(c, openTerrain))) &&
                              members.Any(m => m.Corners.Any(c => Eq(c, openTerrain)));
        var matchesSecondary = !string.IsNullOrEmpty(secondaryOpenTerrain) &&
                                members.All(m => m.Corners.All(c => Eq(c, solidTerrain) || Eq(c, secondaryOpenTerrain))) &&
                                members.Any(m => m.Corners.Any(c => Eq(c, secondaryOpenTerrain)));

        return matchesPrimary || matchesSecondary ? MirroredGroupKind.OpenSetPiece : MirroredGroupKind.Invalid;
    }
}
