using System;
using System.Collections.Generic;

namespace SWLOR.Game.Server.Service.AreaGenerationService
{
    /// <summary>
    /// Resolves a corner-granularity <see cref="MacroLayout"/> into concrete (tileId, orientation) picks
    /// from a <see cref="TilesetModel"/>'s tile inventory, matching each tile cell's four world corners
    /// AND four world edges (<see cref="MacroLayout.Crossers"/>) against the tileset's corner-terrain
    /// and edge-crosser data (the same corner/edge-matching model the toolset terrain brush uses).
    ///
    /// Scope: ungrouped, flat-cornered (CornerHeights all 0) tiles only. Within that scope, a tile is a
    /// candidate when it is either (a) crosser-free and door-free — the full v1 tile set, unchanged —
    /// or (b) has at least one edge crosser. Crosser tiles that ALSO carry door slots are only ever
    /// registered under keys whose edge part contains a Doorway crosser (a door slot implies a door
    /// frame, so such a tile must never leak into a blank-edge cell); door-slot tiles with no crosser
    /// at all remain excluded — they are TileDoorPlanner's post-resolution inventory, not the corner
    /// resolver's.
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
            var featureLookup = layout.FeatureTiles.Count > 0
                ? BuildFeatureLookup(tileset, layout.FeatureTiles)
                : null;
            var tiles = new ResolvedTile[width * height];

            // Cells that have already received a feature tile this resolve, tracked for the spacing
            // rule (no two features within Chebyshev distance 2 of each other).
            var placedFeatures = new List<(int X, int Y)>();

