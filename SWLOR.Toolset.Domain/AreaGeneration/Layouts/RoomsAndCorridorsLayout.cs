#nullable disable
using System;
using System.Collections.Generic;

namespace SWLOR.Toolset.Domain.AreaGeneration.Layouts
{
    /// <summary>
    /// Classic dungeon shape: rectangular rooms of varied size joined by a corridor spanning tree,
    /// plus extra loop corridors so the result isn't a pure tree of dead ends.
    /// </summary>
    internal static class RoomsAndCorridorsLayout
    {
        /// <summary>Minimum solid-corner gap required between a room rectangle and the border ring, and between rooms.</summary>
        private const int MinBorderGap = 1;

        private const int MaxPlacementAttempts = 2000;

        internal static MacroLayout Generate(MacroLayoutParameters parameters, System.Random random)
        {
            var width = parameters.Width;
            var height = parameters.Height;

            var corners = new CornerTerrainGrid(width, height, parameters.SolidTerrain);
            var layout = new MacroLayout(corners);

            var roomRects = PlaceRooms(parameters, random);

            // Multi-terrain districts (v1: Tunnel mode only -- see MacroLayoutParameters.
            // SecondaryOpenTerrain doc comment for why OpenLane is excluded). Room 0 (the first room a
            // style places) always stays OpenTerrain so the entrance biases toward the primary
            // district; every other room independently rolls SecondaryRoomFraction. When districts are
            // inactive every entry is just parameters.OpenTerrain and zero extra RNG is consumed, so
            // back-compat is exact.
            //
            // Also requires CorridorCrosserType.Corridor: Alley is a single-terrain-verified vocabulary
            // (vmr01's Plaza-only "exterior streets" crosser, see CorridorCrosserType doc comment) with
            // no confirmed Alley-port tiles for any secondary district terrain, whereas Corridor/Doorway
            // is verified present on every generation tileset including both district terrains (zsf01
            // floor+Floor2, vmr01 Plaza+Floor). A tileset profile may declare SecondaryOpenTerrain and
            // still be composed with an Alley-mode (Streets) layout profile for its own single-terrain
            // exterior use — districts simply stay inactive for that pairing.
            var useDistricts = parameters.CorridorMode == CorridorMode.Tunnel &&
                                parameters.CorridorCrosserType == CorridorCrosserType.Corridor &&
                                !string.IsNullOrEmpty(parameters.SecondaryOpenTerrain) &&
                                parameters.SecondaryRoomFraction > 0;

            var roomTerrain = new string[roomRects.Count];
            for (var i = 0; i < roomRects.Count; i++)
            {
                roomTerrain[i] = i == 0 || !useDistricts
                    ? parameters.OpenTerrain
                    : random.NextDouble() < parameters.SecondaryRoomFraction
                        ? parameters.SecondaryOpenTerrain
                        : parameters.OpenTerrain;
            }

            // Carve room interiors open, each from its own assigned terrain. Border ring is never
            // touched since room placement always keeps a >=1 solid-corner gap from it.
            for (var i = 0; i < roomRects.Count; i++)
            {
                var rect = roomRects[i];
                for (var x = rect.X0; x <= rect.X1; x++)
                {
                    for (var y = rect.Y0; y <= rect.Y1; y++)
                    {
                        corners.Labels[x, y] = roomTerrain[i];
                    }
                }
            }

            var centers = new (int X, int Y)[roomRects.Count];
            for (var i = 0; i < roomRects.Count; i++)
            {
                var rect = roomRects[i];
                centers[i] = ((rect.X0 + rect.X1) / 2, (rect.Y0 + rect.Y1) / 2);
            }

            var treeEdges = BuildSpanningTree(centers);
            var edgeSet = new HashSet<(int, int)>();
            var allEdges = new List<(int U, int V)>();
            foreach (var edge in treeEdges)
            {
                edgeSet.Add(Normalize(edge.U, edge.V));
                allEdges.Add((edge.U, edge.V));
            }

            // Loop connections: extra corridors between random room pairs beyond the spanning tree,
            // so the layout has cycles instead of reading as a strict branching tree.
            var extraCount = (int)Math.Round(parameters.LoopFactor * roomRects.Count);
            var extraAttempts = 0;
            var added = 0;

            while (added < extraCount && extraAttempts < MaxPlacementAttempts && roomRects.Count >= 2)
            {
                extraAttempts++;

                var a = random.Next(roomRects.Count);
                var b = random.Next(roomRects.Count);
                if (a == b) continue;

                var key = Normalize(a, b);
                if (!edgeSet.Add(key)) continue;

                allEdges.Add((a, b));
                added++;
            }

            CarveAllEdges(layout, roomRects, centers, roomTerrain, allEdges, parameters, random);

            var rooms = new List<LayoutRoom>(roomRects.Count);
            for (var i = 0; i < roomRects.Count; i++)
                rooms.Add(LayoutRoomBuilder.BuildFromRect(i, roomRects[i], corners, roomTerrain[i]));

            layout.Rooms = rooms;
            return layout;
        }

