#nullable disable
using System;
using System.Collections.Generic;
using System.Linq;

namespace SWLOR.Toolset.Domain.AreaGeneration.Layouts
{
    /// <summary>
    /// BSP-subdivided facility interior: rooms share 1-corner walls, joined by door gaps. High density,
    /// little solid mass — the whole rectangle reads as subdivided into rooms.
    /// </summary>
    internal static class PackedRoomsLayout
    {
        private class BspNode
        {
            public RoomRect Rect;
            public BspNode Left;
            public BspNode Right;
            public bool IsVertical;
            public int SplitLine;
        }

        internal static MacroLayout Generate(MacroLayoutParameters parameters, System.Random random)
        {
            var width = parameters.Width;
            var height = parameters.Height;

            var corners = new CornerTerrainGrid(width, height, parameters.SolidTerrain);
            var layout = new MacroLayout(corners);

            var minSize = Math.Max(2, parameters.MinRoomCornerSize);
            var maxSize = Math.Max(minSize, parameters.MaxRoomCornerSize);

            var root = Build(new RoomRect(1, 1, width - 1, height - 1), minSize, maxSize, random);

            var leaves = new List<BspNode>();
            CollectLeaves(root, leaves);

            if (leaves.Count < 2)
            {
                throw new InvalidOperationException(
                    $"PackedRooms BSP produced only {leaves.Count} leaf/leaves in a {width}x{height} area; " +
                    "at least 2 are required.");
            }

            foreach (var leaf in leaves)
            {
                for (var x = leaf.Rect.X0; x <= leaf.Rect.X1; x++)
                    for (var y = leaf.Rect.Y0; y <= leaf.Rect.Y1; y++)
                        corners.Labels[x, y] = parameters.OpenTerrain;
            }

            var corridorWidth = Math.Max(1, parameters.CorridorWidth);
            CarveSplitDoors(root, corners, parameters, corridorWidth, random);

            var extraCount = (int)Math.Round(parameters.LoopFactor * leaves.Count);
            CarveExtraDoors(leaves, corners, parameters, corridorWidth, extraCount, random);

            EnsureConnectivity(corners, parameters, random);

            var allRooms = new List<LayoutRoom>(leaves.Count);
            for (var i = 0; i < leaves.Count; i++)
                allRooms.Add(LayoutRoomBuilder.BuildFromRect(i, leaves[i].Rect, corners, parameters.OpenTerrain));

            // All leaves stay carved (dense facility feel); only the reported room list — used for
            // role/spawn metadata — is capped, keeping the largest leaves.
            var capped = allRooms
                .OrderByDescending(r => r.Tiles.Count)
                .Take(Math.Max(2, parameters.MaxRooms))
                .ToList();

            for (var i = 0; i < capped.Count; i++)
                capped[i].Id = i;

            layout.Rooms = capped;
            return layout;
        }

        private static BspNode Build(RoomRect rect, int minSize, int maxSize, System.Random random)
        {
            var w = rect.CornerWidth;
            var h = rect.CornerHeight;

            var canSplitX = w >= minSize * 2 + 1;
            var canSplitY = h >= minSize * 2 + 1;
            var mustSplit = w > maxSize + 1 || h > maxSize + 1;

            if (!canSplitX && !canSplitY)
                return new BspNode { Rect = rect };

            if (!mustSplit && random.NextDouble() < 0.35)
                return new BspNode { Rect = rect };

            // Split along the longer axis (aspect-driven), falling back to whichever axis is splittable.
            var splitVertical = canSplitX && (!canSplitY || w >= h);

            if (splitVertical)
            {
                var loBound = rect.X0 + minSize;
                var hiBound = rect.X1 - minSize - 1;
                if (hiBound < loBound)
                    return new BspNode { Rect = rect };

                var splitX = random.Next(loBound, hiBound + 1);
                var left = Build(new RoomRect(rect.X0, rect.Y0, splitX - 1, rect.Y1), minSize, maxSize, random);
                var right = Build(new RoomRect(splitX + 1, rect.Y0, rect.X1, rect.Y1), minSize, maxSize, random);

                return new BspNode { Rect = rect, Left = left, Right = right, IsVertical = true, SplitLine = splitX };
            }
            else
            {
                var loBound = rect.Y0 + minSize;
                var hiBound = rect.Y1 - minSize - 1;
                if (hiBound < loBound)
                    return new BspNode { Rect = rect };

                var splitY = random.Next(loBound, hiBound + 1);
                var bottom = Build(new RoomRect(rect.X0, rect.Y0, rect.X1, splitY - 1), minSize, maxSize, random);
                var top = Build(new RoomRect(rect.X0, splitY + 1, rect.X1, rect.Y1), minSize, maxSize, random);

                return new BspNode { Rect = rect, Left = bottom, Right = top, IsVertical = false, SplitLine = splitY };
            }
        }

