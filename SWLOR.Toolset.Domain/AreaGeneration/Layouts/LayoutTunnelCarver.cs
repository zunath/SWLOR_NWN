#nullable disable
using System;
using System.Collections.Generic;
using SWLOR.Toolset.Domain.AreaGeneration.Tileset;

namespace SWLOR.Toolset.Domain.AreaGeneration.Layouts
{
    /// <summary>
    /// Connects two rooms with a wall-embedded tunnel: a chain of Corridor edge crossers through
    /// fully solid cells, entered and exited through Doorway crossers punched in the rooms' straight
    /// walls — the way hand-built facility interiors (czs220_maintlvl) are assembled, in contrast to
    /// the open-terrain corner lanes OpenLane corridors carve.
    ///
    /// Vocabulary the tunnel relies on (verified present in all four generation tilesets): all-solid
    /// tiles with Corridor edges in straight/L/T/X arrangements, and side-open tiles with a Doorway
    /// edge opposite the open side. Ports are therefore only punched through straight wall segments,
    /// with the doorway leading perpendicularly away from the room.
    /// </summary>
    internal static class LayoutTunnelCarver
    {
        private const string CorridorCrosser = "Corridor";
        private const string DoorwayCrosser = "Doorway";
        private const string AlleyCrosser = "Alley";

        /// <summary>
        /// A candidate doorway position on a room's wall: the boundary cell straddling the wall
        /// (open corners on the room side, solid on the other), the edge slot that receives the
        /// Doorway crosser, and the solid cell the tunnel starts from on the far side of that edge.
        /// </summary>
        private readonly struct Port
        {
            public Port((int X, int Y) boundaryCell, int doorwaySlot, (int X, int Y) tunnelCell, (int X, int Y) openCorner)
            {
                BoundaryCell = boundaryCell;
                DoorwaySlot = doorwaySlot;
                TunnelCell = tunnelCell;
                OpenCorner = openCorner;
            }

            public (int X, int Y) BoundaryCell { get; }
            public int DoorwaySlot { get; }
            public (int X, int Y) TunnelCell { get; }
            /// <summary>An open corner on the room side, used as the geodesic anchor for TunnelLinks.</summary>
            public (int X, int Y) OpenCorner { get; }
        }

