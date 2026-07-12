using System;
using System.Collections.Generic;

namespace SWLOR.Game.Server.Service.AreaGenerationService
{
    /// <summary>
    /// Resolves a corner-granularity <see cref="MacroLayout"/> into concrete (tileId, orientation) picks
    /// from a <see cref="TilesetModel"/>'s tile inventory, matching each tile cell's four world corners
    /// against the tileset's corner-terrain data (the same corner-matching model the toolset terrain
    /// brush uses).
    ///
    /// Scope (v1): only tiles with no edge crossers, no group membership, flat corner heights, and no
    /// door slots are considered — matching the height/edge/group scope discipline of the layout solver.
    /// </summary>
    public static class TileResolver
    {
        public static bool TryResolve(
            TilesetModel tileset,
            MacroLayout layout,
            System.Random random,
            out ResolvedLayout resolved,
            out string failureReason)
        {
            if (tileset == null) throw new ArgumentNullException(nameof(tileset));
            if (layout == null) throw new ArgumentNullException(nameof(layout));
            if (random == null) throw new ArgumentNullException(nameof(random));

            var width = layout.Corners.Width;
            var height = layout.Corners.Height;

            var candidateLookup = BuildCandidateLookup(tileset);
            var tiles = new ResolvedTile[width * height];

            // Bottom-up, row-major order — matches ResolvedLayout.Tiles indexing (index = y * Width + x,
            // y = 0 at the south edge). This is also the "first unresolvable cell" order used for
            // failure reporting.
            for (var y = 0; y < height; y++)
            {
                for (var x = 0; x < width; x++)
                {
                    var tl = layout.Corners.Labels[x, y + 1];
                    var tr = layout.Corners.Labels[x + 1, y + 1];
                    var br = layout.Corners.Labels[x + 1, y];
                    var bl = layout.Corners.Labels[x, y];

                    var key = MakeKey(tl, tr, br, bl);

                    if (!candidateLookup.TryGetValue(key, out var candidates) || candidates.All.Count == 0)
                    {
                        failureReason =
                            $"No matching tile for cell ({x},{y}): TL={tl}, TR={tr}, BR={br}, BL={bl}.";
                        resolved = null;
                        return false;
                    }

                    // Prefer fully-pathable tiles: 'A' path nodes connect all walkable edges, while
                    // restricted nodes (observed on zsf01's junction tiles) can wall off corners the
                    // terrain labels say are open, failing path validation later. Restricted tiles
                    // remain in play only when no 'A' alternative exists for the combination.
                    var pool = candidates.FullyPathable.Count > 0 ? candidates.FullyPathable : candidates.All;
                    var pick = pool[random.Next(pool.Count)];
                    tiles[y * width + x] = new ResolvedTile
                    {
                        TileId = pick.TileId,
                        Orientation = pick.Orientation,
                        Height = 0
                    };
                }
            }

            resolved = new ResolvedLayout
            {
                TilesetResref = tileset.Resref,
                Seed = layout.Seed,
                Width = width,
                Height = height,
                Tiles = tiles,
                Rooms = layout.Rooms
            };
            failureReason = null;
            return true;
        }

        /// <summary>
        /// Builds a lookup from a case-insensitive (TL, TR, BR, BL) corner-label key to every
        /// (tileId, orientation) candidate satisfying the v1 resolution rules. Built once per resolve
        /// call rather than scanning all tiles per cell.
        ///
        /// Rotation permutes a tile's fixed Corners/Edges/CornerHeights arrays, so "all edges empty" /
        /// "all corner heights zero" are rotation-invariant — they're checked once on the raw arrays
        /// rather than once per orientation.
        /// </summary>
        private class CandidateSet
        {
            public List<(int TileId, int Orientation)> All { get; } = new();
            public List<(int TileId, int Orientation)> FullyPathable { get; } = new();
        }

        private static Dictionary<string, CandidateSet> BuildCandidateLookup(TilesetModel tileset)
        {
            var lookup = new Dictionary<string, CandidateSet>();

            foreach (var tile in tileset.Tiles)
            {
                if (tile.GroupIndex != -1) continue;
                if (tile.Doors.Count != 0) continue;
                if (tile.HasAnyCrosser) continue;
                if (tile.CornerHeights[0] != 0 || tile.CornerHeights[1] != 0 ||
                    tile.CornerHeights[2] != 0 || tile.CornerHeights[3] != 0) continue;

                var fullyPathable = string.Equals(tile.PathNode, "A", StringComparison.OrdinalIgnoreCase);

                for (var orientation = 0; orientation < 4; orientation++)
                {
                    var tl = tile.GetCornerAt(orientation, CornerSlot.TopLeft);
                    var tr = tile.GetCornerAt(orientation, CornerSlot.TopRight);
                    var br = tile.GetCornerAt(orientation, CornerSlot.BottomRight);
                    var bl = tile.GetCornerAt(orientation, CornerSlot.BottomLeft);

                    var key = MakeKey(tl, tr, br, bl);

                    if (!lookup.TryGetValue(key, out var set))
                    {
                        set = new CandidateSet();
                        lookup[key] = set;
                    }

                    set.All.Add((tile.TileId, orientation));
                    if (fullyPathable)
                        set.FullyPathable.Add((tile.TileId, orientation));
                }
            }

            return lookup;
        }

        private static string MakeKey(string tl, string tr, string br, string bl)
        {
            return string.Join(
                "|",
                (tl ?? string.Empty).ToUpperInvariant(),
                (tr ?? string.Empty).ToUpperInvariant(),
                (br ?? string.Empty).ToUpperInvariant(),
                (bl ?? string.Empty).ToUpperInvariant());
        }
    }
}
