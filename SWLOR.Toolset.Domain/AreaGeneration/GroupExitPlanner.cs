#nullable disable
using System;
using System.Collections.Generic;
using System.Linq;
using Serilog;
using SWLOR.Toolset.Domain.AreaGeneration.Tileset;

namespace SWLOR.Toolset.Domain.AreaGeneration
{
    /// <summary>
    /// Opportunistically substitutes a themed 1x1 tileset "exit" group tile (a wall-embedded doorway
    /// with an outward-facing threshold and a door slot, e.g. tdt01's Exit01-03 — no edge crossers of
    /// its own) for an Exit-kind transition's placeable, trying the tileset's configured
    /// DungeonTilesetProfile.ExitGroups in priority order. Entrances never receive a group exit
    /// (arrival anchors stay plain placeables/doors). Runs inside TileResolver.TryResolve BEFORE
    /// TileDoorPlanner, so group exits get first pick of a room's wall cells; any Exit transition this
    /// pass can't place falls through unchanged to TileDoorPlanner (a real generic door) and then
    /// plain Placeable.
    ///
    /// Placement mirrors TileDoorPlanner's own room-wall candidate search (a ring of cells one step
    /// outside each room's open interior, ordered by distance from the transition's original tile),
    /// but needs only a single cell per placement: exit-group tiles carry no crosser edges at all
    /// (unlike TileDoorPlanner's room-edge/terminator pair), so the tile's own corners — matched at
    /// whichever of its four orientations lines up with the corner grid already fixed at that cell —
    /// are the only structural requirement. The placed cell is pinned into MacroLayout.PinnedTiles,
    /// which TileDoorPlanner's existing pinned-cell guard already refuses to touch, so the only extra
    /// coordination needed between the two passes is TileDoorPlanner skipping transitions already
    /// GroupExit style (done there).
    ///
    /// No RNG is used — candidate order is by (Manhattan distance from the transition's original
    /// tile, then grid index), and the first group/cell/orientation/door-slot match that faces the
    /// room wins, so results are fully deterministic for a given resolved grid.
    /// </summary>
    internal sealed class GroupExitPlanner
    {
        private static readonly ILogger Logger = Log.ForContext<GroupExitPlanner>();

        private GroupExitPlanner()
        {
        }

        // Slot -> (Dx, Dy) step from a room-interior tile to its wall-cell neighbor across that edge.
        // Matches EdgeSlot's Top=0/Right=1/Bottom=2/Left=3 ordering.
        private static readonly (int Dx, int Dy)[] Directions = { (0, 1), (1, 0), (0, -1), (-1, 0) };

        private sealed class ExitGroupCandidate
        {
            public TileRecord Tile;
        }

        internal static void ApplyGroupExits(TilesetModel tileset, MacroLayout layout, ResolvedTile[] tiles, int width, int height)
        {
            if (layout.ExitGroups == null || layout.ExitGroups.Count == 0)
                return;

            var candidateGroups = BuildCandidateGroups(tileset, layout.ExitGroups);
            if (candidateGroups.Count == 0)
            {
                Logger.Information(
                    "Tileset {TilesetResref} has no usable configured exit-group candidates; " +
                    "exit transitions will continue to generated-door or placeable fallbacks.",
                    tileset.Resref);
                return;
            }

            var roomsById = layout.Rooms.ToDictionary(r => r.Id);

            // Every transition's originally-assigned tile is reserved up front, mirroring
            // TileDoorPlanner's own claim bookkeeping so the two passes never fight over a cell.
            var claimed = new HashSet<(int X, int Y)>(layout.Transitions.Select(t => t.Tile));

            foreach (var transition in layout.Transitions)
            {
                if (transition.Kind != TransitionKind.Exit)
                    continue; // entrances stay plain arrival anchors, never a group exit

                var originalTile = transition.Tile;
                claimed.Remove(originalTile);

                if (!roomsById.TryGetValue(transition.RoomId, out var room) || room.Tiles.Count == 0)
                {
                    Logger.Information(
                        "Exit transition for missing or empty room {RoomId} in tileset {TilesetResref} " +
                        "cannot use a configured exit group and will continue to fallback handling.",
                        transition.RoomId,
                        tileset.Resref);
                    claimed.Add(originalTile);
                    continue;
                }

                var placed = TryPlaceGroupExit(
                    tileset, layout, tiles, width, height, room, originalTile, candidateGroups, claimed,
                    out var cell, out var innerTile, out var tileId, out var orientation,
                    out var doorX, out var doorY, out var doorZ, out var doorOrientation,
                    out var doorType);

                if (placed)
                {
                    layout.PinnedTiles[cell] = (tileId, orientation, 0);
                    tiles[cell.Y * width + cell.X] = new ResolvedTile
                    {
                        TileId = tileId,
                        Orientation = orientation,
                        Height = 0
                    };

                    transition.Style = TransitionStyle.GroupExit;
                    transition.Tile = innerTile;
                    transition.DoorCell = cell;
                    transition.DoorwayCell = cell;
                    transition.DoorX = doorX;
                    transition.DoorY = doorY;
                    transition.DoorZ = doorZ;
                    transition.DoorOrientation = doorOrientation;
                    transition.DoorType = doorType;

                    claimed.Add(cell);
                    claimed.Add(innerTile);
                }
                else
                {
                    Logger.Information(
                        "Exit transition for room {RoomId} in tileset {TilesetResref} has no compatible " +
                        "exit-group placement and will continue to generated-door or placeable fallback handling.",
                        transition.RoomId,
                        tileset.Resref);
                    claimed.Add(originalTile);
                }
            }
        }