            // Bottom-up, row-major order — matches ResolvedLayout.Tiles indexing (index = y * Width + x,
            // y = 0 at the south edge). This is also the "first unresolvable cell" order used for
            // failure reporting.
            for (var y = 0; y < height; y++)
            {
                for (var x = 0; x < width; x++)
                {
                    // LayoutGroupStamper-pinned cells bypass candidate lookup entirely: the stamper
                    // already verified the exact (tileId, orientation) is structurally consistent with
                    // the surrounding grid, so it is placed verbatim with no random draw.
                    if (layout.PinnedTiles.TryGetValue((x, y), out var pin))
                    {
                        tiles[y * width + x] = new ResolvedTile
                        {
                            TileId = pin.TileId,
                            Orientation = pin.Orientation,
                            Height = 0
                        };
                        continue;
                    }

                    var tl = layout.Corners.Labels[x, y + 1];
                    var tr = layout.Corners.Labels[x + 1, y + 1];
                    var br = layout.Corners.Labels[x + 1, y];
                    var bl = layout.Corners.Labels[x, y];

                    var top = layout.Crossers.GetEdge(x, y, EdgeSlot.Top);
                    var right = layout.Crossers.GetEdge(x, y, EdgeSlot.Right);
                    var bottom = layout.Crossers.GetEdge(x, y, EdgeSlot.Bottom);
                    var left = layout.Crossers.GetEdge(x, y, EdgeSlot.Left);

                    var key = MakeKey(tl, tr, br, bl, top, right, bottom, left);

                    if (!candidateLookup.TryGetValue(key, out var candidates) || candidates.All.Count == 0)
                    {
                        var edgeNote = string.Empty;
                        if (!string.IsNullOrEmpty(top) || !string.IsNullOrEmpty(right) ||
                            !string.IsNullOrEmpty(bottom) || !string.IsNullOrEmpty(left))
                        {
                            edgeNote =
                                $" Edges: Top={Describe(top)}, Right={Describe(right)}, Bottom={Describe(bottom)}, Left={Describe(left)}.";
                        }

                        failureReason =
                            $"No matching tile for cell ({x},{y}): TL={tl}, TR={tr}, BR={br}, BL={bl}.{edgeNote}";
                        resolved = null;
                        return false;
                    }

                    // Prefer fully-pathable tiles: 'A' path nodes connect all walkable edges, while
                    // restricted nodes (observed on zsf01's junction tiles) can wall off corners the
                    // terrain labels say are open, failing path validation later. Restricted tiles
                    // remain in play only when no 'A' alternative exists for the combination.
                    var pool = candidates.FullyPathable.Count > 0 ? candidates.FullyPathable : candidates.All;

                    // Feature sprinkling: only ever rolled when this configuration has feature tiles
                    // AND this specific cell's key has a matching feature candidate. This ordering is
                    // load-bearing for determinism/back-compat — when layout.FeatureTiles is empty
                    // (every caller until a tileset profile stamps it), featureLookup is null and this
                    // whole block is skipped with zero extra random calls, so existing seeds/tests
                    // resolve with exactly the pre-feature RNG sequence (one random.Next per cell).
                    var (tileId, orientation) = (pool[0].TileId, pool[0].Orientation);
                    var usedNormalPick = false;

                    if (featureLookup != null &&
                        featureLookup.TryGetValue(key, out var featureSet) &&
                        featureSet.TotalWeight > 0)
                    {
                        var densityRoll = random.NextDouble();
                        if (densityRoll < layout.FeatureDensity)
                        {
                            var featurePick = PickWeighted(featureSet, random);

                            var isTransitionAnchor = false;
                            foreach (var transition in layout.Transitions)
                            {
                                if (transition.Tile.X == x && transition.Tile.Y == y)
                                {
                                    isTransitionAnchor = true;
                                    break;
                                }
                            }

                            var tooCloseToAnotherFeature = false;
                            foreach (var placed in placedFeatures)
                            {
                                if (Math.Max(Math.Abs(placed.X - x), Math.Abs(placed.Y - y)) <= 2)
                                {
                                    tooCloseToAnotherFeature = true;
                                    break;
                                }
                            }

                            if (isTransitionAnchor || tooCloseToAnotherFeature)
                            {
                                usedNormalPick = true;
                            }
                            else
                            {
                                tileId = featurePick.TileId;
                                orientation = featurePick.Orientation;
                                placedFeatures.Add((x, y));
                            }
                        }
                        else
                        {
                            usedNormalPick = true;
                        }
                    }
                    else
                    {
                        usedNormalPick = true;
                    }

                    if (usedNormalPick)
                    {
                        var pick = pool[random.Next(pool.Count)];
                        tileId = pick.TileId;
                        orientation = pick.Orientation;
                    }

                    tiles[y * width + x] = new ResolvedTile
                    {
                        TileId = tileId,
                        Orientation = orientation,
                        Height = 0
                    };
                }
            }

            if (layout.DoorTransitions)
            {
                TileDoorPlanner.ApplyDoorTransitions(tileset, layout, tiles, width, height);
            }