        private static void CollectLeaves(BspNode node, List<BspNode> leaves)
        {
            if (node.Left == null || node.Right == null)
            {
                leaves.Add(node);
                return;
            }

            CollectLeaves(node.Left, leaves);
            CollectLeaves(node.Right, leaves);
        }

        /// <summary>
        /// Punches a door gap through every BSP split line. Connecting every split this way yields a
        /// connected tree over the whole leaf set by construction.
        /// </summary>
        private static void CarveSplitDoors(BspNode node, CornerTerrainGrid corners, MacroLayoutParameters parameters, int corridorWidth, System.Random random)
        {
            if (node.Left == null || node.Right == null) return;

            if (node.IsVertical)
            {
                var yLo = Math.Max(node.Left.Rect.Y0, node.Right.Rect.Y0);
                var yHi = Math.Min(node.Left.Rect.Y1, node.Right.Rect.Y1);
                CarveDoorGap(corners, parameters, node.SplitLine, yLo, yHi, corridorWidth, vertical: true, random);
            }
            else
            {
                var xLo = Math.Max(node.Left.Rect.X0, node.Right.Rect.X0);
                var xHi = Math.Min(node.Left.Rect.X1, node.Right.Rect.X1);
                CarveDoorGap(corners, parameters, node.SplitLine, xLo, xHi, corridorWidth, vertical: false, random);
            }

            CarveSplitDoors(node.Left, corners, parameters, corridorWidth, random);
            CarveSplitDoors(node.Right, corners, parameters, corridorWidth, random);
        }

        /// <summary>
        /// Opens a door gap along a 1-corner wall line, restricted to positions where BOTH sides of the
        /// wall are already open corners. A door corner whose flank is another (perpendicular) wall corner
        /// connects nothing — sibling subtrees can have internal walls abutting the split line, so raw
        /// random positions along the overlap span are not guaranteed to be usable.
        /// </summary>
        private static void CarveDoorGap(CornerTerrainGrid corners, MacroLayoutParameters parameters, int line, int lo, int hi, int corridorWidth, bool vertical, System.Random random)
        {
            if (hi < lo) return;

            var valid = new List<int>();
            for (var pos = lo; pos <= hi; pos++)
            {
                var sideA = vertical ? corners.Labels[line - 1, pos] : corners.Labels[pos, line - 1];
                var sideB = vertical ? corners.Labels[line + 1, pos] : corners.Labels[pos, line + 1];

                if (sideA == parameters.OpenTerrain && sideB == parameters.OpenTerrain)
                    valid.Add(pos);
            }

            if (valid.Count == 0)
                return; // No usable position on this wall; the connectivity safety pass covers the gap.

            // Prefer a full corridor-width gap over a contiguous run of valid positions; fall back to
            // a single-corner door when no run is long enough.
            var gapWidth = Math.Min(corridorWidth, valid.Count);
            var starts = new List<int>();
            for (var i = 0; i + gapWidth - 1 < valid.Count; i++)
            {
                if (valid[i + gapWidth - 1] - valid[i] == gapWidth - 1)
                    starts.Add(valid[i]);
            }

            int chosenStart;
            if (starts.Count > 0)
            {
                chosenStart = starts[random.Next(starts.Count)];
            }
            else
            {
                gapWidth = 1;
                chosenStart = valid[random.Next(valid.Count)];
            }

            for (var i = 0; i < gapWidth; i++)
            {
                var pos = chosenStart + i;
                if (vertical)
                    corners.Labels[line, pos] = parameters.OpenTerrain;
                else
                    corners.Labels[pos, line] = parameters.OpenTerrain;
            }
        }

