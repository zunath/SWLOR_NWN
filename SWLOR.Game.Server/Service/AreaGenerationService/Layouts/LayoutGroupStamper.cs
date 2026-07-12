using System;
using System.Collections.Generic;
using System.Linq;

namespace SWLOR.Game.Server.Service.AreaGenerationService.Layouts
{
    /// <summary>
    /// Stamps pre-designed tileset "group" set pieces (multi-tile, and 1x1) into a macro layout: wall-
    /// bounded rooms hanging off Tunnel-mode corridors ("WallRoom", e.g. zsf01's Cell/Bedroom) and
    /// floor-level decorative pieces dropped into open room interiors ("OpenSetPiece", e.g. vmr01's
    /// Amphitheater). Runs in MacroLayoutGenerator after AssignTransitions (transitions are already
    /// anchored to open room tiles, so set pieces can avoid them) and before ValidateInvariants (so a
    /// bad stamp fails loudly instead of silently corrupting a layout).
    ///
    /// Every candidate site is fully validated before any grid data is written; a group that can't
    /// find a legal site for a given instance is simply skipped (0..maxCount placed for that group)
    /// rather than ever leaving the layout in a bad state or failing generation outright.
    ///
    /// Orientation is always 0 (v1: no group rotation). TileGroupRecord.TileIds is row-major with row
    /// 0 the group's southernmost row and column 0 its westernmost column at orientation 0 — identical
    /// to ResolvedLayout.Tiles' own bottom-up row-major indexing. Pinned empirically against
    /// czs220_maintlvl's placed "Bedroom" group (zsf01 TILE69/70, Rows=2 Columns=1, TileIds=[70,69]):
    /// at orientation 2 (180 degrees) TILE69 (row 1) sits south and TILE70 (row 0) sits north — the
    /// unrotated row0=south is flipped to north by the half turn — and at orientation 1 (90 degrees
    /// CCW) TILE69 (row 1, unrotated north) lands west while TILE70 (row 0, unrotated south) lands
    /// east, matching the engine's standard CCW90 rotation (north -> west) already documented on
    /// TileRecord.GetCornerAt. If this pin ever needs to change, fix ONLY the anchor/footprint math
    /// below — nothing else in this file assumes a direction.
    /// </summary>
    internal static class LayoutGroupStamper
    {
        private const string DoorwayCrosser = "Doorway";
        private const string CorridorCrosser = "Corridor";

        // Slot -> (Dx, Dy) step to the neighboring cell across that edge. Matches EdgeSlot's
        // Top=0/Right=1/Bottom=2/Left=3 ordering and the "Top is the +Y (north) side" convention
        // documented on TilesetModel.TileRecord — also the convention pinned above for group rows.
        private static readonly (int Dx, int Dy)[] SlotOffsets = { (0, 1), (1, 0), (0, -1), (-1, 0) };

        private sealed class GroupMember
        {
            public int LocalRow;
            public int LocalCol;
            public TileRecord Tile;
        }

        private enum GroupKind { WallRoom, OpenSetPiece }

        private sealed class ClassifiedGroup
        {
            public TileGroupRecord Group;
            public List<GroupMember> Members;
            public GroupKind Kind;

            /// <summary>
            /// (LocalRow, LocalCol, Slot) for every Doorway edge whose neighbor cell falls outside the
            /// group's own footprint — the openings that must face a real tunnel corridor cell.
            /// WallRoom only.
            /// </summary>
            public List<(int Row, int Col, int Slot)> PerimeterDoorways;
        }

        internal static void Stamp(MacroLayout layout, MacroLayoutParameters parameters, TilesetModel tileset, System.Random random)
        {
            if (tileset == null || parameters.SetPieces == null || parameters.SetPieces.Count == 0)
                return;

            var nextRoomId = layout.Rooms.Count == 0 ? 0 : layout.Rooms.Max(r => r.Id) + 1;

            foreach (var groupName in parameters.SetPieces.Keys.OrderBy(k => k, StringComparer.Ordinal))
            {
                var maxCount = parameters.SetPieces[groupName];
                if (maxCount <= 0) continue;

                var group = FindGroup(tileset, groupName);
                if (group == null) continue;

                if (!TryClassify(tileset, group, parameters, out var classified))
                    continue;

                for (var i = 0; i < maxCount; i++)
                {
                    var placed = classified.Kind == GroupKind.WallRoom
                        ? TryPlaceWallRoom(layout, parameters, classified, random, ref nextRoomId)
                        : TryPlaceOpenSetPiece(layout, parameters, classified, random);

                    // A failed search means the grid state can't improve for this group without
                    // human/seed changes; further attempts on the same state would only repeat it.
                    if (!placed) break;
                }
            }
        }

