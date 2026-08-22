#nullable disable
using System;
using System.Collections.Generic;
using System.Linq;
using SWLOR.Toolset.Domain.AreaGeneration.Tileset;

namespace SWLOR.Toolset.Domain.AreaGeneration
{
    /// <summary>
    /// Resolves a corner-granularity <see cref="MacroLayout"/> into concrete (tileId, orientation) picks
    /// from a <see cref="TilesetModel"/>'s tile inventory, matching each tile cell's four world corners
    /// AND four world edges (<see cref="MacroLayout.Crossers"/>) against the tileset's corner-terrain
    /// and edge-crosser data (the same corner/edge-matching model the toolset terrain brush uses).
    ///
    /// Height (elevation) participates the same way, gated per-layout for ironclad back-compat: when
    /// <see cref="MacroLayout.Corners"/>' corner-height grid is entirely zero (every caller until a
    /// layout style paints elevation), TileResolver uses the LEGACY lookup below — flat-cornered
    /// (CornerHeights all 0) tiles only, exactly the v1 pools/RNG sequence, byte-identical output.
    /// Only when a layout's corner-height grid carries any nonzero value does TileResolver switch to
    /// the height-aware lookup, which additionally admits raised tiles: a candidate (tile, orientation)
    /// matches a cell when terrains/edges match AND there exists an integer placementHeight (the
    /// eventual Tile_Height) such that placementHeight + tile.GetCornerHeightAt(orientation, slot) ==
    /// grid height at that corner, for all 4 corners. This is keyed efficiently by normalizing both
    /// sides' 4 corner heights to a 0-based "delta profile" (subtract the min of the 4) before hashing;
    /// two height profiles that differ only by a constant collapse to the same delta profile, and that
    /// shared constant IS placementHeight = gridMin - candidateTileMin (see BuildCandidateLookup).
    ///
    /// Empirically pinned: TileRecord.GetCornerHeightAt's existing (slot + orientation) % 4 rotation
    /// (the same formula corners/edges already use) reproduces zero mismatches across 206,872 adjacent
    /// world-corner-height comparisons drawn from 25 real hand-built tilesets with nonzero corner
    /// height content (see HeightResolutionTests). The same sweep found terrain labels are NOT
    /// height-qualified — every terrain label sampled occurs at multiple heights — so a corner's
    /// identity for matching purposes is the (terrain, height) pair, not terrain alone.
    ///
    /// Scope otherwise unchanged: within a lookup, a tile is a candidate when it is either (a)
    /// crosser-free and door-free — the full v1 tile set, unchanged — or (b) has at least one edge
    /// crosser. Crosser tiles that ALSO carry door slots are only ever registered under keys whose edge
    /// part contains a Doorway or Bridge crosser (a door slot implies a door frame, so such a tile must
    /// never leak into a blank-edge cell) OR one of the composition's own declared extra door-slot
    /// crossers (see <see cref="MacroLayout.DoorSlotCrossers"/>/<see cref="MacroLayoutParameters.DoorSlotCrossers"/>/
    /// DungeonTilesetProfile.DoorSlotCrossers) -- some registered tilesets rename their door-implying
    /// crosser entirely (e.g. Barrows/tbw01's "door_corridor", paired with its own "corridor" body
    /// crosser rather than the canonical Corridor/Doorway pair) rather than merely renaming the body
    /// half the way tdc01's "GreyCorridor" does. Declaring the alternate name here is what lets such a
    /// tile resolve as an ordinary structural tile the same way a canonical Doorway/Bridge door-slot
    /// tile always has -- the door slot itself is still never populated by this resolver (doors are only
    /// ever placed by TileDoorPlanner/GroupExitPlanner at a real TransitionPoint), so an unpopulated
    /// slot here renders exactly like any other unpopulated Doorway-keyed door-slot tile does today.
    /// Door-slot tiles with no crosser at all remain excluded regardless — they are TileDoorPlanner's
    /// post-resolution inventory, not the corner resolver's. Every existing caller passes no extra
    /// crossers (null/empty), so this is fully back-compat: the gate reduces to the original
    /// Doorway-or-Bridge-only check byte-for-byte whenever a composition declares nothing.
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