        /// <summary>
        /// Final safety pass: if the open space is still disconnected (a wall carried no usable door
        /// position, or a leaf ended up sealed), carve additional door gaps between components instead
        /// of failing the layout. Each iteration opens the closest solid gap between the largest
        /// component and any other component, so the loop strictly reduces the component count.
        /// </summary>
        private static void EnsureConnectivity(CornerTerrainGrid corners, MacroLayoutParameters parameters, System.Random random)
        {
            var guard = 0;

            while (guard++ < 64)
            {
                var open = LayoutCornerUtils.GetCorners(corners, parameters.OpenTerrain);
                if (open.Count == 0) return;

                var componentOf = new Dictionary<(int X, int Y), int>();
                var componentSizes = new List<int>();

                foreach (var corner in open)
                {
                    if (componentOf.ContainsKey(corner)) continue;

                    var component = LayoutCornerUtils.FloodFill(corners, parameters.OpenTerrain, corner);
                    var id = componentSizes.Count;
                    foreach (var c in component)
                        componentOf[c] = id;
                    componentSizes.Add(component.Count);
                }

                if (componentSizes.Count <= 1) return;

                var largestId = 0;
                for (var i = 1; i < componentSizes.Count; i++)
                    if (componentSizes[i] > componentSizes[largestId])
                        largestId = i;

                // Closest pair of corners between the largest component and any other component.
                var bestDist = long.MaxValue;
                var bestFrom = open[0];
                var bestTo = open[0];

                foreach (var a in open)
                {
                    if (componentOf[a] != largestId) continue;

                    foreach (var b in open)
                    {
                        if (componentOf[b] == largestId) continue;

                        var dx = (long)(a.X - b.X);
                        var dy = (long)(a.Y - b.Y);
                        var dist = dx * dx + dy * dy;

                        if (dist < bestDist)
                        {
                            bestDist = dist;
                            bestFrom = a;
                            bestTo = b;
                        }
                    }
                }

                LayoutCornerUtils.CarveLShapedCorridor(
                    corners, bestFrom.X, bestFrom.Y, bestTo.X, bestTo.Y,
                    random.Next(2) == 0, 1, parameters.Width, parameters.Height, parameters.OpenTerrain);
            }
        }

        /// <summary>
        /// Extra door gaps between random leaf pairs that are geometrically adjacent (share a 1-corner
        /// wall with an overlapping edge), regardless of where they sit in the BSP tree. Adds loops on
        /// top of the tree-connected base.
        /// </summary>
        private static void CarveExtraDoors(List<BspNode> leaves, CornerTerrainGrid corners, MacroLayoutParameters parameters, int corridorWidth, int extraCount, System.Random random)
        {
            var adjacentPairs = new List<(bool Vertical, int Line, int Lo, int Hi)>();

            for (var i = 0; i < leaves.Count; i++)
            {
                for (var j = i + 1; j < leaves.Count; j++)
                {
                    var a = leaves[i].Rect;
                    var b = leaves[j].Rect;

                    if (a.X1 + 2 == b.X0 || b.X1 + 2 == a.X0)
                    {
                        var lo = Math.Max(a.Y0, b.Y0);
                        var hi = Math.Min(a.Y1, b.Y1);
                        if (hi >= lo)
                        {
                            var line = a.X1 + 2 == b.X0 ? a.X1 + 1 : b.X1 + 1;
                            adjacentPairs.Add((true, line, lo, hi));
                        }
                    }

                    if (a.Y1 + 2 == b.Y0 || b.Y1 + 2 == a.Y0)
                    {
                        var lo = Math.Max(a.X0, b.X0);
                        var hi = Math.Min(a.X1, b.X1);
                        if (hi >= lo)
                        {
                            var line = a.Y1 + 2 == b.Y0 ? a.Y1 + 1 : b.Y1 + 1;
                            adjacentPairs.Add((false, line, lo, hi));
                        }
                    }
                }
            }

            if (adjacentPairs.Count == 0) return;

            for (var i = 0; i < extraCount; i++)
            {
                var pair = adjacentPairs[random.Next(adjacentPairs.Count)];
                CarveDoorGap(corners, parameters, pair.Line, pair.Lo, pair.Hi, corridorWidth, pair.Vertical, random);
            }
        }
    }
}