        private static TileGroupRecord FindGroup(TilesetModel tileset, string name)
        {
            foreach (var candidate in tileset.Groups)
            {
                if (string.Equals(candidate.Name, name, StringComparison.OrdinalIgnoreCase))
                    return candidate;
            }

            return null;
        }

        /// <summary>
        /// Structurally re-verifies a configured group name against real tileset data rather than
        /// trusting a tileset profile's list blindly: rejects holes (-1 members), raised corners, door
        /// slots, and non-Doorway crossers, then classifies the surviving shape as WallRoom (all-solid
        /// corners with at least one perimeter Doorway edge) or OpenSetPiece (no crosser edges at all,
        /// with every corner either solid or matching this layout's own open terrain).
        /// </summary>
        private static bool TryClassify(TilesetModel tileset, TileGroupRecord group, MacroLayoutParameters parameters, out ClassifiedGroup classified)
        {
            classified = null;
            if (group.Rows <= 0 || group.Columns <= 0) return false;
            if (group.TileIds.Count != group.Rows * group.Columns) return false;

            var members = new List<GroupMember>();
            for (var row = 0; row < group.Rows; row++)
            {
                for (var col = 0; col < group.Columns; col++)
                {
                    var tileId = group.TileIds[row * group.Columns + col];
                    if (tileId < 0 || tileId >= tileset.Tiles.Count) return false; // hole or out of range

                    var tile = tileset.Tiles[tileId];
                    if (tile.CornerHeights[0] != 0 || tile.CornerHeights[1] != 0 ||
                        tile.CornerHeights[2] != 0 || tile.CornerHeights[3] != 0) return false; // raised
                    if (tile.Doors.Count != 0) return false; // door slots are out of scope for this pass

                    foreach (var edge in tile.Edges)
                    {
                        if (!string.IsNullOrEmpty(edge) && !Eq(edge, DoorwayCrosser)) return false;
                    }

                    members.Add(new GroupMember { LocalRow = row, LocalCol = col, Tile = tile });
                }
            }

            var perimeterDoorways = new List<(int, int, int)>();
            foreach (var member in members)
            {
                for (var slot = 0; slot < 4; slot++)
                {
                    if (!Eq(member.Tile.GetEdgeAt(0, slot), DoorwayCrosser)) continue;

                    var (dx, dy) = SlotOffsets[slot];
                    var neighborRow = member.LocalRow + dy;
                    var neighborCol = member.LocalCol + dx;
                    var isPerimeter = neighborRow < 0 || neighborRow >= group.Rows ||
                                       neighborCol < 0 || neighborCol >= group.Columns;
                    if (isPerimeter)
                        perimeterDoorways.Add((member.LocalRow, member.LocalCol, slot));
                }
            }

            var hasAnyDoorway = members.Any(m => m.Tile.Edges.Any(e => Eq(e, DoorwayCrosser)));
            var allCornersSolid = members.All(m => m.Tile.Corners.All(c => Eq(c, parameters.SolidTerrain)));

            if (hasAnyDoorway)
            {
                // A doorway edge implies a WallRoom; anything that isn't all-solid-cornered with at
                // least one opening facing outward is an unsupported shape for this pass (v1 scope is
                // WallRooms and OpenSetPieces only, per the verified .set inventory).
                if (!allCornersSolid || perimeterDoorways.Count == 0) return false;

                classified = new ClassifiedGroup
                {
                    Group = group,
                    Members = members,
                    Kind = GroupKind.WallRoom,
                    PerimeterDoorways = perimeterDoorways
                };
                return true;
            }

            // OpenSetPiece: every corner must be either solid or this layout's own open terrain —
            // groups whose "open" corner name doesn't match the current layout (e.g. vmr01's
            // Floor-cornered InteriorMosaic in a Plaza-terrain area) are structurally incompatible
            // here and skipped whole.
            foreach (var member in members)
            {
                foreach (var corner in member.Tile.Corners)
                {
                    if (!Eq(corner, parameters.SolidTerrain) && !Eq(corner, parameters.OpenTerrain))
                        return false;
                }
            }

            classified = new ClassifiedGroup
            {
                Group = group,
                Members = members,
                Kind = GroupKind.OpenSetPiece,
                PerimeterDoorways = perimeterDoorways
            };
            return true;
        }

        // ---------------- WallRoom ----------------

