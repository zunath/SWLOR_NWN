#nullable disable
using System;
using System.Collections.Generic;
using System.Linq;
using SWLOR.Toolset.Domain.AreaGeneration.Tileset;

namespace SWLOR.Toolset.Domain.AreaGeneration
{
    /// <summary>
    /// Opportunistically substitutes a real tileset door for each transition's placeable-style
    /// assignment: a "room-edge" tile embedded in the room's wall (a doorway crosser cut through an
    /// otherwise-solid edge) paired with an adjoining "terminator" tile on the solid side (an
    /// all-solid-corner tile whose matching Doorway edge faces back at the room). Both tiles must be
    /// flat and ungrouped, matching the corner-matching resolver's v1 scope, and their facing edges
    /// must both carry the "Doorway" crosser — TileResolver's base pass never selects tiles with door
    /// slots (see its Doors.Count filter), so a distinct planner pass has to open the wall back up.
    ///
    /// Substitution is per-transition and fails soft: when no matching room-edge/terminator pair
    /// exists near a transition's tile (or none exists at all in the tileset, e.g. zsf01), that
    /// transition keeps its original Placeable style and Tile untouched. No RNG is used — candidate
    /// order is by (Manhattan distance from the transition's original tile, then grid index), and the
    /// first door-tile/orientation match for a given corner+facing combination wins, so results are
    /// fully deterministic for a given resolved grid.
    /// </summary>
    internal static class TileDoorPlanner
    {
        private const string DoorwayCrosser = "Doorway";

        // Facing direction to step from a room-edge cell into its solid neighbor, paired with the
        // edge slot on the room-edge tile (facing d) and the opposite edge slot on the neighbor
        // (facing back at the room). Order is the deterministic direction-search order.
        private static readonly (int Dx, int Dy, int EdgeFromCell, int EdgeBack)[] Directions =
        {
            (0, 1, EdgeSlot.Top, EdgeSlot.Bottom),
            (1, 0, EdgeSlot.Right, EdgeSlot.Left),
            (0, -1, EdgeSlot.Bottom, EdgeSlot.Top),
            (-1, 0, EdgeSlot.Left, EdgeSlot.Right),
        };

        internal static void ApplyDoorTransitions(TilesetModel tileset, MacroLayout layout, ResolvedTile[] tiles, int width, int height)
        {
            var roomsById = layout.Rooms.ToDictionary(r => r.Id);
            var edgeCandidates = BuildEdgeCandidates(tileset);
            var terminatorCandidates = BuildTerminatorCandidates(tileset);

            if (edgeCandidates.Count == 0 || terminatorCandidates.Count == 0)
                return; // e.g. zsf01: no usable door tiles at all — every transition stays Placeable.

            // Every transition's originally-assigned tile is reserved up front so no transition can
            // steal another's anchor as its own door spot; a transition's own tile is un-reserved
            // just before its own search so it may reuse (and is preferred to reuse) that same cell.
            var claimed = new HashSet<(int X, int Y)>(layout.Transitions.Select(t => t.Tile));

            foreach (var transition in layout.Transitions)
            {
                // GroupExitPlanner already finalized this transition (a themed exit-group tile pinned
                // and claimed); its Tile stays reserved via the initial `claimed` snapshot above, and
                // its pinned cell is separately guarded below wherever a candidate is pinned.
                if (transition.Style == TransitionStyle.GroupExit)
                    continue;

                var originalTile = transition.Tile;
                claimed.Remove(originalTile);

                if (!roomsById.TryGetValue(transition.RoomId, out var room) || room.Tiles.Count == 0)
                {
                    claimed.Add(originalTile);
                    continue;
                }

                var placed = TryPlaceDoor(
                    tileset, layout, tiles, width, height, room, originalTile,
                    edgeCandidates, terminatorCandidates, claimed,
                    out var roomSideCell, out var roomEdgeCell, out var solidCell,
                    out var doorX, out var doorY, out var doorZ, out var doorOrientation,
                    out var doorType);

                if (placed)
                {
                    transition.Style = TransitionStyle.Door;
                    // The anchor stays on open room floor in front of the doorway — waypoints,
                    // arrival jumps, and preview chevrons all use Tile and must never sit inside
                    // the doorway wall tile itself.
                    transition.Tile = roomSideCell;
                    transition.DoorwayCell = roomEdgeCell;
                    transition.DoorCell = solidCell;
                    transition.DoorX = doorX;
                    transition.DoorY = doorY;
                    transition.DoorZ = doorZ;
                    transition.DoorOrientation = doorOrientation;
                    transition.DoorType = doorType;

                    claimed.Add(roomSideCell);
                    claimed.Add(roomEdgeCell);
                    claimed.Add(solidCell);
                }
                else
                {
                    claimed.Add(originalTile);
                }
            }
        }