        /// <summary>
        /// Carves a tunnel between two rooms. Returns true on success (edges labeled, TunnelLink
        /// recorded); false when no solid path exists between any port pair, in which case the caller
        /// should fall back to an open-lane corridor.
        ///
        /// <paramref name="roomAOpen"/>/<paramref name="roomBOpen"/> are each room's OWN carved
        /// terrain (equal to each other and to MacroLayoutParameters.OpenTerrain unless multi-terrain
        /// districts are active -- see MacroLayoutParameters.SecondaryOpenTerrain), so a room's ports
        /// are only found on ITS OWN open corners even when the two rooms are carved from different
        /// terrains.
        /// </summary>
        internal static bool TryConnect(
            MacroLayout layout,
            RoomRect roomA,
            RoomRect roomB,
            string roomAOpen,
            string roomBOpen,
            MacroLayoutParameters parameters,
            System.Random random)
        {
            var corners = layout.Corners;
            var crossers = layout.Crossers;

            // The solid tunnel chain must avoid EVERY district's open terrain, not just one room's own
            // -- otherwise a chain could wander through another district's already-carved interior.
            var allOpenLabels = LayoutCornerUtils.OpenLabelSet(parameters);

            // Alley mode carves vmr01's exterior alley crosser for both the tunnel body AND the room
            // port (verified offline: no separate Doorway-equivalent exists for Alley); Custom mode
            // carves whatever body/port pair the composed tileset profile declared (see
            // MacroLayoutParameters.TunnelBodyCrosser/TunnelPortCrosser -- a district-scoped crosser
            // family that is mechanically identical to Corridor/Doorway, just under different names);
            // Corridor mode (default) keeps the original two-crosser vocabulary.
            var bodyCrosser = parameters.CorridorCrosserType switch
            {
                CorridorCrosserType.Alley => AlleyCrosser,
                CorridorCrosserType.Custom => parameters.TunnelBodyCrosser,
                _ => CorridorCrosser
            };
            var portCrosser = parameters.CorridorCrosserType switch
            {
                CorridorCrosserType.Alley => AlleyCrosser,
                CorridorCrosserType.Custom => parameters.TunnelPortCrosser,
                _ => DoorwayCrosser
            };

            var portsA = EnumeratePorts(corners, crossers, roomA, roomAOpen, allOpenLabels);
            var portsB = EnumeratePorts(corners, crossers, roomB, roomBOpen, allOpenLabels);
            if (portsA.Count == 0 || portsB.Count == 0)
                return false;

            Shuffle(portsA, random);
            Shuffle(portsB, random);

            // Direct adjacency: room A's doorway opens straight into a port cell of room B
            // (rooms one wall apart). One shared Doorway edge links them with no tunnel cells.
            foreach (var a in portsA)
            {
                foreach (var b in portsB)
                {
                    if (a.TunnelCell != b.BoundaryCell || b.TunnelCell != a.BoundaryCell) continue;

                    crossers.SetEdge(a.BoundaryCell.X, a.BoundaryCell.Y, a.DoorwaySlot, portCrosser);
                    layout.TunnelLinks.Add(new TunnelLink { CornerA = a.OpenCorner, CornerB = b.OpenCorner, Length = 1 });
                    return true;
                }
            }

            // Multi-source BFS over fully solid cells from every A tunnel-start; the first B tunnel-
            // start reached wins. Fixed expansion order + seeded port shuffle keeps this deterministic.
            var goals = new Dictionary<(int X, int Y), Port>();
            foreach (var b in portsB)
            {
                if (IsSolidCell(corners, b.TunnelCell, allOpenLabels) && !goals.ContainsKey(b.TunnelCell))
                    goals[b.TunnelCell] = b;
            }
            if (goals.Count == 0)
                return false;

            var cameFrom = new Dictionary<(int X, int Y), (int X, int Y)>();
            var origin = new Dictionary<(int X, int Y), Port>();
            var queue = new Queue<(int X, int Y)>();

            foreach (var a in portsA)
            {
                if (!IsSolidCell(corners, a.TunnelCell, allOpenLabels) || origin.ContainsKey(a.TunnelCell)) continue;
                origin[a.TunnelCell] = a;
                cameFrom[a.TunnelCell] = a.TunnelCell;
                queue.Enqueue(a.TunnelCell);
            }
            if (queue.Count == 0)
                return false;

            (int X, int Y)? reachedGoal = null;
            while (queue.Count > 0 && reachedGoal == null)
            {
                var current = queue.Dequeue();
                if (goals.ContainsKey(current))
                {
                    reachedGoal = current;
                    break;
                }

                foreach (var (dx, dy) in LayoutCornerUtils.Ortho4)
                {
                    var next = (X: current.X + dx, Y: current.Y + dy);
                    if (next.X < 0 || next.Y < 0 || next.X >= corners.Width || next.Y >= corners.Height) continue;
                    if (!IsSolidCell(corners, next, allOpenLabels)) continue;
                    if (cameFrom.ContainsKey(next)) continue;

                    cameFrom[next] = current;
                    origin[next] = origin[current];
                    queue.Enqueue(next);
                }
            }

            if (reachedGoal == null)
                return false;

            // Reconstruct the solid-cell chain, then label: Doorway at both room walls, Corridor
            // along the chain. Corridor labels may overwrite/merge with earlier tunnels crossing the
            // same cells — junction tiles (T/X) cover that; Doorway ports were pre-filtered so a
            // boundary cell never carries two crossers.
            var goalPort = goals[reachedGoal.Value];
            var startPort = origin[reachedGoal.Value];

            var chain = new List<(int X, int Y)>();
            var walk = reachedGoal.Value;
            while (true)
            {
                chain.Add(walk);
                var prev = cameFrom[walk];
                if (prev == walk) break;
                walk = prev;
            }
            chain.Reverse(); // startPort.TunnelCell ... goalPort.TunnelCell

            crossers.SetEdge(startPort.BoundaryCell.X, startPort.BoundaryCell.Y, startPort.DoorwaySlot, portCrosser);
            crossers.SetEdge(goalPort.BoundaryCell.X, goalPort.BoundaryCell.Y, goalPort.DoorwaySlot, portCrosser);

            for (var i = 0; i + 1 < chain.Count; i++)
                SetSharedEdge(crossers, chain[i], chain[i + 1], bodyCrosser);

            layout.TunnelLinks.Add(new TunnelLink
            {
                CornerA = startPort.OpenCorner,
                CornerB = goalPort.OpenCorner,
                Length = chain.Count + 1
            });
            return true;
        }

        /// <summary>
        /// All usable doorway positions along a room's four straight walls. A port requires: both
        /// wall-adjacent corners on the room side open, both corners on the far side solid (a clean
        /// side-open cell), no crosser already on any edge of the boundary cell (side-open doorway
        /// tiles carry exactly one crosser), and the doorway not opening off-grid.
        /// </summary>
        private static List<Port> EnumeratePorts(CornerTerrainGrid corners, EdgeCrosserGrid crossers, RoomRect rect, string open, HashSet<string> allOpenLabels)
        {
            var ports = new List<Port>();

            // Left wall: boundary cells in column rect.X0 - 1, open side = Right, doorway = Left.
            for (var y = rect.Y0; y < rect.Y1; y++)
            {
                TryAddPort(corners, crossers, ports, open, allOpenLabels,
                    boundaryCell: (rect.X0 - 1, y), doorwaySlot: EdgeSlot.Left,
                    tunnelCell: (rect.X0 - 2, y), openCorner: (rect.X0, y));
            }

            // Right wall: boundary cells in column rect.X1, open side = Left, doorway = Right.
            for (var y = rect.Y0; y < rect.Y1; y++)
            {
                TryAddPort(corners, crossers, ports, open, allOpenLabels,
                    boundaryCell: (rect.X1, y), doorwaySlot: EdgeSlot.Right,
                    tunnelCell: (rect.X1 + 1, y), openCorner: (rect.X1, y));
            }

            // Bottom wall: boundary cells in row rect.Y0 - 1, open side = Top, doorway = Bottom.
            for (var x = rect.X0; x < rect.X1; x++)
            {
                TryAddPort(corners, crossers, ports, open, allOpenLabels,
                    boundaryCell: (x, rect.Y0 - 1), doorwaySlot: EdgeSlot.Bottom,
                    tunnelCell: (x, rect.Y0 - 2), openCorner: (x, rect.Y0));
            }

            // Top wall: boundary cells in row rect.Y1, open side = Bottom, doorway = Top.
            for (var x = rect.X0; x < rect.X1; x++)
            {
                TryAddPort(corners, crossers, ports, open, allOpenLabels,
                    boundaryCell: (x, rect.Y1), doorwaySlot: EdgeSlot.Top,
                    tunnelCell: (x, rect.Y1 + 1), openCorner: (x, rect.Y1));
            }

            return ports;
        }