        private static (int, int) Normalize(int u, int v) => u < v ? (u, v) : (v, u);

        /// <summary>
        /// Carves every room connection. Tunnel mode is all-or-nothing: a lane carved after tunnels
        /// could open corners underneath existing crosser chains and strand unresolvable half-tunnels,
        /// so if any tunnel fails, all crossers are discarded and every connection is re-carved as an
        /// open lane. Failures are rare (a tunnel needs only one solid path between two walls).
        ///
        /// District-aware: OpenLane bands are carved straight from room center to room center, which
        /// would repaint a secondary-terrain room's own interior back to OpenTerrain partway through.
        /// So the fallback additionally downgrades every room to OpenTerrain (a no-op when districts
        /// were never active -- every entry already equals OpenTerrain) before carving lanes, keeping
        /// <paramref name="roomTerrain"/> consistent with what the corner grid actually ends up holding
        /// for the caller's subsequent LayoutRoomBuilder pass.
        /// </summary>
        private static void CarveAllEdges(
            MacroLayout layout,
            List<RoomRect> roomRects,
            (int X, int Y)[] centers,
            string[] roomTerrain,
            List<(int U, int V)> edges,
            MacroLayoutParameters parameters,
            System.Random random)
        {
            if (parameters.CorridorMode == CorridorMode.Tunnel)
            {
                var allTunneled = true;
                foreach (var (u, v) in edges)
                {
                    if (!LayoutTunnelCarver.TryConnect(layout, roomRects[u], roomRects[v], roomTerrain[u], roomTerrain[v], parameters, random))
                    {
                        allTunneled = false;
                        break;
                    }
                }

                if (allTunneled)
                    return;

                layout.Crossers = new EdgeCrosserGrid(parameters.Width, parameters.Height);
                layout.TunnelLinks.Clear();

                DowngradeDistrictsToOpenTerrain(layout.Corners, roomRects, roomTerrain, parameters.OpenTerrain);
            }

            foreach (var (u, v) in edges)
            {
                var a = centers[u];
                var b = centers[v];
                var horizontalFirst = random.Next(2) == 0;

                LayoutCornerUtils.CarveLShapedCorridor(
                    layout.Corners, a.X, a.Y, b.X, b.Y, horizontalFirst,
                    Math.Max(1, parameters.CorridorWidth), parameters.Width, parameters.Height, parameters.OpenTerrain);
            }
        }

        /// <summary>
        /// Repaints every room whose assigned terrain differs from <paramref name="primaryOpenTerrain"/>
        /// back to it, and resets <paramref name="roomTerrain"/> to match -- used only when Tunnel
        /// carving fails entirely and generation falls back to OpenLane corridors (see CarveAllEdges).
        /// </summary>
        private static void DowngradeDistrictsToOpenTerrain(
            CornerTerrainGrid corners, List<RoomRect> roomRects, string[] roomTerrain, string primaryOpenTerrain)
        {
            for (var i = 0; i < roomRects.Count; i++)
            {
                if (roomTerrain[i] == primaryOpenTerrain) continue;

                var rect = roomRects[i];
                for (var x = rect.X0; x <= rect.X1; x++)
                {
                    for (var y = rect.Y0; y <= rect.Y1; y++)
                    {
                        corners.Labels[x, y] = primaryOpenTerrain;
                    }
                }

                roomTerrain[i] = primaryOpenTerrain;
            }
        }