        private static bool TryPlaceDoor(
            TilesetModel tileset,
            MacroLayout layout,
            ResolvedTile[] tiles,
            int width,
            int height,
            LayoutRoom room,
            (int X, int Y) originalTile,
            Dictionary<(string CornerKey, int EdgeFromCell), List<(int TileId, int Orientation)>> edgeCandidates,
            Dictionary<int, List<(int TileId, int Orientation)>> terminatorCandidates,
            HashSet<(int X, int Y)> claimed,
            out (int X, int Y) roomSideCell,
            out (int X, int Y) roomEdgeCell,
            out (int X, int Y) solidCell,
            out float doorX, out float doorY, out float doorZ, out float doorOrientation,
            out int doorType)
        {
            roomSideCell = default;
            roomEdgeCell = default;
            solidCell = default;
            doorX = doorY = doorZ = doorOrientation = 0f;
            doorType = 0;

            // room.Tiles only lists fully-open (all-4-corner-Floor) interior cells — the wall itself,
            // where a door tile would actually go, is the ring of cells one step outside that set (its
            // corners are a Floor/Wall mix: open on the room-facing side, solid on the far side). So
            // candidates are enumerated as (innerTile + direction), not innerTile itself.
            var candidates = new List<((int X, int Y) InnerTile, (int X, int Y) RoomEdgeCell, (int X, int Y) SolidCell, int EdgeFromCell, int EdgeBack)>();
            var roomTileSet = new HashSet<(int X, int Y)>(room.Tiles);

            foreach (var innerTile in room.Tiles)
            {
                // The inner tile becomes the transition's walkable anchor (waypoints, arrival
                // jumps). Resolution may have sprinkled a feature tile there (treasure mound,
                // pillar) whose art occupies the tile center — skip those so anchors stay on
                // plain floor.
                var innerResolved = tiles[innerTile.Y * width + innerTile.X];
                if (tileset.Tiles[innerResolved.TileId].GroupIndex != -1)
                    continue;

                foreach (var (dx, dy, edgeFromCell, edgeBack) in Directions)
                {
                    var roomEdge = (X: innerTile.X + dx, Y: innerTile.Y + dy);
                    if (roomEdge.X < 0 || roomEdge.Y < 0 || roomEdge.X >= width || roomEdge.Y >= height)
                        continue;
                    if (roomTileSet.Contains(roomEdge))
                        continue; // still inside the room's open interior, not a wall cell

                    var solid = (X: roomEdge.X + dx, Y: roomEdge.Y + dy);
                    if (solid.X < 0 || solid.Y < 0 || solid.X >= width || solid.Y >= height)
                        continue;

                    // Cells carrying tunnel crossers already resolved to corridor/doorway tiles whose
                    // edges their neighbors depend on; substituting a transition door there would
                    // sever the tunnel and break edge agreement.
                    if (TileDoorGeometry.HasAnyCrosserEdge(layout.Crossers, roomEdge) || TileDoorGeometry.HasAnyCrosserEdge(layout.Crossers, solid))
                        continue;

                    // LayoutGroupStamper-pinned cells are placed verbatim by TileResolver; a
                    // transition door must never overwrite one.
                    if (layout.PinnedTiles.ContainsKey(roomEdge) || layout.PinnedTiles.ContainsKey(solid))
                        continue;

                    candidates.Add((innerTile, roomEdge, solid, edgeFromCell, edgeBack));
                }
            }

            var orderedCandidates = candidates
                .Where(c => !claimed.Contains(c.RoomEdgeCell) && !claimed.Contains(c.SolidCell))
                // Defensive: no layout style paints CornerTerrainGrid.Heights yet, so this is always
                // true today, but a raised cell can never structurally match this planner's flat-only
                // door/terminator candidate pools (see BuildEdgeCandidates/BuildTerminatorCandidates).
                .Where(c => TileDoorGeometry.IsFlatCell(layout.Corners, c.InnerTile.X, c.InnerTile.Y) &&
                            TileDoorGeometry.IsFlatCell(layout.Corners, c.RoomEdgeCell.X, c.RoomEdgeCell.Y) &&
                            TileDoorGeometry.IsFlatCell(layout.Corners, c.SolidCell.X, c.SolidCell.Y))
                .OrderBy(c => ManhattanDistance(c.RoomEdgeCell, originalTile))
                .ThenBy(c => c.RoomEdgeCell.Y * width + c.RoomEdgeCell.X)
                .ThenBy(c => c.EdgeFromCell)
                .ToList();

            foreach (var (innerTile, candidateCell, neighbor, edgeFromCell, edgeBack) in orderedCandidates)
            {
                if (claimed.Contains(candidateCell) || claimed.Contains(neighbor))
                    continue; // an earlier candidate in this same search may have just claimed it

                var (tl, tr, br, bl) = TileDoorGeometry.CellCorners(layout.Corners, candidateCell.X, candidateCell.Y);
                var cornerKey = MakeCornerKey(tl, tr, br, bl);

                var (ntl, ntr, nbr, nbl) = TileDoorGeometry.CellCorners(layout.Corners, neighbor.X, neighbor.Y);
                if (!IsAllSolid(ntl, ntr, nbr, nbl, tileset.DefaultTerrain))
                    continue;

                if (!edgeCandidates.TryGetValue((cornerKey, edgeFromCell), out var edgePicks) || edgePicks.Count == 0)
                    continue;
                if (!terminatorCandidates.TryGetValue(edgeBack, out var termPicks) || termPicks.Count == 0)
                    continue;

                var edgePick = edgePicks[0];
                var termPick = termPicks[0];

                var edgeTile = tileset.Tiles[edgePick.TileId];
                var slot = FindDoorSlotOnEdge(edgeTile, edgePick.Orientation, edgeFromCell);
                if (slot == null)
                    continue;

                tiles[candidateCell.Y * width + candidateCell.X] = new ResolvedTile
                {
                    TileId = edgePick.TileId,
                    Orientation = edgePick.Orientation,
                    Height = 0
                };
                tiles[neighbor.Y * width + neighbor.X] = new ResolvedTile
                {
                    TileId = termPick.TileId,
                    Orientation = termPick.Orientation,
                    Height = 0
                };

                var (wx, wy, wz, worientation) = TileDoorGeometry.DoorWorldTransform(slot, candidateCell.X, candidateCell.Y, edgePick.Orientation);
                roomSideCell = innerTile;
                roomEdgeCell = candidateCell;
                solidCell = neighbor;
                doorX = wx;
                doorY = wy;
                doorZ = wz;
                doorOrientation = worientation;
                doorType = slot.Type;
                return true;
            }

            return false;
        }