            // Cheap, once-per-resolve check: every caller until a layout style paints elevation has an
            // all-zero corner-height grid, so heightAware is false and the legacy flat-only lookup
            // below runs unchanged (byte-identical pools/RNG sequence to pre-height behavior).
            var heightAware = layout.Corners.HasAnyHeight();

            var candidateLookup = BuildCandidateLookup(tileset, heightAware, layout.DoorSlotCrossers, layout.ExcludedTiles);
            // Feature sprinkling stays scoped to flat layouts for now (v1): no layout style paints
            // elevation yet, so this is not a behavior change; a future task can extend feature
            // sprinkling to height-aware cells deliberately.
            var featureLookup = !heightAware && layout.FeatureTiles.Count > 0
                ? BuildFeatureLookup(tileset, layout.FeatureTiles, layout.ExcludedTiles)
                : null;
            var tiles = new ResolvedTile[width * height];

            // Cells that have already received a feature tile this resolve, tracked for the spacing
            // rule (no two features within Chebyshev distance 2 of each other).
            var placedFeatures = new List<(int X, int Y)>();
            // The same cells mapped to their feature GROUP name, carried onto ResolvedLayout so
            // downstream dressing can compose an ensemble on area-marking feature tiles (see
            // ResolvedLayout.FeatureTileCells).
            var placedFeatureCells = new Dictionary<(int X, int Y), string>();

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
                            Height = pin.Height
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

                    // gridMin is only meaningful (and only computed) in the height-aware path; legacy
                    // cells are always flat (heightAware false), so this stays 0 and unused there.
                    var gridMin = 0;
                    string key;

                    if (heightAware)
                    {
                        var hTl = layout.Corners.Heights[x, y + 1];
                        var hTr = layout.Corners.Heights[x + 1, y + 1];
                        var hBr = layout.Corners.Heights[x + 1, y];
                        var hBl = layout.Corners.Heights[x, y];
                        gridMin = Math.Min(Math.Min(hTl, hTr), Math.Min(hBr, hBl));

                        key = MakeHeightAwareKey(
                            tl, tr, br, bl, top, right, bottom, left,
                            hTl - gridMin, hTr - gridMin, hBr - gridMin, hBl - gridMin);
                    }
                    else
                    {
                        key = MakeKey(tl, tr, br, bl, top, right, bottom, left);
                    }

                    List<(int TileId, int Orientation, int TileMin)> pool = null;
                    if (candidateLookup.TryGetValue(key, out var candidates))
                    {
                        // A normalized height profile can contain both a ground-level tile and a
                        // plateau tile whose authored corner heights differ only by a positive
                        // constant. NWN Tile_Height is unsigned in practice, so the latter is not a
                        // viable choice when its own minimum exceeds this cell's grid minimum.
                        var viableAll = heightAware
                            ? candidates.All.Where(candidate => candidate.TileMin <= gridMin).ToList()
                            : candidates.All;
                        var viableFullyPathable = heightAware
                            ? candidates.FullyPathable
                                .Where(candidate => candidate.TileMin <= gridMin)
                                .ToList()
                            : candidates.FullyPathable;
                        pool = viableFullyPathable.Count > 0 ? viableFullyPathable : viableAll;
                    }

                    if (pool == null || pool.Count == 0)
                    {
                        var edgeNote = string.Empty;
                        if (!string.IsNullOrEmpty(top) || !string.IsNullOrEmpty(right) ||
                            !string.IsNullOrEmpty(bottom) || !string.IsNullOrEmpty(left))
                        {
                            edgeNote =
                                $" Edges: Top={Describe(top)}, Right={Describe(right)}, Bottom={Describe(bottom)}, Left={Describe(left)}.";
                        }

                        var heightNote = heightAware
                            ? $" Heights: TL={layout.Corners.Heights[x, y + 1]}, TR={layout.Corners.Heights[x + 1, y + 1]}, BR={layout.Corners.Heights[x + 1, y]}, BL={layout.Corners.Heights[x, y]}."
                            : string.Empty;

                        failureReason =
                            $"No matching tile for cell ({x},{y}): TL={tl}, TR={tr}, BR={br}, BL={bl}.{edgeNote}{heightNote}";
                        resolved = null;
                        return false;
                    }

