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
/// apply to a Rows&gt;=2/Columns&gt;=2 group, and never generalizes DoorwayCrosser via
/// MacroLayoutParameters.DoorSlotCrossers -- callers on a DoorSlotCrossers-declaring profile, e.g.
/// udp2/tbx78, must not rely on this mirror to recognize their renamed door-family edge as
/// "Doorway"). Keep in sync with LayoutGroupStamper.TryClassify if that method's multi-tile rules
/// ever change -- see this class's own mixed/open-member fallthrough below (added alongside
/// production's identical fallthrough) for the current shape of that sync.
/// </summary>
internal enum MirroredGroupKind { Invalid, WallRoom, WallAlcove, OpenSetPiece, CorridorStubChain }

internal static class OpenSetPieceClassificationMirror
{
    private const string DoorwayCrosser = "Doorway";
    private const string CorridorCrosser = "Corridor";
    private const string AlleyCrosser = "Alley";

    // Slot -> (Dx, Dy) step to the neighboring cell across that edge -- mirrors
    // LayoutGroupStamper.SlotOffsets (Top=0/Right=1/Bottom=2/Left=3, Top is +Y/north).
    private static readonly (int Dx, int Dy)[] SlotOffsets = { (0, 1), (1, 0), (0, -1), (-1, 0) };

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
        var positioned = new System.Collections.Generic.List<(int Row, int Col, TileRecord Tile)>();
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
                positioned.Add((row, col, tile));
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
            // Mirrors LayoutGroupStamper.TryClassify's own mixed/open-member fallthrough: a doorway
            // edge implies WallRoom only when every corner is solid; a mixed shape is tolerated ONLY
            // when every doorway edge is interior to the group's own footprint (faces another member of
            // the SAME group, never the group's own perimeter) -- see production's own doc comment on
            // this exact branch. Falls through to the OpenSetPiece corner-match check below when that
            // holds (e.g. udp2's "*_Entry 2x1" family, tbx78's "elevator").
            if (allCornersSolid) return MirroredGroupKind.WallRoom;

            var hasAnyPerimeterDoorway = false;
            foreach (var (row, col, tile) in positioned)
            {
                for (var slot = 0; slot < 4; slot++)
                {
                    if (!Eq(tile.GetEdgeAt(0, slot), DoorwayCrosser)) continue;

                    var (dx, dy) = SlotOffsets[slot];
                    var neighborRow = row + dy;
                    var neighborCol = col + dx;
                    var outOfBounds = neighborRow < 0 || neighborRow >= group.Rows ||
                                       neighborCol < 0 || neighborCol >= group.Columns;
                    if (outOfBounds || group.TileIds[neighborRow * group.Columns + neighborCol] < 0)
                        hasAnyPerimeterDoorway = true;
                }
            }
            if (hasAnyPerimeterDoorway) return MirroredGroupKind.Invalid;
            // else: fall through to the OpenSetPiece corner-match check below.
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
