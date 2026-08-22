#nullable disable
using System;
using System.Collections.Generic;
using System.Linq;

namespace SWLOR.Toolset.Domain.AreaGeneration.Layouts
{
    /// <summary>
    /// Shared transition-assignment post-pass: places EntranceCount arrival anchors and ExitCount
    /// outbound exit points on fully-open room tiles. The first entrance is always the Entrance
    /// room's center (the primary arrival anchor, preserving single-entrance behavior). Additional
    /// transitions prefer distinct, geodesically far-apart rooms; boss rooms may host exits but
    /// never entrances. Runs after accent painting so tile openness is final, and before
    /// LayoutGroupStamper in MacroLayoutGenerator.Generate, so set-piece rooms do not exist yet and
    /// are never picked here (LayoutGroupStamper itself avoids landing a set piece on an already-
    /// assigned transition tile).
    /// </summary>
    internal static class LayoutTransitionAssignment
    {
        private const int MaxPerKind = 3;

        internal static void AssignTransitions(MacroLayout layout, MacroLayoutParameters parameters, System.Random random)
        {
            layout.Transitions.Clear();

            var entranceCount = Math.Clamp(parameters.EntranceCount, 1, MaxPerKind);
            var exitCount = Math.Clamp(parameters.ExitCount, 1, MaxPerKind);

            var entranceRoom = layout.Rooms.First(r => r.Role == RoomRole.Entrance);
            var usedTiles = new HashSet<(int X, int Y)>();

            AddTransition(layout, TransitionKind.Entrance, entranceRoom, entranceRoom.CenterTile, usedTiles);

            var entranceRooms = new List<LayoutRoom> { entranceRoom };
            for (var i = 1; i < entranceCount; i++)
            {
                var room = PickRoom(layout, usedRooms: entranceRooms, allowBoss: false, random);
                entranceRooms.Add(room);
                AddTransition(layout, TransitionKind.Entrance, room, PickTile(room, usedTiles, random), usedTiles);
            }

            // Exits prefer rooms that host no entrance so leaving means traversing the layout;
            // boss rooms are allowed (defeat the boss, take its exit).
            var exitRooms = new List<LayoutRoom>(entranceRooms);
            for (var i = 0; i < exitCount; i++)
            {
                var room = PickRoom(layout, usedRooms: exitRooms, allowBoss: true, random);
                exitRooms.Add(room);
                AddTransition(layout, TransitionKind.Exit, room, PickTile(room, usedTiles, random), usedTiles);
            }
        }

        private static void AddTransition(MacroLayout layout, TransitionKind kind, LayoutRoom room, (int X, int Y) tile, HashSet<(int X, int Y)> usedTiles)
        {
            usedTiles.Add(tile);
            layout.Transitions.Add(new TransitionPoint
            {
                Kind = kind,
                Tile = tile,
                RoomId = room.Id
            });
        }

        /// <summary>
        /// Picks the room whose center is farthest (geodesically) from every already-used room,
        /// excluding used rooms while unused ones remain. Falls back to reusing rooms when the
        /// layout has fewer rooms than requested transitions.
        /// </summary>
        private static LayoutRoom PickRoom(MacroLayout layout, List<LayoutRoom> usedRooms, bool allowBoss, System.Random random)
        {
            var candidates = layout.Rooms
                .Where(r => r.Tiles.Count > 0)
                .Where(r => allowBoss || r.Role != RoomRole.Boss)
                .Where(r => !usedRooms.Contains(r))
                .ToList();

            if (candidates.Count == 0)
            {
                candidates = layout.Rooms
                    .Where(r => r.Tiles.Count > 0)
                    .Where(r => allowBoss || r.Role != RoomRole.Boss)
                    .ToList();
            }

            if (candidates.Count == 0)
                candidates = layout.Rooms.Where(r => r.Tiles.Count > 0).ToList();

            LayoutRoom best = null;
            var bestScore = -1;
            foreach (var candidate in candidates)
            {
                var score = usedRooms.Count == 0
                    ? random.Next(1000)
                    : usedRooms.Min(u => ManhattanDistance(u.CenterTile, candidate.CenterTile));

                if (score > bestScore)
                {
                    bestScore = score;
                    best = candidate;
                }
            }

            return best;
        }

        private static (int X, int Y) PickTile(LayoutRoom room, HashSet<(int X, int Y)> usedTiles, System.Random random)
        {
            var free = room.Tiles.Where(t => !usedTiles.Contains(t)).ToList();
            if (free.Count == 0)
                return room.CenterTile;

            // Prefer a tile away from the room center so transitions do not collide with
            // boss/treasure placement, which anchors on the center.
            var best = free[0];
            var bestDist = -1;
            foreach (var tile in free)
            {
                var d = ManhattanDistance(tile, room.CenterTile);
                if (d > bestDist)
                {
                    bestDist = d;
                    best = tile;
                }
            }

            return best;
        }

        private static int ManhattanDistance((int X, int Y) a, (int X, int Y) b)
        {
            return Math.Abs(a.X - b.X) + Math.Abs(a.Y - b.Y);
        }
    }
}
