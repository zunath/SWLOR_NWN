using System;

namespace SWLOR.Game.Server.Service.AreaGenerationService.Layouts
{
    /// <summary>
    /// Shared role-assignment post-pass: Entrance is a random room, Boss is whichever other room's
    /// center is geodesically farthest from the entrance (BFS over open corners), the rest Standard.
    /// Runs identically regardless of which style produced the rooms. Runs before LayoutGroupStamper
    /// in MacroLayoutGenerator.Generate, so it only ever sees the style's own rooms — set-piece rooms
    /// do not exist yet and never receive Entrance/Boss roles.
    /// </summary>
    internal static class LayoutRoleAssignment
    {
        internal static void AssignRoles(MacroLayout layout, MacroLayoutParameters parameters, System.Random random)
        {
            var rooms = layout.Rooms;
            if (rooms.Count == 0) return;

            foreach (var room in rooms)
                room.Role = RoomRole.Standard;

            var entranceIndex = random.Next(rooms.Count);
            rooms[entranceIndex].Role = RoomRole.Entrance;

            if (rooms.Count < 2) return;

            var entranceCorner = rooms[entranceIndex].CenterTile;
            var distances = LayoutCornerUtils.DistancesWithLinks(
                layout.Corners, parameters.OpenTerrain, entranceCorner, layout.TunnelLinks);

            var bossIndex = -1;
            var bestDist = -1;

            for (var i = 0; i < rooms.Count; i++)
            {
                if (i == entranceIndex) continue;

                var corner = rooms[i].CenterTile;
                var d = distances.TryGetValue(corner, out var dist) ? dist : -1;

                if (d > bestDist)
                {
                    bestDist = d;
                    bossIndex = i;
                }
            }

            if (bossIndex != -1)
                rooms[bossIndex].Role = RoomRole.Boss;
        }
    }
}