        /// <summary>
        /// Room-edge candidates: flat, ungrouped, door-bearing tiles with exactly one rotated
        /// "Doorway" edge crosser (the other three blank), keyed by (rotated corner signature, world
        /// edge slot the Doorway faces). Sorted by (TileId, Orientation) so the first entry is
        /// deterministic.
        /// </summary>
        private static Dictionary<(string CornerKey, int EdgeFromCell), List<(int TileId, int Orientation)>> BuildEdgeCandidates(TilesetModel tileset)
        {
            var lookup = new Dictionary<(string, int), List<(int, int)>>();

            foreach (var tile in tileset.Tiles)
            {
                if (tile.GroupIndex != -1) continue;
                if (tile.Doors.Count == 0) continue;
                if (tile.CornerHeights[0] != 0 || tile.CornerHeights[1] != 0 ||
                    tile.CornerHeights[2] != 0 || tile.CornerHeights[3] != 0) continue;

                for (var orientation = 0; orientation < 4; orientation++)
                {
                    if (!TryGetSingleDoorwaySlot(tile, orientation, out var doorwaySlot)) continue;

                    var cornerKey = MakeCornerKey(
                        tile.GetCornerAt(orientation, CornerSlot.TopLeft),
                        tile.GetCornerAt(orientation, CornerSlot.TopRight),
                        tile.GetCornerAt(orientation, CornerSlot.BottomRight),
                        tile.GetCornerAt(orientation, CornerSlot.BottomLeft));

                    var key = (cornerKey, doorwaySlot);
                    if (!lookup.TryGetValue(key, out var list))
                    {
                        list = new List<(int, int)>();
                        lookup[key] = list;
                    }

                    list.Add((tile.TileId, orientation));
                }
            }

            foreach (var list in lookup.Values)
                list.Sort((a, b) => a.Item1 != b.Item1 ? a.Item1.CompareTo(b.Item1) : a.Item2.CompareTo(b.Item2));

            return lookup;
        }