                    // Feature sprinkling: only ever rolled when this configuration has feature tiles
                    // AND this specific cell's key has a matching feature candidate. This ordering is
                    // load-bearing for determinism/back-compat — when layout.FeatureTiles is empty
                    // (every caller until a tileset profile stamps it), featureLookup is null and this
                    // whole block is skipped with zero extra random calls, so existing seeds/tests
                    // resolve with exactly the pre-feature RNG sequence (one random.Next per cell).
                    var (tileId, orientation, tileMin) = (pool[0].TileId, pool[0].Orientation, pool[0].TileMin);
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
                                placedFeatureCells[(x, y)] = featurePick.GroupName;
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
                        tileMin = pick.TileMin;
                    }

                    // placementHeight = gridMin - candidateTileMin (see class doc): the constant that,
                    // added to the candidate's own corner heights, reproduces the grid's corner
                    // heights at every one of the 4 corners. Always 0 outside the height-aware path
                    // (gridMin and tileMin are both 0 there), so legacy output is unchanged.
                    tiles[y * width + x] = new ResolvedTile
                    {
                        TileId = tileId,
                        Orientation = orientation,
                        Height = heightAware ? gridMin - tileMin : 0
                    };
                }
            }

            if (layout.DoorTransitions)
            {
                // GroupExitPlanner runs first so themed exit-group tiles get first pick of a room's
                // wall cells; any Exit transition it can't place falls through unchanged to
                // TileDoorPlanner (a real generic door) and then plain Placeable.
                GroupExitPlanner.ApplyGroupExits(tileset, layout, tiles, width, height);
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
                OpenTerrain = layout.OpenTerrain,
                SecondaryOpenTerrain = layout.SecondaryOpenTerrain,
                Crossers = layout.Crossers,
                StampedStructureTiles = layout.StampedOpenSetPieceFootprints
                    .SelectMany(f => f)
                    .ToHashSet(),
                FeatureTileCells = placedFeatureCells,
                CornerTerrains = layout.Corners,
                HeightTransition = tileset.HasHeightTransition ? tileset.HeightTransition : 0f
            };
            failureReason = null;
            return true;
        }

        /// <summary>
        /// Builds a lookup from a case-insensitive (TL, TR, BR, BL, Top, Right, Bottom, Left[, height
        /// delta profile]) key to every (tileId, orientation, tileMin) candidate satisfying the
        /// resolution rules. Built once per resolve call rather than scanning all tiles per cell.
        ///
        /// Rotation permutes a tile's fixed Corners/Edges/CornerHeights arrays, so "has any crosser at
        /// all" and a tile's raw min corner height are rotation-invariant — checked/computed once on the
        /// raw arrays rather than once per orientation. Back-compat with the pre-crosser resolver is by
        /// construction: a crosser-free, door-free tile's oriented edge tuple is "","","","" under every
        /// orientation (rotating four blanks yields four blanks), so it only ever registers under a key
        /// whose edge part is fully blank — exactly the set, order, and per-key grouping the v1 resolver
        /// produced before edges existed in the key. A tile with HasAnyCrosser true can never rotate to
        /// an all-blank edge tuple (rotation only permutes the existing non-blank value(s), it can't
        /// erase them), so it can never appear under a fully-blank-edge key and therefore never disturbs
        /// the blank-edge candidate pools crosser-free layouts resolve against.
        ///
        /// <paramref name="heightAware"/> selects between two disjoint, differently-shaped lookups
        /// rather than one lookup that happens to include more tiles when true — this is the back-compat
        /// guard: when false (every existing caller), only flat-cornered (CornerHeights all 0) tiles are
        /// registered, under the plain (no height suffix) key, EXACTLY reproducing the pre-height
        /// resolver's pools/iteration-order/RNG sequence bit for bit. When true, EVERY tile (flat or
        /// raised) is registered, keyed by its rotated corner-height "delta profile" (each of the 4
        /// rotated corner heights minus their own min, so two tiles/orientations whose corner heights
        /// differ only by a constant land under the same key) alongside the usual terrain/edge parts.
        /// This deliberately does NOT reduce to the legacy lookup's flat-key pools when heights happen to
        /// be all zero: real tilesets contain "plateau-top" tiles (uniform nonzero corner height, e.g.
        /// wsf10 TILE2316/2318/2452/2454, h=1) whose delta profile also normalizes to all-zero, and admitting
        /// those under the flat key (at a nonzero placementHeight) would change the legacy flat-key pools
        /// for every existing caller. Per-layout gating in TryResolve (heightAware = false unless a
        /// layout actually painted a nonzero corner height somewhere) means this never happens: no
        /// current caller ever reaches the heightAware=true lookup at all.
        /// </summary>
        private class CandidateSet
        {
            public List<(int TileId, int Orientation, int TileMin)> All { get; } = new();
            public List<(int TileId, int Orientation, int TileMin)> FullyPathable { get; } = new();
        }

        private static Dictionary<string, CandidateSet> BuildCandidateLookup(
            TilesetModel tileset, bool heightAware, IReadOnlyCollection<string> extraDoorSlotCrossers = null,
            IReadOnlyCollection<int> excludedTiles = null)
        {
            var lookup = new Dictionary<string, CandidateSet>();

            foreach (var tile in tileset.Tiles)
            {
                if (tile.GroupIndex != -1) continue;

                // Confirmed placeholder/stub art (see DungeonTilesetProfile.ExcludedTiles) -- excluded
                // at this single shared candidate-building level so every placement path that draws
                // from this lookup (legacy flat, height-aware) skips it uniformly. Checked before the
                // heightAware/isFlat gate below so it applies regardless of which lookup shape is being
                // built. Does NOT affect LayoutGroupStamper's pinned-tile path (bypasses this lookup
                // entirely) -- see ExcludedTileRegressionTests for the static guarantee that no excluded
                // ID is ever a wired SetPieces/ExitGroups group member.
                if (excludedTiles != null && excludedTiles.Count > 0 && excludedTiles.Contains(tile.TileId))
                    continue;

                var isFlat = tile.CornerHeights[0] == 0 && tile.CornerHeights[1] == 0 &&
                             tile.CornerHeights[2] == 0 && tile.CornerHeights[3] == 0;

                // Legacy scope: flat-cornered tiles only (unchanged from the pre-height resolver).
                if (!heightAware && !isFlat) continue;

                var tileMin = isFlat
                    ? 0
                    : Math.Min(Math.Min(tile.CornerHeights[0], tile.CornerHeights[1]),
                        Math.Min(tile.CornerHeights[2], tile.CornerHeights[3]));

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
                    // edge part carries a Doorway (or Bridge — a door built directly into a bridge
                    // bank's wall-facing edge, verified on vmr01 TILE100) crosser somewhere, OR one of
                    // this composition's own declared extra door-slot crossers (see class doc comment;
                    // extraDoorSlotCrossers is null/empty for every caller that declares nothing, so
                    // this is a no-op there) — a door slot implies a door frame, so it must never leak
                    // into a cell that doesn't expect one.
                    if (hasCrosser && hasDoors)
                    {
                        var hasDoorwayEdge =
                            IsDoorway(top) || IsDoorway(right) || IsDoorway(bottom) || IsDoorway(left);
                        var hasBridgeEdge =
                            IsBridge(top) || IsBridge(right) || IsBridge(bottom) || IsBridge(left);
                        var hasExtraEdge = extraDoorSlotCrossers != null && extraDoorSlotCrossers.Count > 0 &&
                            (IsExtra(top, extraDoorSlotCrossers) || IsExtra(right, extraDoorSlotCrossers) ||
                             IsExtra(bottom, extraDoorSlotCrossers) || IsExtra(left, extraDoorSlotCrossers));
                        if (!hasDoorwayEdge && !hasBridgeEdge && !hasExtraEdge) continue;
                    }

                    string key;
                    if (heightAware)
                    {
                        var dTl = tile.GetCornerHeightAt(orientation, CornerSlot.TopLeft) - tileMin;
                        var dTr = tile.GetCornerHeightAt(orientation, CornerSlot.TopRight) - tileMin;
                        var dBr = tile.GetCornerHeightAt(orientation, CornerSlot.BottomRight) - tileMin;
                        var dBl = tile.GetCornerHeightAt(orientation, CornerSlot.BottomLeft) - tileMin;
                        key = MakeHeightAwareKey(tl, tr, br, bl, top, right, bottom, left, dTl, dTr, dBr, dBl);
                    }
                    else
                    {
                        key = MakeKey(tl, tr, br, bl, top, right, bottom, left);
                    }

                    if (!lookup.TryGetValue(key, out var set))
                    {
                        set = new CandidateSet();
                        lookup[key] = set;
                    }

                    set.All.Add((tile.TileId, orientation, tileMin));
                    if (fullyPathable)
                        set.FullyPathable.Add((tile.TileId, orientation, tileMin));
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
            public List<(int TileId, int Orientation, int Weight, string GroupName)> Candidates { get; } = new();
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
            TilesetModel tileset, IReadOnlyDictionary<string, int> featureTiles,
            IReadOnlyCollection<int> excludedTiles = null)
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
                // Confirmed placeholder/stub art (see DungeonTilesetProfile.ExcludedTiles) -- excluded
                // from feature sprinkling too, the third of the three placement paths this mechanism
                // must cover uniformly (legacy flat / height-aware candidate lookup above, feature
                // sprinkling here).
                if (excludedTiles != null && excludedTiles.Count > 0 && excludedTiles.Contains(tileId)) continue;

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

                    // Recorded under the CONFIGURED group name (the FeatureTiles key), so downstream
                    // dressing lookups (FeatureTileDressings, keyed the same way) match trivially.
                    set.Candidates.Add((tile.TileId, orientation, weight, groupName));
                    set.TotalWeight += weight;
                }
            }

            return lookup;
        }

        /// <summary>Weighted roll over a feature candidate set's entries, consuming one random.Next call.</summary>
        private static (int TileId, int Orientation, string GroupName) PickWeighted(FeatureCandidateSet set, System.Random random)
        {
            var roll = random.Next(set.TotalWeight);
            var cumulative = 0;
            foreach (var candidate in set.Candidates)
            {
                cumulative += candidate.Weight;
                if (roll < cumulative)
                    return (candidate.TileId, candidate.Orientation, candidate.GroupName);
            }

            // Unreachable given TotalWeight is the sum of all Weight values, but keeps the compiler
            // and any future refactor honest.
            var last = set.Candidates[^1];
            return (last.TileId, last.Orientation, last.GroupName);
        }

        private static bool IsDoorway(string edge)
        {
            return string.Equals(edge, "Doorway", StringComparison.OrdinalIgnoreCase);
        }

        private static bool IsBridge(string edge)
        {
            return string.Equals(edge, "Bridge", StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>True when <paramref name="edge"/> case-insensitively matches one of a composition's
        /// declared extra door-slot crossers (see class doc comment). <paramref name="edge"/> may be
        /// null/blank (an unset edge slot); <paramref name="extraCrossers"/> is assumed non-null/non-empty
        /// by every call site (checked before calling).</summary>
        private static bool IsExtra(string edge, IReadOnlyCollection<string> extraCrossers)
        {
            if (string.IsNullOrEmpty(edge)) return false;
            foreach (var candidate in extraCrossers)
            {
                if (string.Equals(edge, candidate, StringComparison.OrdinalIgnoreCase)) return true;
            }
            return false;
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
        /// Height-aware variant of <see cref="MakeKey"/>: appends the cell/tile's normalized ([TL, TR,
        /// BR, BL] minus their own min) corner-height delta profile as a third key segment. Two cells or
        /// candidates whose raw corner heights differ only by a constant land under the same key —
        /// that shared constant is the placementHeight this candidate must be placed at (see
        /// BuildCandidateLookup/TryResolve).
        /// </summary>
        private static string MakeHeightAwareKey(
            string tl, string tr, string br, string bl,
            string top, string right, string bottom, string left,
            int dTl, int dTr, int dBr, int dBl)
        {
            var baseKey = MakeKey(tl, tr, br, bl, top, right, bottom, left);
            var heightPart = string.Join("|", dTl, dTr, dBr, dBl);
            return baseKey + "‖" + heightPart;
        }

        /// <summary>
        /// Test/tooling hook: true if the tileset has at least one (tileId, orientation) FLAT candidate
        /// for the given corner+edge combination, under the same legacy rules <see cref="TryResolve"/>
        /// uses when a layout's corner-height grid is all zero. Builds the lookup fresh each call — not
        /// for use in hot per-cell resolution.
        /// </summary>
        public static bool HasCandidate(
            TilesetModel tileset,
            string tl, string tr, string br, string bl,
            string top, string right, string bottom, string left,
            IReadOnlyCollection<string> extraDoorSlotCrossers = null,
            IReadOnlyCollection<int> excludedTiles = null)
        {
            if (tileset == null) throw new ArgumentNullException(nameof(tileset));

            var lookup = BuildCandidateLookup(tileset, heightAware: false, extraDoorSlotCrossers, excludedTiles);
            var key = MakeKey(tl, tr, br, bl, top, right, bottom, left);
            return lookup.TryGetValue(key, out var set) && set.All.Count > 0;
        }

        /// <summary>
        /// Test/tooling hook: true if the tileset has at least one (tileId, orientation) candidate for
        /// the given corner+edge combination AND normalized corner-height delta profile, under the
        /// same height-aware rules <see cref="TryResolve"/> uses once a layout paints any nonzero
        /// corner height (see BuildCandidateLookup's heightAware=true lookup). Used by shape-gated
        /// height-painting passes (e.g. LayoutElevationPainter) to verify a tileset's real tile
        /// inventory actually covers a rim shape before committing to paint it — mirroring how
        /// TunnelVocabularyCheck/LayoutFenceCarver/LayoutAccentChannelCarver probe capability before
        /// carving. Builds the lookup fresh each call — not for use in hot per-cell resolution or any
        /// mechanism issuing more than a handful of probes per Paint() call; see
        /// <see cref="BuildHeightAwareProbeCache"/> for that case.
        /// </summary>
        public static bool HasHeightAwareCandidate(
            TilesetModel tileset,
            string tl, string tr, string br, string bl,
            string top, string right, string bottom, string left,
            int dTl, int dTr, int dBr, int dBl)
        {
            if (tileset == null) throw new ArgumentNullException(nameof(tileset));

            var lookup = BuildCandidateLookup(tileset, heightAware: true);
            var key = MakeHeightAwareKey(tl, tr, br, bl, top, right, bottom, left, dTl, dTr, dBr, dBl);
            return lookup.TryGetValue(key, out var set) && set.All.Count > 0;
        }

        /// <summary>
        /// Opaque, reusable wrapper around a height-aware candidate lookup, built once and probed many
        /// times via <see cref="HasHeightAwareCandidate(HeightAwareProbeCache,string,string,string,string,string,string,string,string,int,int,int,int)"/>.
        /// <see cref="BuildCandidateLookup"/> scans every tile in the tileset and is not free (tdm01's
        /// 1810-tile inventory takes low-single-digit milliseconds) — cheap for the couple of upfront
        /// probes <see cref="Layouts.LayoutElevationPainter"/>/<see cref="Layouts.LayoutElevationPoolPainter"/>
        /// used to make per Paint() call, but any mechanism issuing dozens of probes per placement
        /// attempt (e.g. corner-by-corner irregular region growth, re-verifying several touching cells
        /// on every grown corner) must build this ONCE per Paint() call and reuse it, or generation time
        /// regresses badly across many placement attempts.
        /// </summary>
        public sealed class HeightAwareProbeCache
        {
            // private (not internal): CandidateSet itself is a private nested type of TileResolver, so
            // this field's accessibility must not exceed it. Only ever set by BuildHeightAwareProbeCache
            // and read by the cache-based HasHeightAwareCandidate overload below -- both members of the
            // enclosing TileResolver class, which (like any enclosing type) can see this nested class's
            // private members directly.
            private Dictionary<string, CandidateSet> Lookup;

            private static HeightAwareProbeCache From(Dictionary<string, CandidateSet> lookup)
            {
                return new HeightAwareProbeCache { Lookup = lookup };
            }

            private bool TryGetSet(string key, out CandidateSet set) => Lookup.TryGetValue(key, out set);

            internal static HeightAwareProbeCache Build(
                TilesetModel tileset,
                IReadOnlyCollection<string> extraDoorSlotCrossers,
                IReadOnlyCollection<int> excludedTiles) =>
                From(BuildCandidateLookup(tileset, heightAware: true, extraDoorSlotCrossers, excludedTiles));

            internal bool HasCandidate(string key) => TryGetSet(key, out var set) && set.All.Count > 0;

            internal bool HasCandidate(string key, int maximumTileMin) =>
                TryGetSet(key, out var set) && set.All.Any(candidate => candidate.TileMin <= maximumTileMin);
        }

        /// <summary>Builds a <see cref="HeightAwareProbeCache"/> for repeated height-aware probes against
        /// <paramref name="tileset"/> within a single post-pass invocation.</summary>
        public static HeightAwareProbeCache BuildHeightAwareProbeCache(
            TilesetModel tileset,
            IReadOnlyCollection<string> extraDoorSlotCrossers = null,
            IReadOnlyCollection<int> excludedTiles = null)
        {
            if (tileset == null) throw new ArgumentNullException(nameof(tileset));
            return HeightAwareProbeCache.Build(tileset, extraDoorSlotCrossers, excludedTiles);
        }

        /// <summary>Cache-based twin of <see cref="HasHeightAwareCandidate(TilesetModel,string,string,string,string,string,string,string,string,int,int,int,int)"/>
        /// — identical matching rules, but reads a lookup built once via <see cref="BuildHeightAwareProbeCache"/>
        /// instead of rebuilding it on every call. Use this for any mechanism that probes more than a
        /// handful of candidates per Paint() call.</summary>
        public static bool HasHeightAwareCandidate(
            HeightAwareProbeCache cache,
            string tl, string tr, string br, string bl,
            string top, string right, string bottom, string left,
            int dTl, int dTr, int dBr, int dBl)
        {
            if (cache == null) throw new ArgumentNullException(nameof(cache));

            var key = MakeHeightAwareKey(tl, tr, br, bl, top, right, bottom, left, dTl, dTr, dBr, dBl);
            return cache.HasCandidate(key);
        }

        /// <summary>
        /// Placement-aware cached height probe. In addition to matching the normalized corner-height
        /// profile, requires a candidate whose authored minimum height does not exceed
        /// <paramref name="gridMin"/>, exactly mirroring the viability filter in <see cref="TryResolve"/>.
        /// </summary>
        public static bool HasHeightAwareCandidate(
            HeightAwareProbeCache cache,
            string tl, string tr, string br, string bl,
            string top, string right, string bottom, string left,
            int dTl, int dTr, int dBr, int dBl,
            int gridMin)
        {
            if (cache == null) throw new ArgumentNullException(nameof(cache));

            var key = MakeHeightAwareKey(tl, tr, br, bl, top, right, bottom, left, dTl, dTr, dBr, dBl);
            return cache.HasCandidate(key, gridMin);
        }
    }
}