        private static bool TryPlaceWallRoom(
            MacroLayout layout, MacroLayoutParameters parameters, ClassifiedGroup classified,
            System.Random random, ref int nextRoomId)
        {
            var group = classified.Group;
            var width = layout.Corners.Width;
            var height = layout.Corners.Height;

            var anchors = new List<(int X, int Y)>();
            for (var ay = 0; ay <= height - group.Rows; ay++)
            for (var ax = 0; ax <= width - group.Columns; ax++)
                anchors.Add((ax, ay));

            Shuffle(anchors, random);

            var transitionTiles = new HashSet<(int X, int Y)>(layout.Transitions.Select(t => t.Tile));

            foreach (var anchor in anchors)
            {
                if (!IsWallRoomSiteValid(layout, parameters, classified, anchor, transitionTiles))
                    continue;

                StampWallRoom(layout, parameters, classified, anchor, ref nextRoomId);
                return true;
            }

            return false;
        }

        private static bool IsWallRoomSiteValid(
            MacroLayout layout, MacroLayoutParameters parameters, ClassifiedGroup classified,
            (int X, int Y) anchor, HashSet<(int X, int Y)> transitionTiles)
        {
            var group = classified.Group;
            var corners = layout.Corners;
            var crossers = layout.Crossers;
            var width = corners.Width;
            var height = corners.Height;

            for (var r = 0; r < group.Rows; r++)
            {
                for (var c = 0; c < group.Columns; c++)
                {
                    var cell = (X: anchor.X + c, Y: anchor.Y + r);

                    if (layout.PinnedTiles.ContainsKey(cell)) return false;
                    if (transitionTiles.Contains(cell)) return false;
                    if (!IsFullySolidCell(corners, cell, parameters.SolidTerrain)) return false;

                    for (var slot = 0; slot < 4; slot++)
                    {
                        if (crossers.GetEdge(cell.X, cell.Y, slot).Length != 0) return false;
                    }
                }
            }

            foreach (var (row, col, slot) in classified.PerimeterDoorways)
            {
                var cell = (X: anchor.X + col, Y: anchor.Y + row);
                var (dx, dy) = SlotOffsets[slot];
                var neighbor = (X: cell.X + dx, Y: cell.Y + dy);

                if (neighbor.X < 0 || neighbor.Y < 0 || neighbor.X >= width || neighbor.Y >= height) return false;

                var neighborHasCorridor = false;
                var neighborHasDoorway = false;
                for (var slot2 = 0; slot2 < 4; slot2++)
                {
                    var edge = crossers.GetEdge(neighbor.X, neighbor.Y, slot2);
                    if (Eq(edge, CorridorCrosser)) neighborHasCorridor = true;
                    if (Eq(edge, DoorwayCrosser)) neighborHasDoorway = true;
                }

                // Strict v1: every Doorway perimeter edge must face a plain tunnel-corridor cell.
                // Requiring it not already carry a Doorway keeps two different WallRoom instances from
                // ever claiming the same corridor adapter cell from different sides.
                if (!neighborHasCorridor || neighborHasDoorway) return false;
            }

            return true;
        }

        private static void StampWallRoom(
            MacroLayout layout, MacroLayoutParameters parameters, ClassifiedGroup classified,
            (int X, int Y) anchor, ref int nextRoomId)
        {
            var footprint = new List<(int X, int Y)>();

            foreach (var member in classified.Members)
            {
                var cell = (X: anchor.X + member.LocalCol, Y: anchor.Y + member.LocalRow);
                footprint.Add(cell);
                WriteMember(layout, parameters, member.Tile, cell);
            }

            layout.Rooms.Add(new LayoutRoom
            {
                Id = nextRoomId++,
                Role = RoomRole.Standard,
                IsSetPiece = true,
                CenterTile = footprint[0],
                Tiles = footprint
            });
        }

        // ---------------- OpenSetPiece ----------------

        private static bool TryPlaceOpenSetPiece(
            MacroLayout layout, MacroLayoutParameters parameters, ClassifiedGroup classified, System.Random random)
        {
            var group = classified.Group;

            var siteCandidates = new List<(LayoutRoom Room, (int X, int Y) Anchor)>();
            foreach (var room in layout.Rooms.Where(r => !r.IsSetPiece).OrderBy(r => r.Id))
            {
                foreach (var anchor in room.Tiles.OrderBy(t => t.Y).ThenBy(t => t.X))
                    siteCandidates.Add((room, anchor));
            }

            Shuffle(siteCandidates, random);

            foreach (var (room, anchor) in siteCandidates)
            {
                if (!IsOpenSetPieceSiteValid(layout, room, group, anchor, out var footprint))
                    continue;

                StampOpenSetPiece(layout, parameters, classified, room, footprint);
                return true;
            }

            return false;
        }