        /// <summary>
        /// Resolves and structurally re-verifies each configured exit-group name against real
        /// tileset data (1x1, flat, crosser-free, has a door slot) rather than trusting the tileset
        /// profile's list blindly. Preserves the configured priority order; unresolvable/ineligible
        /// names are silently dropped.
        /// </summary>
        private static List<ExitGroupCandidate> BuildCandidateGroups(TilesetModel tileset, List<string> exitGroupNames)
        {
            var result = new List<ExitGroupCandidate>();

            foreach (var name in exitGroupNames)
            {
                TileGroupRecord group = null;
                foreach (var candidate in tileset.Groups)
                {
                    if (string.Equals(candidate.Name, name, StringComparison.OrdinalIgnoreCase))
                    {
                        group = candidate;
                        break;
                    }
                }

                if (group == null)
                {
                    LogUnavailableExitGroup(tileset, name, "the group name was not found");
                    continue;
                }
                if (group.Rows != 1 || group.Columns != 1 || group.TileIds.Count != 1)
                {
                    LogUnavailableExitGroup(tileset, name, "the group is not exactly one tile");
                    continue;
                }

                var tileId = group.TileIds[0];
                if (tileId < 0 || tileId >= tileset.Tiles.Count)
                {
                    LogUnavailableExitGroup(tileset, name, "its tile ID is outside the tileset");
                    continue;
                }

                var tile = tileset.Tiles[tileId];
                if (tile.CornerHeights[0] != 0 || tile.CornerHeights[1] != 0 ||
                    tile.CornerHeights[2] != 0 || tile.CornerHeights[3] != 0)
                {
                    LogUnavailableExitGroup(tileset, name, "its tile is raised");
                    continue;
                }
                if (tile.Doors.Count == 0)
                {
                    LogUnavailableExitGroup(tileset, name, "its tile has no door slot");
                    continue;
                }
                if (tile.HasAnyCrosser)
                {
                    LogUnavailableExitGroup(tileset, name, "its tile has edge crossers");
                    continue;
                }

                result.Add(new ExitGroupCandidate { Tile = tile });
            }

            return result;
        }

        private static void LogUnavailableExitGroup(TilesetModel tileset, string groupName, string reason)
        {
            Logger.Information(
                "Configured exit group {ExitGroupName} is unavailable in tileset {TilesetResref}: {Reason}.",
                groupName,
                tileset.Resref,
                reason);
        }