        private static void TryAddPort(
            CornerTerrainGrid corners,
            EdgeCrosserGrid crossers,
            List<Port> ports,
            string open,
            HashSet<string> allOpenLabels,
            (int X, int Y) boundaryCell,
            int doorwaySlot,
            (int X, int Y) tunnelCell,
            (int X, int Y) openCorner)
        {
            var (cx, cy) = boundaryCell;
            if (cx < 0 || cy < 0 || cx >= corners.Width || cy >= corners.Height) return;

            // The cell must be cleanly side-open: room-side edge corners open (in THIS room's own
            // terrain), far-side corners solid (not open in ANY district's terrain -- a bordering
            // different-terrain room's already-carved corner must never be mistaken for a wall).
            // Classify via the doorway direction: the two corners on the doorway edge must be solid,
            // the two opposite corners open.
            var (solidA, solidB, openA, openB) = doorwaySlot switch
            {
                EdgeSlot.Left => ((cx, cy), (cx, cy + 1), (cx + 1, cy), (cx + 1, cy + 1)),
                EdgeSlot.Right => ((cx + 1, cy), (cx + 1, cy + 1), (cx, cy), (cx, cy + 1)),
                EdgeSlot.Bottom => ((cx, cy), (cx + 1, cy), (cx, cy + 1), (cx + 1, cy + 1)),
                EdgeSlot.Top => ((cx, cy + 1), (cx + 1, cy + 1), (cx, cy), (cx + 1, cy)),
                _ => throw new ArgumentOutOfRangeException(nameof(doorwaySlot))
            };

            if (corners.Labels[openA.Item1, openA.Item2] != open) return;
            if (corners.Labels[openB.Item1, openB.Item2] != open) return;
            if (allOpenLabels.Contains(corners.Labels[solidA.Item1, solidA.Item2])) return;
            if (allOpenLabels.Contains(corners.Labels[solidB.Item1, solidB.Item2])) return;

            // Side-open doorway tiles carry exactly one crosser; skip boundary cells already claimed.
            for (var slot = 0; slot < 4; slot++)
            {
                if (crossers.GetEdge(cx, cy, slot).Length != 0) return;
            }

            if (tunnelCell.X < 0 || tunnelCell.Y < 0 || tunnelCell.X >= corners.Width || tunnelCell.Y >= corners.Height) return;

            ports.Add(new Port(boundaryCell, doorwaySlot, tunnelCell, openCorner));
        }

        private static bool IsSolidCell(CornerTerrainGrid corners, (int X, int Y) cell, HashSet<string> allOpenLabels)
        {
            return !allOpenLabels.Contains(corners.Labels[cell.X, cell.Y]) &&
                   !allOpenLabels.Contains(corners.Labels[cell.X + 1, cell.Y]) &&
                   !allOpenLabels.Contains(corners.Labels[cell.X, cell.Y + 1]) &&
                   !allOpenLabels.Contains(corners.Labels[cell.X + 1, cell.Y + 1]);
        }

        private static void SetSharedEdge(EdgeCrosserGrid crossers, (int X, int Y) a, (int X, int Y) b, string crosser)
        {
            if (b.X == a.X + 1) crossers.SetEdge(a.X, a.Y, EdgeSlot.Right, crosser);
            else if (b.X == a.X - 1) crossers.SetEdge(a.X, a.Y, EdgeSlot.Left, crosser);
            else if (b.Y == a.Y + 1) crossers.SetEdge(a.X, a.Y, EdgeSlot.Top, crosser);
            else if (b.Y == a.Y - 1) crossers.SetEdge(a.X, a.Y, EdgeSlot.Bottom, crosser);
            else throw new ArgumentException($"Cells ({a.X},{a.Y}) and ({b.X},{b.Y}) are not adjacent.");
        }

        private static void Shuffle(List<Port> ports, System.Random random)
        {
            for (var i = ports.Count - 1; i > 0; i--)
            {
                var j = random.Next(i + 1);
                (ports[i], ports[j]) = (ports[j], ports[i]);
            }
        }
    }
}