        /// <summary>
        /// Terminator candidates: flat, all-solid-corner tiles with exactly one rotated "Doorway" edge
        /// crosser (the other three blank), keyed by the world edge slot the Doorway faces. Sorted by
        /// (TileId, Orientation) for deterministic selection.
        ///
        /// Ungrouped tiles qualify directly; a tile wrapped in a trivial 1x1 [GROUPn] entry (e.g.
        /// tds01/vmr01 "Door_Trans", vmr01 "Door_Trans_Exterior" -- shape-identical to a plain
        /// terminator, just group-wrapped in the .set data) also qualifies -- see
        /// IsSingleCellGroup's doc comment for why this never races LayoutGroupStamper. Multi-cell
        /// groups stay excluded (BuildEdgeCandidates' room-edge pool is unaffected and stays
        /// ungrouped-only: a grouped room-edge tile would need LayoutGroupStamper's own site
        /// validation, not this planner's).
        /// </summary>
        private static Dictionary<int, List<(int TileId, int Orientation)>> BuildTerminatorCandidates(TilesetModel tileset)
        {
            var lookup = new Dictionary<int, List<(int, int)>>();

            foreach (var tile in tileset.Tiles)
            {
                if (tile.GroupIndex != -1 && !IsSingleCellGroup(tileset, tile.GroupIndex)) continue;
                if (tile.CornerHeights[0] != 0 || tile.CornerHeights[1] != 0 ||
                    tile.CornerHeights[2] != 0 || tile.CornerHeights[3] != 0) continue;

                for (var orientation = 0; orientation < 4; orientation++)
                {
                    if (!IsAllSolid(
                            tile.GetCornerAt(orientation, CornerSlot.TopLeft),
                            tile.GetCornerAt(orientation, CornerSlot.TopRight),
                            tile.GetCornerAt(orientation, CornerSlot.BottomRight),
                            tile.GetCornerAt(orientation, CornerSlot.BottomLeft),
                            tileset.DefaultTerrain))
                        continue;

                    if (!TryGetSingleDoorwaySlot(tile, orientation, out var doorwaySlot)) continue;

                    if (!lookup.TryGetValue(doorwaySlot, out var list))
                    {
                        list = new List<(int, int)>();
                        lookup[doorwaySlot] = list;
                    }

                    list.Add((tile.TileId, orientation));
                }
            }

            foreach (var list in lookup.Values)
                list.Sort((a, b) => a.Item1 != b.Item1 ? a.Item1.CompareTo(b.Item1) : a.Item2.CompareTo(b.Item2));

            return lookup;
        }