        private static bool IsOpenSetPieceSiteValid(
            MacroLayout layout, LayoutRoom room, TileGroupRecord group, (int X, int Y) anchor,
            out List<(int X, int Y)> footprint)
        {
            footprint = null;

            var roomTiles = new HashSet<(int X, int Y)>(room.Tiles);
            var transitionTiles = new HashSet<(int X, int Y)>(layout.Transitions.Select(t => t.Tile));

            var fp = new List<(int X, int Y)>();
            for (var r = 0; r < group.Rows; r++)
            for (var c = 0; c < group.Columns; c++)
                fp.Add((anchor.X + c, anchor.Y + r));

            // Footprint plus a 1-cell margin ring must sit entirely inside this same room's open
            // tiles, and touch neither the room's own path anchor nor any transition. This single pass
            // over the extended rectangle covers footprint and margin identically.
            for (var y = anchor.Y - 1; y <= anchor.Y + group.Rows; y++)
            {
                for (var x = anchor.X - 1; x <= anchor.X + group.Columns; x++)
                {
                    var cell = (X: x, Y: y);
                    if (!roomTiles.Contains(cell)) return false;
                    if (cell == room.CenterTile) return false;
                    if (transitionTiles.Contains(cell)) return false;
                    if (layout.PinnedTiles.ContainsKey(cell)) return false;
                }
            }

            footprint = fp;
            return true;
        }

        private static void StampOpenSetPiece(
            MacroLayout layout, MacroLayoutParameters parameters, ClassifiedGroup classified,
            LayoutRoom room, List<(int X, int Y)> footprint)
        {
            foreach (var member in classified.Members)
            {
                var cell = footprint[member.LocalRow * classified.Group.Columns + member.LocalCol];
                WriteMember(layout, parameters, member.Tile, cell);
            }

            var footprintSet = new HashSet<(int X, int Y)>(footprint);
            room.Tiles = room.Tiles.Where(t => !footprintSet.Contains(t)).ToList();
        }

        // ---------------- shared write helpers ----------------

        /// <summary>
        /// Writes one member tile's corners and all 4 edges into the shared grids at orientation 0,
        /// then pins the cell verbatim. Corner/edge writes for a multi-cell group's INTERIOR shared
        /// boundary may disagree between the two flanking members (the real zsf01 "Bedroom" group
        /// does — both members carry the same raw Doorway-on-Top edge data) but that is harmless: both
        /// flanking cells are pinned, so neither is ever read back via corner/edge key lookup. Only
        /// PERIMETER writes (read by an unpinned neighbor's own key) need to be correct, and those are
        /// exclusive per shared grid slot, so last-write-wins for interior slots never touches them.
        /// </summary>
        private static void WriteMember(MacroLayout layout, MacroLayoutParameters parameters, TileRecord tile, (int X, int Y) cell)
        {
            var tl = Canonicalize(tile.GetCornerAt(0, CornerSlot.TopLeft), parameters);
            var tr = Canonicalize(tile.GetCornerAt(0, CornerSlot.TopRight), parameters);
            var br = Canonicalize(tile.GetCornerAt(0, CornerSlot.BottomRight), parameters);
            var bl = Canonicalize(tile.GetCornerAt(0, CornerSlot.BottomLeft), parameters);

            layout.Corners.Labels[cell.X, cell.Y + 1] = tl;
            layout.Corners.Labels[cell.X + 1, cell.Y + 1] = tr;
            layout.Corners.Labels[cell.X + 1, cell.Y] = br;
            layout.Corners.Labels[cell.X, cell.Y] = bl;

            for (var slot = 0; slot < 4; slot++)
                layout.Crossers.SetEdge(cell.X, cell.Y, slot, tile.GetEdgeAt(0, slot));

            layout.PinnedTiles[cell] = (tile.TileId, 0);
        }

        /// <summary>Normalizes a .set corner label's casing to match the layout's own solid/open terrain strings exactly, so downstream exact-string comparisons never trip on a source file's casing quirks.</summary>
        private static string Canonicalize(string label, MacroLayoutParameters parameters)
        {
            if (Eq(label, parameters.SolidTerrain)) return parameters.SolidTerrain;
            if (Eq(label, parameters.OpenTerrain)) return parameters.OpenTerrain;
            return label;
        }

        private static bool IsFullySolidCell(CornerTerrainGrid corners, (int X, int Y) cell, string solidTerrain)
        {
            return Eq(corners.Labels[cell.X, cell.Y], solidTerrain) &&
                   Eq(corners.Labels[cell.X + 1, cell.Y], solidTerrain) &&
                   Eq(corners.Labels[cell.X, cell.Y + 1], solidTerrain) &&
                   Eq(corners.Labels[cell.X + 1, cell.Y + 1], solidTerrain);
        }

        private static bool Eq(string a, string b) => string.Equals(a ?? string.Empty, b ?? string.Empty, StringComparison.OrdinalIgnoreCase);

        private static void Shuffle<T>(List<T> list, System.Random random)
        {
            for (var i = list.Count - 1; i > 0; i--)
            {
                var j = random.Next(i + 1);
                (list[i], list[j]) = (list[j], list[i]);
            }
        }
    }
}