        private static bool TryPlaceGroupExit(
            TilesetModel tileset, MacroLayout layout, ResolvedTile[] tiles,
            int width, int height, LayoutRoom room, (int X, int Y) originalTile,
            List<ExitGroupCandidate> candidateGroups, HashSet<(int X, int Y)> claimed,
            out (int X, int Y) cell, out (int X, int Y) innerTile, out int tileId, out int orientation,
            out float doorX, out float doorY, out float doorZ, out float doorOrientation,
            out int doorType)
        {
            cell = default;
            innerTile = default;
            tileId = 0;
            orientation = 0;
            doorX = doorY = doorZ = doorOrientation = 0f;
            doorType = 0;

            // room.Tiles only lists fully-open interior cells; the wall itself (where the exit group
            // tile would go) is the ring of cells one step outside that set — the same enumeration
            // approach TileDoorPlanner uses for its own room-edge candidates.
            var roomTileSet = new HashSet<(int X, int Y)>(room.Tiles);
            var seenCells = new HashSet<(int X, int Y)>();
            var wallCandidates = new List<((int X, int Y) Cell, (int X, int Y) InnerTile)>();

            foreach (var inner in room.Tiles)
            {
                // The inner tile becomes the transition's walkable anchor (waypoints, arrival
                // jumps). Resolution may have sprinkled a feature tile there (treasure mound,
                // pillar) whose art occupies the tile center — skip those so anchors stay on
                // plain floor.
                var innerResolved = tiles[inner.Y * width + inner.X];
                if (tileset.Tiles[innerResolved.TileId].GroupIndex != -1)
                    continue;

                foreach (var (dx, dy) in Directions)
                {
                    var wallCell = (X: inner.X + dx, Y: inner.Y + dy);
                    if (wallCell.X < 0 || wallCell.Y < 0 || wallCell.X >= width || wallCell.Y >= height)
                        continue;
                    if (roomTileSet.Contains(wallCell))
                        continue; // still inside the room's open interior, not a wall cell
                    if (!seenCells.Add(wallCell))
                        continue; // first (nearest) inner tile that reaches this wall cell wins

                    wallCandidates.Add((wallCell, inner));
                }
            }

            var ordered = wallCandidates
                .Where(c => !claimed.Contains(c.Cell) && !claimed.Contains(c.InnerTile))
                .Where(c => !layout.PinnedTiles.ContainsKey(c.Cell))
                .Where(c => !TileDoorGeometry.HasAnyCrosserEdge(layout.Crossers, c.Cell))
                // Feature-group cells were resolved before this pass and may drive later
                // feature-specific decoration planning. Never replace their art while leaving that
                // bookkeeping behind.
                .Where(c => tileset.Tiles[tiles[c.Cell.Y * width + c.Cell.X].TileId].GroupIndex == -1)
                // Defensive: no layout style paints CornerTerrainGrid.Heights yet, so this is always
                // true today, but a raised cell can never structurally match this planner's flat-only
                // exit-group candidates (see BuildCandidateGroups).
                .Where(c => TileDoorGeometry.IsFlatCell(layout.Corners, c.InnerTile.X, c.InnerTile.Y) &&
                            TileDoorGeometry.IsFlatCell(layout.Corners, c.Cell.X, c.Cell.Y))
                .OrderBy(c => ManhattanDistance(c.Cell, originalTile))
                .ThenBy(c => c.Cell.Y * width + c.Cell.X)
                .ToList();

            foreach (var group in candidateGroups)
            {
                foreach (var (candidateCell, inner) in ordered)
                {
                    if (claimed.Contains(candidateCell) || claimed.Contains(inner) || layout.PinnedTiles.ContainsKey(candidateCell))
                        continue; // an earlier group in this same search may have just claimed it

                    var (tl, tr, br, bl) = TileDoorGeometry.CellCorners(layout.Corners, candidateCell.X, candidateCell.Y);

                    for (var o = 0; o < 4; o++)
                    {
                        if (!TileDoorGeometry.Eq(group.Tile.GetCornerAt(o, CornerSlot.TopLeft), tl)) continue;
                        if (!TileDoorGeometry.Eq(group.Tile.GetCornerAt(o, CornerSlot.TopRight), tr)) continue;
                        if (!TileDoorGeometry.Eq(group.Tile.GetCornerAt(o, CornerSlot.BottomRight), br)) continue;
                        if (!TileDoorGeometry.Eq(group.Tile.GetCornerAt(o, CornerSlot.BottomLeft), bl)) continue;

                        var slot = FindDoorSlotFacingInner(group.Tile, o, candidateCell, inner);
                        if (slot == null)
                            continue;
                        var (wx, wy, wz, worientation) = TileDoorGeometry.DoorWorldTransform(slot, candidateCell.X, candidateCell.Y, o);

                        cell = candidateCell;
                        innerTile = inner;
                        tileId = group.Tile.TileId;
                        orientation = o;
                        doorX = wx;
                        doorY = wy;
                        doorZ = wz;
                        doorOrientation = worientation;
                        doorType = slot.Type;
                        return true;
                    }
                }
            }

            return false;
        }

        private static TileDoorRecord FindDoorSlotFacingInner(
            TileRecord tile,
            int orientation,
            (int X, int Y) cell,
            (int X, int Y) inner)
        {
            const float tolerance = 1.5f;
            var dx = inner.X - cell.X;
            var dy = inner.Y - cell.Y;

            foreach (var slot in tile.Doors)
            {
                var (rx, ry) = TileDoorGeometry.RotateCcw90Multiple(slot.X, slot.Y, orientation);
                if ((dx < 0 && rx <= -5f + tolerance) ||
                    (dx > 0 && rx >= 5f - tolerance) ||
                    (dy < 0 && ry <= -5f + tolerance) ||
                    (dy > 0 && ry >= 5f - tolerance))
                {
                    return slot;
                }
            }

            return null;
        }

        private static int ManhattanDistance((int X, int Y) a, (int X, int Y) b)
        {
            return Math.Abs(a.X - b.X) + Math.Abs(a.Y - b.Y);
        }
    }
}