        /// <summary>True when exactly one of the tile's rotated edges is "Doorway" and the other three are blank.</summary>
        private static bool TryGetSingleDoorwaySlot(TileRecord tile, int orientation, out int doorwaySlot)
        {
            doorwaySlot = -1;
            var found = 0;

            for (var slot = 0; slot < 4; slot++)
            {
                var edge = tile.GetEdgeAt(orientation, slot);
                if (string.Equals(edge, DoorwayCrosser, StringComparison.OrdinalIgnoreCase))
                {
                    found++;
                    doorwaySlot = slot;
                }
                else if (!string.IsNullOrEmpty(edge))
                {
                    return false; // some other crosser present — not a plain doorway wall segment
                }
            }

            return found == 1;
        }

        /// <summary>
        /// Finds the door slot (in raw, unrotated tile-local coordinates) that lies on the given
        /// rotated world edge slot. If the tile has exactly one door slot, that slot is used directly
        /// (matching the common single-doorway tile shape); otherwise the slot whose rotated position
        /// lies on that edge (within tolerance) is picked.
        /// </summary>
        private static TileDoorRecord FindDoorSlotOnEdge(TileRecord tile, int orientation, int edgeSlot)
        {
            if (tile.Doors.Count == 1)
                return tile.Doors[0];

            const float tolerance = 1.5f;
            foreach (var door in tile.Doors)
            {
                var (rx, ry) = TileDoorGeometry.RotateCcw90Multiple(door.X, door.Y, orientation);
                var onEdge = edgeSlot switch
                {
                    EdgeSlot.Top => ry >= 5f - tolerance,
                    EdgeSlot.Right => rx >= 5f - tolerance,
                    EdgeSlot.Bottom => ry <= -5f + tolerance,
                    EdgeSlot.Left => rx <= -5f + tolerance,
                    _ => false
                };

                if (onEdge)
                    return door;
            }

            return null;
        }

        // Rotation, world-transform, corner-lookup, and crosser-check math shared with
        // GroupExitPlanner lives in TileDoorGeometry (see RotateCcw90Multiple/DoorWorldTransform/
        // CellCorners/HasAnyCrosserEdge/Eq call sites above).

        /// <summary>
        /// True when <paramref name="groupIndex"/> refers to a 1x1 tileset group -- a tile
        /// shape-identical to an ungrouped terminator candidate, just wrapped in a trivial single-cell
        /// [GROUPn] entry. LayoutGroupStamper's group inventory never contests these cells: no shipped
        /// StandardTilesetProfiles entry references tds01/vmr01's "Door_Trans"/"Door_Trans_Exterior" as
        /// a SetPiece, and even if one did, LayoutGroupStamper.TryClassify's WallRoom branch rejects any
        /// door-bearing member outright (its hasAnyDoor check) -- these tiles all carry a door slot, so
        /// they could never classify as a WallRoom set piece and would simply go unplaced by that path.
        /// Tolerating them here therefore never creates a placement race between the two passes.
        /// Multi-cell groups stay excluded -- this planner's terminator pool is strictly single-cell.
        /// </summary>
        private static bool IsSingleCellGroup(TilesetModel tileset, int groupIndex)
        {
            if (groupIndex < 0 || groupIndex >= tileset.Groups.Count) return false;
            var group = tileset.Groups[groupIndex];
            return group.Rows == 1 && group.Columns == 1 && group.TileIds.Count == 1;
        }

        private static bool IsAllSolid(string tl, string tr, string br, string bl, string solidTerrain)
        {
            return TileDoorGeometry.Eq(tl, solidTerrain) && TileDoorGeometry.Eq(tr, solidTerrain) &&
                   TileDoorGeometry.Eq(br, solidTerrain) && TileDoorGeometry.Eq(bl, solidTerrain);
        }

        private static string MakeCornerKey(string tl, string tr, string br, string bl)
        {
            return string.Join(
                "|",
                (tl ?? string.Empty).ToUpperInvariant(),
                (tr ?? string.Empty).ToUpperInvariant(),
                (br ?? string.Empty).ToUpperInvariant(),
                (bl ?? string.Empty).ToUpperInvariant());
        }

        private static int ManhattanDistance((int X, int Y) a, (int X, int Y) b)
        {
            return Math.Abs(a.X - b.X) + Math.Abs(a.Y - b.Y);
        }
    }
}