        private static List<RoomRect> PlaceRooms(MacroLayoutParameters parameters, System.Random random)
        {
            var targetCount = random.Next(parameters.MinRooms, parameters.MaxRooms + 1);

            var minSize = Math.Max(2, parameters.MinRoomCornerSize);
            var maxSize = Math.Max(minSize, parameters.MaxRoomCornerSize);

            var rooms = RunPlacementRound(parameters, random, targetCount, minSize, maxSize);

            // Rescue round: a large early room plus its solid gap can blockade the entire interior
            // (e.g. a max-size room centered in a 16x16 area leaves no legal spot for even a minimum
            // room), and no amount of rerolling escapes that. Discard the blockading layout and repack
            // from scratch with minimum-size rooms only.
            if (rooms.Count < 2)
            {
                rooms = RunPlacementRound(parameters, random, targetCount, minSize, minSize);
            }

            if (rooms.Count < 2)
            {
                throw new InvalidOperationException(
                    $"RoomsAndCorridors could not place enough rooms: only {rooms.Count} fit in a " +
                    $"{parameters.Width}x{parameters.Height} area (at least 2 rooms are required).");
            }

            return rooms;
        }

        private static List<RoomRect> RunPlacementRound(
            MacroLayoutParameters parameters, System.Random random, int targetCount, int minSize, int maxSize)
        {
            var rooms = new List<RoomRect>();
            var attempts = 0;

            while (rooms.Count < targetCount && attempts < MaxPlacementAttempts)
            {
                attempts++;

                // Degrade the size ceiling toward the minimum as failed attempts accumulate: early
                // attempts roll the full small/large mix, but a cramped area falls back to packing
                // small rooms instead of exhausting the budget rerolling rectangles that cannot fit.
                var degrade = Math.Min(1.0, attempts / (MaxPlacementAttempts * 0.5));
                var effectiveMax = Math.Max(minSize, maxSize - (int)Math.Round(degrade * (maxSize - minSize)));
                var midpoint = minSize + (effectiveMax - minSize) / 2;

                // Bias toward mixing small and large rooms rather than a single narrow size band.
                int w, h;
                if (midpoint > minSize && random.NextDouble() < 0.5)
                {
                    w = random.Next(minSize, midpoint + 1);
                    h = random.Next(minSize, midpoint + 1);
                }
                else
                {
                    w = random.Next(midpoint, effectiveMax + 1);
                    h = random.Next(midpoint, effectiveMax + 1);
                }

                var maxX0 = parameters.Width - MinBorderGap - 1 - (w - 1);
                var maxY0 = parameters.Height - MinBorderGap - 1 - (h - 1);
                var minX0 = MinBorderGap + 1;
                var minY0 = MinBorderGap + 1;

                if (maxX0 < minX0 || maxY0 < minY0)
                    continue;

                var x0 = random.Next(minX0, maxX0 + 1);
                var y0 = random.Next(minY0, maxY0 + 1);
                var x1 = x0 + w - 1;
                var y1 = y0 + h - 1;

                if (OverlapsAny(rooms, x0, y0, x1, y1))
                    continue;

                rooms.Add(new RoomRect(x0, y0, x1, y1));
            }

            return rooms;
        }

        private static bool OverlapsAny(List<RoomRect> rooms, int x0, int y0, int x1, int y1)
        {
            foreach (var r in rooms)
            {
                if (x0 - MinBorderGap <= r.X1 && x1 + MinBorderGap >= r.X0 &&
                    y0 - MinBorderGap <= r.Y1 && y1 + MinBorderGap >= r.Y0)
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Deterministic Prim's algorithm (nearest-neighbor) spanning tree over room centers.
        /// No randomness is needed here: ties are broken by iteration order, which is fixed for a
        /// given room list.
        /// </summary>
        private static List<(int U, int V)> BuildSpanningTree((int X, int Y)[] centers)
        {
            var n = centers.Length;
            var edges = new List<(int U, int V)>(n - 1);
            var inTree = new bool[n];
            inTree[0] = true;
            var connected = 1;

            while (connected < n)
            {
                var bestDist = long.MaxValue;
                var bestU = -1;
                var bestV = -1;

                for (var u = 0; u < n; u++)
                {
                    if (!inTree[u]) continue;

                    for (var v = 0; v < n; v++)
                    {
                        if (inTree[v]) continue;

                        var dx = (long)(centers[u].X - centers[v].X);
                        var dy = (long)(centers[u].Y - centers[v].Y);
                        var dist = dx * dx + dy * dy;

                        if (dist < bestDist)
                        {
                            bestDist = dist;
                            bestU = u;
                            bestV = v;
                        }
                    }
                }

                inTree[bestV] = true;
                edges.Add((bestU, bestV));
                connected++;
            }

            return edges;
        }
    }
}