            resolved = new ResolvedLayout
            {
                TilesetResref = tileset.Resref,
                Seed = layout.Seed,
                Width = width,
                Height = height,
                Tiles = tiles,
                Rooms = layout.Rooms,
                Transitions = layout.Transitions,
                OpenTerrain = layout.OpenTerrain
            };
            failureReason = null;
            return true;
        }

        /// <summary>
        /// Builds a lookup from a case-insensitive (TL, TR, BR, BL, Top, Right, Bottom, Left) key to
        /// every (tileId, orientation) candidate satisfying the resolution rules. Built once per resolve
        /// call rather than scanning all tiles per cell.
        ///
        /// Rotation permutes a tile's fixed Corners/Edges/CornerHeights arrays, so "all corner heights
        /// zero" and "has any crosser at all" are rotation-invariant — checked once on the raw arrays
        /// rather than once per orientation. Back-compat with the pre-crosser resolver is by
        /// construction: a crosser-free, door-free tile's oriented edge tuple is "","","","" under every
        /// orientation (rotating four blanks yields four blanks), so it only ever registers under a key
        /// whose edge part is fully blank — exactly the set, order, and per-key grouping the v1 resolver
        /// produced before edges existed in the key. A tile with HasAnyCrosser true can never rotate to
        /// an all-blank edge tuple (rotation only permutes the existing non-blank value(s), it can't
        /// erase them), so it can never appear under a fully-blank-edge key and therefore never disturbs
        /// the blank-edge candidate pools crosser-free layouts resolve against.
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
                if (tile.CornerHeights[0] != 0 || tile.CornerHeights[1] != 0 ||
                    tile.CornerHeights[2] != 0 || tile.CornerHeights[3] != 0) continue;

                var hasCrosser = tile.HasAnyCrosser;
                var hasDoors = tile.Doors.Count != 0;

                // Door-slot tiles with no crosser at all stay excluded: they're TileDoorPlanner's
                // inventory (a bare door slot with no matching Doorway crosser can't be corner/edge
                // matched into a generic cell).
                if (hasDoors && !hasCrosser) continue;

                var fullyPathable = string.Equals(tile.PathNode, "A", StringComparison.OrdinalIgnoreCase);

                for (var orientation = 0; orientation < 4; orientation++)
                {
                    var tl = tile.GetCornerAt(orientation, CornerSlot.TopLeft);
                    var tr = tile.GetCornerAt(orientation, CornerSlot.TopRight);
                    var br = tile.GetCornerAt(orientation, CornerSlot.BottomRight);
                    var bl = tile.GetCornerAt(orientation, CornerSlot.BottomLeft);

                    var top = tile.GetEdgeAt(orientation, EdgeSlot.Top);
                    var right = tile.GetEdgeAt(orientation, EdgeSlot.Right);
                    var bottom = tile.GetEdgeAt(orientation, EdgeSlot.Bottom);
                    var left = tile.GetEdgeAt(orientation, EdgeSlot.Left);

                    // A crosser tile that also has door slots may only be registered under keys whose
                    // edge part carries a Doorway crosser somewhere — a door slot implies a door frame,
                    // so it must never leak into a cell that doesn't expect a doorway.
                    if (hasCrosser && hasDoors)
                    {
                        var hasDoorwayEdge =
                            IsDoorway(top) || IsDoorway(right) || IsDoorway(bottom) || IsDoorway(left);
                        if (!hasDoorwayEdge) continue;
                    }

                    var key = MakeKey(tl, tr, br, bl, top, right, bottom, left);

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

        /// <summary>
        /// One configured feature group's registered candidates: every (tileId, orientation, weight)
        /// under the corner+edge key(s) its 1x1 tile matches. All entries for a given feature group
        /// share the same weight (the group's configured weight).
        /// </summary>
        private class FeatureCandidateSet
        {
            public List<(int TileId, int Orientation, int Weight)> Candidates { get; } = new();
            public int TotalWeight { get; set; }
        }

        /// <summary>
        /// Builds the feature-tile candidate lookup: for each configured (group name, weight) pair,
        /// resolves the tileset's matching [GROUPn] and re-verifies structural eligibility (1x1, flat,
        /// doorless, crosser-free, pathnode A) rather than trusting the configured name blindly. A
        /// name that doesn't resolve to a tileset group, or resolves to a group that fails the
        /// structural check, is silently skipped — the caller (a tileset profile) may list features
        /// that don't apply to every tileset, or a group's shape may have changed since the profile
        /// was written.
        /// </summary>
        private static Dictionary<string, FeatureCandidateSet> BuildFeatureLookup(
            TilesetModel tileset, IReadOnlyDictionary<string, int> featureTiles)
        {
            var lookup = new Dictionary<string, FeatureCandidateSet>();

            foreach (var (groupName, weight) in featureTiles)
            {
                if (weight <= 0) continue;

                TileGroupRecord group = null;
                foreach (var candidate in tileset.Groups)
                {
                    if (string.Equals(candidate.Name, groupName, StringComparison.OrdinalIgnoreCase))
                    {
                        group = candidate;
                        break;
                    }
                }

                if (group == null) continue;
                if (group.Rows != 1 || group.Columns != 1 || group.TileIds.Count != 1) continue;

                var tileId = group.TileIds[0];
                if (tileId < 0 || tileId >= tileset.Tiles.Count) continue;

                var tile = tileset.Tiles[tileId];

                if (tile.CornerHeights[0] != 0 || tile.CornerHeights[1] != 0 ||
                    tile.CornerHeights[2] != 0 || tile.CornerHeights[3] != 0) continue;
                if (tile.HasAnyCrosser) continue;
                if (tile.Doors.Count != 0) continue;
                if (!string.Equals(tile.PathNode, "A", StringComparison.OrdinalIgnoreCase)) continue;

                for (var orientation = 0; orientation < 4; orientation++)
                {
                    var tl = tile.GetCornerAt(orientation, CornerSlot.TopLeft);
                    var tr = tile.GetCornerAt(orientation, CornerSlot.TopRight);
                    var br = tile.GetCornerAt(orientation, CornerSlot.BottomRight);
                    var bl = tile.GetCornerAt(orientation, CornerSlot.BottomLeft);

                    // Structurally eligible group tiles are crosser-free by construction (checked
                    // above), so their edge tuple is always blank under every orientation.
                    var key = MakeKey(tl, tr, br, bl, string.Empty, string.Empty, string.Empty, string.Empty);

                    if (!lookup.TryGetValue(key, out var set))
                    {
                        set = new FeatureCandidateSet();
                        lookup[key] = set;
                    }

                    set.Candidates.Add((tile.TileId, orientation, weight));
                    set.TotalWeight += weight;
                }
            }

            return lookup;
        }

        /// <summary>Weighted roll over a feature candidate set's entries, consuming one random.Next call.</summary>
        private static (int TileId, int Orientation) PickWeighted(FeatureCandidateSet set, System.Random random)
        {
            var roll = random.Next(set.TotalWeight);
            var cumulative = 0;
            foreach (var candidate in set.Candidates)
            {
                cumulative += candidate.Weight;
                if (roll < cumulative)
                    return (candidate.TileId, candidate.Orientation);
            }

            // Unreachable given TotalWeight is the sum of all Weight values, but keeps the compiler
            // and any future refactor honest.
            var last = set.Candidates[^1];
            return (last.TileId, last.Orientation);
        }

        private static bool IsDoorway(string edge)
        {
            return string.Equals(edge, "Doorway", StringComparison.OrdinalIgnoreCase);
        }

        private static string Describe(string edge)
        {
            return string.IsNullOrEmpty(edge) ? "-" : edge;
        }

        private static string MakeKey(string tl, string tr, string br, string bl, string top, string right, string bottom, string left)
        {
            var cornerPart = string.Join(
                "|",
                (tl ?? string.Empty).ToUpperInvariant(),
                (tr ?? string.Empty).ToUpperInvariant(),
                (br ?? string.Empty).ToUpperInvariant(),
                (bl ?? string.Empty).ToUpperInvariant());

            var edgePart = string.Join(
                "|",
                (top ?? string.Empty).ToUpperInvariant(),
                (right ?? string.Empty).ToUpperInvariant(),
                (bottom ?? string.Empty).ToUpperInvariant(),
                (left ?? string.Empty).ToUpperInvariant());

            return cornerPart + "‖" + edgePart;
        }

        /// <summary>
        /// Test/tooling hook: true if the tileset has at least one (tileId, orientation) candidate for
        /// the given corner+edge combination under the same rules <see cref="TryResolve"/> uses. Builds
        /// the lookup fresh each call — not for use in hot per-cell resolution.
        /// </summary>
        public static bool HasCandidate(
            TilesetModel tileset,
            string tl, string tr, string br, string bl,
            string top, string right, string bottom, string left)
        {
            if (tileset == null) throw new ArgumentNullException(nameof(tileset));

            var lookup = BuildCandidateLookup(tileset);
            var key = MakeKey(tl, tr, br, bl, top, right, bottom, left);
            return lookup.TryGetValue(key, out var set) && set.All.Count > 0;
        }
    }
}
