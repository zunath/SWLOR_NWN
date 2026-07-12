using System;
using System.Collections.Generic;
using System.Linq;

namespace SWLOR.Game.Server.Service.AreaGenerationService
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
                    out var roomEdgeCell, out var solidCell, out var doorX, out var doorY, out var doorZ, out var doorOrientation);

                if (placed)
                {
                    transition.Style = TransitionStyle.Door;
                    transition.Tile = roomEdgeCell;
                    transition.DoorCell = solidCell;
                    transition.DoorX = doorX;
                    transition.DoorY = doorY;
                    transition.DoorZ = doorZ;
                    transition.DoorOrientation = doorOrientation;

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
            out (int X, int Y) roomEdgeCell,
            out (int X, int Y) solidCell,
            out float doorX, out float doorY, out float doorZ, out float doorOrientation)
        {
            roomEdgeCell = default;
            solidCell = default;
            doorX = doorY = doorZ = doorOrientation = 0f;

            // room.Tiles only lists fully-open (all-4-corner-Floor) interior cells — the wall itself,
            // where a door tile would actually go, is the ring of cells one step outside that set (its
            // corners are a Floor/Wall mix: open on the room-facing side, solid on the far side). So
            // candidates are enumerated as (innerTile + direction), not innerTile itself.
            var candidates = new List<((int X, int Y) RoomEdgeCell, (int X, int Y) SolidCell, int EdgeFromCell, int EdgeBack)>();
            var roomTileSet = new HashSet<(int X, int Y)>(room.Tiles);

            foreach (var innerTile in room.Tiles)
            {
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
                    if (HasAnyCrosserEdge(layout.Crossers, roomEdge) || HasAnyCrosserEdge(layout.Crossers, solid))
                        continue;

                    // LayoutGroupStamper-pinned cells are placed verbatim by TileResolver; a
                    // transition door must never overwrite one.
                    if (layout.PinnedTiles.ContainsKey(roomEdge) || layout.PinnedTiles.ContainsKey(solid))
                        continue;

                    candidates.Add((roomEdge, solid, edgeFromCell, edgeBack));
                }
            }

            var orderedCandidates = candidates
                .Where(c => !claimed.Contains(c.RoomEdgeCell) && !claimed.Contains(c.SolidCell))
                .OrderBy(c => ManhattanDistance(c.RoomEdgeCell, originalTile))
                .ThenBy(c => c.RoomEdgeCell.Y * width + c.RoomEdgeCell.X)
                .ThenBy(c => c.EdgeFromCell)
                .ToList();

            foreach (var (candidateCell, neighbor, edgeFromCell, edgeBack) in orderedCandidates)
            {
                if (claimed.Contains(candidateCell) || claimed.Contains(neighbor))
                    continue; // an earlier candidate in this same search may have just claimed it

                var (tl, tr, br, bl) = CellCorners(layout.Corners, candidateCell.X, candidateCell.Y);
                var cornerKey = MakeCornerKey(tl, tr, br, bl);

                var (ntl, ntr, nbr, nbl) = CellCorners(layout.Corners, neighbor.X, neighbor.Y);
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

                var (rx, ry) = RotateCcw90Multiple(slot.X, slot.Y, edgePick.Orientation);
                roomEdgeCell = candidateCell;
                solidCell = neighbor;
                doorX = candidateCell.X * 10f + 5f + rx;
                doorY = candidateCell.Y * 10f + 5f + ry;
                doorZ = slot.Z;
                doorOrientation = NormalizeDegrees(slot.Orientation + edgePick.Orientation * 90f);
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
        /// Terminator candidates: flat, ungrouped, all-solid-corner tiles with exactly one rotated
        /// "Doorway" edge crosser (the other three blank), keyed by the world edge slot the Doorway
        /// faces. Sorted by (TileId, Orientation) for deterministic selection.
        /// </summary>
        private static Dictionary<int, List<(int TileId, int Orientation)>> BuildTerminatorCandidates(TilesetModel tileset)
        {
            var lookup = new Dictionary<int, List<(int, int)>>();

            foreach (var tile in tileset.Tiles)
            {
                if (tile.GroupIndex != -1) continue;
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
                var (rx, ry) = RotateCcw90Multiple(door.X, door.Y, orientation);
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

        /// <summary>
        /// Rotates a tile-local (origin at tile center, range roughly [-5, 5]) point by
        /// orientation * 90 degrees counterclockwise, using exact swaps/negations (orientation is
        /// always a 90-degree multiple, so trig would only introduce needless floating-point error).
        /// Matches the world-transform empirically pinned against hand-built module doors: for a
        /// tile at grid cell (cx, cy) with this orientation, a raw door local (x, y) lands at world
        /// (cx*10 + 5 + rx, cy*10 + 5 + ry).
        /// </summary>
        private static (float X, float Y) RotateCcw90Multiple(float x, float y, int orientation)
        {
            return ((orientation % 4 + 4) % 4) switch
            {
                0 => (x, y),
                1 => (-y, x),
                2 => (-x, -y),
                3 => (y, -x),
                _ => (x, y)
            };
        }

        private static float NormalizeDegrees(float degrees)
        {
            var d = degrees % 360f;
            if (d > 180f) d -= 360f;
            if (d <= -180f) d += 360f;
            return d;
        }

        private static (string TL, string TR, string BR, string BL) CellCorners(CornerTerrainGrid corners, int x, int y)
        {
            return (
                corners.Labels[x, y + 1],
                corners.Labels[x + 1, y + 1],
                corners.Labels[x + 1, y],
                corners.Labels[x, y]);
        }

        private static bool IsAllSolid(string tl, string tr, string br, string bl, string solidTerrain)
        {
            return Eq(tl, solidTerrain) && Eq(tr, solidTerrain) && Eq(br, solidTerrain) && Eq(bl, solidTerrain);
        }

        private static bool Eq(string a, string b) => string.Equals(a ?? string.Empty, b ?? string.Empty, StringComparison.OrdinalIgnoreCase);

        private static bool HasAnyCrosserEdge(EdgeCrosserGrid crossers, (int X, int Y) cell)
        {
            for (var slot = 0; slot < 4; slot++)
            {
                if (crossers.GetEdge(cell.X, cell.Y, slot).Length != 0)
                    return true;
            }

            return false;
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
