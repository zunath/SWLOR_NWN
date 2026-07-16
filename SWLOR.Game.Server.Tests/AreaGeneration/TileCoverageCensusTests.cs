using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Feature.DungeonDefinition;
using SWLOR.Game.Server.Service.AreaGenerationService;

namespace SWLOR.Game.Server.Tests.AreaGeneration;

/// <summary>
/// Final-phase acceptance test for the 100% tile-coverage effort: enumerates EVERY tile in all four
/// generation tilesets' real .set data and classifies each as reachable by one of the generator's
/// shipped mechanisms, or lists it in an exact, reasoned exemption set. The classifier mirrors the
/// real production constraints (TileResolver, TileDoorPlanner, GroupExitPlanner,
/// LayoutGroupStamper) rather than trusting any tileset profile's configured lists -- a tile counts
/// as reachable when it structurally qualifies for a mechanism, whether or not the shipped
/// StandardTilesetProfiles entry happens to enable it for that tileset (an "optional config" tile
/// is exactly as reachable as a currently-wired one, since the mechanism's own structural rule is
/// what a maintainer would extend a profile with).
///
/// This is intentionally a read-only census: it duplicates classification logic (mirroring the
/// existing convention in GroupStamperTests/FeatureTileTests/GroupExitAndCorridorInsertTests, which
/// already duplicate SlotOffsets/corner-key helpers since the production classes are internal with
/// no InternalsVisibleTo) rather than reaching into internals via reflection.
/// </summary>
public class TileCoverageCensusTests
{
    private static TilesetModel LoadTileset(string tilesetResref) => TilesetTestSource.LoadTileset(tilesetResref);

    private const string DoorwayCrosser = "Doorway";
    private const string CorridorCrosser = "Corridor";
    private const string AlleyCrosser = "Alley";
    private const string FenceCrosser = "Fence";
    private const string BridgeCrosser = "Bridge";

    private static bool Eq(string a, string b) => string.Equals(a ?? string.Empty, b ?? string.Empty, StringComparison.OrdinalIgnoreCase);

    private static bool IsFlat(TileRecord tile) =>
        tile.CornerHeights[0] == 0 && tile.CornerHeights[1] == 0 && tile.CornerHeights[2] == 0 && tile.CornerHeights[3] == 0;

    /// <summary>The tileset's declared generation vocabulary — the same terrain/crosser labels a
    /// StandardTilesetProfiles entry declares (solid = the tileset's Default terrain always).</summary>
    private sealed class TilesetVocabulary
    {
        public string Solid = string.Empty;
        public string Open = string.Empty;
        public string Secondary = string.Empty;
        public string Accent = string.Empty; // blob-patch terrain (LayoutAccentPainter)
        public string Channel = string.Empty; // channel/bank terrain (LayoutAccentChannelCarver), falls back to Accent

        /// <summary>
        /// Tunnel body/port crosser names this profile carves -- canonical "Corridor"/"Doorway" unless
        /// the profile declares an alternate district-scoped family (see
        /// DungeonTilesetProfile.TunnelBodyCrosser/TunnelPortCrosser, mirrored here so
        /// IsCorridorInsertEligible/IsCorridorStubEligible/ClassifyMultiTileSetPiece credit the SAME
        /// alternate vocabulary LayoutGroupStamper.CorridorInsertCrossersFor/CorridorStubCrossersFor
        /// and LayoutTunnelCarver actually carve for a Custom-mode composition).
        /// </summary>
        public string TunnelBody = CorridorCrosser;
        public string TunnelPort = DoorwayCrosser;

        /// <summary>
        /// Crosser names (beyond canonical Doorway/Bridge) this profile declares as door-implying for
        /// TileResolver's crosser+door-slot admission gate -- mirrors
        /// DungeonTilesetProfile.DoorSlotCrossers/MacroLayoutParameters.DoorSlotCrossers, so
        /// IsCornerEdgeResolverReachable credits the SAME alternate vocabulary TileResolver.
        /// BuildCandidateLookup actually admits for a real composition (e.g. Barrows/tbw01's
        /// "door_corridor").
        /// </summary>
        public IReadOnlyCollection<string> ExtraDoorSlotCrossers = Array.Empty<string>();

        /// <summary>Slope-blend terrain LayoutReliefPainter may flip open corners to -- mirrors
        /// DungeonTilesetProfile.ReliefBlendTerrain (e.g. tdm01's GentleSlope/GentleDesert/
        /// GentleOrganic). Empty = relief perturbs heights only.</summary>
        public string Blend = string.Empty;

        /// <summary>Ramp-lane crosser name this profile's lane splicing writes -- mirrors
        /// DungeonTilesetProfile.RampCrosser, canonical "Ramp" when undeclared (e.g. tdm01's
        /// "Slope").</summary>
        public string Ramp = "Ramp";
    }

    private static TilesetVocabulary BuildVocabulary(TilesetModel model, DungeonTilesetProfile profile)
    {
        return new TilesetVocabulary
        {
            // Mirrors LayoutSolver.Solve's empty-means-Default solid stamp: an exterior profile may
            // invert solid/open (see DungeonTilesetProfile.SolidTerrainOverride).
            Solid = string.IsNullOrEmpty(profile.SolidTerrainOverride) ? model.DefaultTerrain : profile.SolidTerrainOverride,
            Open = string.IsNullOrEmpty(profile.PrimaryOpenTerrain) ? model.FloorTerrain : profile.PrimaryOpenTerrain,
            Secondary = profile.SecondaryOpenTerrain ?? string.Empty,
            Accent = profile.AccentTerrain ?? string.Empty,
            Channel = !string.IsNullOrEmpty(profile.ChannelTerrain) ? profile.ChannelTerrain : (profile.AccentTerrain ?? string.Empty),
            TunnelBody = !string.IsNullOrEmpty(profile.TunnelBodyCrosser) ? profile.TunnelBodyCrosser : CorridorCrosser,
            TunnelPort = !string.IsNullOrEmpty(profile.TunnelPortCrosser) ? profile.TunnelPortCrosser : DoorwayCrosser,
            ExtraDoorSlotCrossers = (IReadOnlyCollection<string>)profile.DoorSlotCrossers ?? Array.Empty<string>(),
            Blend = profile.ReliefBlendTerrain ?? string.Empty,
            Ramp = !string.IsNullOrEmpty(profile.RampCrosser) ? profile.RampCrosser : "Ramp",
        };
    }

    // ---------------- ungrouped tile mechanisms ----------------

    private static bool IsDoorway(string edge) => Eq(edge, DoorwayCrosser);
    private static bool IsBridge(string edge) => Eq(edge, BridgeCrosser);

    /// <summary>Mirrors TileResolver.BuildCandidateLookup's registration rule for a single tile: flat,
    /// ungrouped, and either crosser-free, or crosser-bearing with any door slot facing a Doorway/Bridge
    /// edge (or one of the profile's own declared extra door-slot crossers -- see
    /// TilesetVocabulary.ExtraDoorSlotCrossers). Uses the real public TileResolver.HasCandidate hook so
    /// this is checking actual production behavior, not a re-guessed copy of it.</summary>
    private static bool IsCornerEdgeResolverReachable(TilesetModel model, TileRecord tile, TilesetVocabulary vocab)
    {
        if (tile.GroupIndex != -1) return false;
        if (!IsFlat(tile)) return false;

        var hasCrosser = tile.HasAnyCrosser;
        var hasDoors = tile.Doors.Count != 0;
        if (hasDoors && !hasCrosser) return false; // TileDoorPlanner's inventory instead

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

            if (hasCrosser && hasDoors)
            {
                var hasDoorwayEdge = IsDoorway(top) || IsDoorway(right) || IsDoorway(bottom) || IsDoorway(left);
                var hasBridgeEdge = IsBridge(top) || IsBridge(right) || IsBridge(bottom) || IsBridge(left);
                var hasExtraEdge = vocab.ExtraDoorSlotCrossers.Count > 0 &&
                    (IsExtra(top, vocab) || IsExtra(right, vocab) || IsExtra(bottom, vocab) || IsExtra(left, vocab));
                if (!hasDoorwayEdge && !hasBridgeEdge && !hasExtraEdge) continue;
            }

            if (TileResolver.HasCandidate(model, tl, tr, br, bl, top, right, bottom, left, vocab.ExtraDoorSlotCrossers))
                return true;
        }

        return false;
    }

    private static bool IsExtra(string edge, TilesetVocabulary vocab)
    {
        if (string.IsNullOrEmpty(edge)) return false;
        foreach (var candidate in vocab.ExtraDoorSlotCrossers)
        {
            if (Eq(edge, candidate)) return true;
        }
        return false;
    }

    /// <summary>Mirrors TileDoorPlanner's TryGetSingleDoorwaySlot + door-slot requirement, covering both
    /// its room-edge pool (any corner pattern) and terminator pool (all-solid corners) uniformly: a
    /// flat, door-bearing tile with exactly one rotated Doorway edge (the other three blank) is
    /// reachable at SOME real room boundary, since boundary corner patterns are produced freely by
    /// every layout style and TileDoorPlanner's room-edge candidates are keyed purely by corner pattern
    /// with no restriction of their own.
    ///
    /// A grouped tile only qualifies via the terminator pool (BuildTerminatorCandidates' post-Fix-A
    /// IsSingleCellGroup tolerance -- see TileDoorPlanner): its own group must be a trivial 1x1 entry
    /// AND its corners must be all-solid (the room-edge pool stays ungrouped-only regardless of
    /// corner pattern).</summary>
    private static bool IsDoorTransitionReachable(TilesetModel model, TileRecord tile)
    {
        if (!IsFlat(tile)) return false;
        if (tile.Doors.Count == 0) return false;

        if (tile.GroupIndex != -1)
        {
            var group = model.Groups[tile.GroupIndex];
            if (group.Rows != 1 || group.Columns != 1) return false;
            if (!tile.Corners.All(c => Eq(c, model.DefaultTerrain))) return false;
        }

        for (var orientation = 0; orientation < 4; orientation++)
        {
            var found = 0;
            var ok = true;
            for (var slot = 0; slot < 4; slot++)
            {
                var edge = tile.GetEdgeAt(orientation, slot) ?? string.Empty;
                if (edge.Length == 0) continue;
                if (!IsDoorway(edge)) { ok = false; break; }
                found++;
            }

            if (ok && found == 1) return true;
        }

        return false;
    }

    /// <summary>Mirrors LayoutElevationPainter's rim shapes: a flat-corner-terrain (uniform Solid or
    /// Open), ungrouped, blank-edge, doorless tile whose normalized corner-height delta profile is a
    /// single raised corner (a rectangle blob's convex outer corner), two ADJACENT corners raised to
    /// the same delta (a rectangle blob's straight edge), or three corners raised to the same delta (a
    /// CONCAVE notch -- reachable only via LayoutElevationPainter.TryGrowIrregularOpenBlob's
    /// corner-by-corner irregular growth, e.g. an L-shaped region's inner bend; a plain filled rectangle
    /// can never produce this shape, see TryPlaceRectangle's own doc comment). The blob's own
    /// interior/exterior cells are already covered by IsCornerEdgeResolverReachable (a ground-level flat
    /// tile resolves at any placementHeight once height-awareness is active, see TileResolver class doc)
    /// -- this classifier only needs to add the rim shapes the painter actually paints.
    ///
    /// Deliberately does NOT accept a 2-corner DIAGONAL delta profile (opposite corners raised, same
    /// terrain): TryGrowIrregularOpenBlob only ever grows ONE 4-connected region per placement, and
    /// reaching a diagonal corner from its neighbor necessarily passes through one of the two shared
    /// ADJACENT corners first, which would already be a region member -- making that cell's shape
    /// 3-of-4 (or 4-of-4), never exactly the 2 diagonal corners with the other 2 still flat. A true
    /// diagonal-only tile (e.g. tdm01 TILE503, all-Floor h=[1,0,1,0]) would require two entirely
    /// separate regions to coincidentally meet at one cell's diagonal -- not a shape either painter
    /// mechanism can reliably produce, so it stays a genuine exemption.</summary>
    private static bool IsElevationBlobReachable(TileRecord tile, TilesetVocabulary vocab)
    {
        if (tile.GroupIndex != -1) return false;
        if (tile.HasAnyCrosser) return false;
        if (tile.Doors.Count != 0) return false;

        foreach (var terrain in new[] { vocab.Solid, vocab.Open })
        {
            if (string.IsNullOrEmpty(terrain)) continue;
            if (!tile.Corners.All(c => Eq(c, terrain))) continue;

            var heights = tile.CornerHeights;
            var min = heights.Min();
            var normalized = heights.Select(h => h - min).ToArray();
            var nonZero = normalized.Where(h => h != 0).ToArray();
            if (nonZero.Length == 0) continue; // flat -- already covered by CornerEdgeResolver
            if (nonZero.Length == 1) return true; // one-corner rim
            if (nonZero.Length == 3 && nonZero.Distinct().Count() == 1) return true; // concave notch (irregular growth only)

            if (nonZero.Length == 2 && nonZero.Distinct().Count() == 1)
            {
                var tl = normalized[0] != 0; var tr = normalized[1] != 0;
                var br = normalized[2] != 0; var bl = normalized[3] != 0;
                var adjacent = (tl && tr) || (tr && br) || (br && bl) || (bl && tl);
                if (adjacent) return true;
            }
        }

        return false;
    }

    /// <summary>Mirrors LayoutElevationPainter.TryAddRampLane's shape: a RAISED (non-flat), ungrouped,
    /// doorless, all-OpenTerrain-corner tile with the "two adjacent corners raised" delta profile (the
    /// same shape IsElevationBlobReachable's edge case already covers) plus one or two "Ramp" edges on
    /// the axis perpendicular to the height transition (the other two edges always blank). Distinguished
    /// from IsElevationBlobReachable by requiring at least one Ramp edge -- that method's own
    /// HasAnyCrosser guard means it never overlaps this one.</summary>
    private static bool IsElevationRampReachable(TileRecord tile, TilesetVocabulary vocab)
    {
        if (tile.GroupIndex != -1) return false;
        if (tile.Doors.Count != 0) return false;
        if (string.IsNullOrEmpty(vocab.Open)) return false;
        if (!tile.Corners.All(c => Eq(c, vocab.Open))) return false;

        var heights = tile.CornerHeights;
        var min = heights.Min();
        var normalized = heights.Select(h => h - min).ToArray();
        var nonZero = normalized.Count(h => h != 0);
        if (nonZero != 2 || normalized.Where(h => h != 0).Distinct().Count() != 1) return false;

        bool tl = normalized[0] != 0, tr = normalized[1] != 0, br = normalized[2] != 0, bl = normalized[3] != 0;
        var adjacent = (tl && tr) || (tr && br) || (br && bl) || (bl && tl);
        if (!adjacent) return false;

        var sawRamp = false;
        foreach (var edge in tile.Edges)
        {
            if (string.IsNullOrEmpty(edge)) continue;
            if (!Eq(edge, "Ramp")) return false;
            sawRamp = true;
        }

        return sawRamp;
    }

    /// <summary>Mirrors LayoutElevationPoolPainter.TryGrowIrregularPoolInterior's boundary shapes: a
    /// raised (non-flat), blank-edge, doorless tile whose corners mix EXACTLY two terrains -- this
    /// layout's OpenTerrain and the tileset's Accent (pool) terrain, with at least one corner of each --
    /// where every Open corner shares one height and every Accent corner shares a height exactly
    /// RaiseDelta (1) below it, and the Accent corners form a single corner (one Accent corner, three
    /// Open), a straight edge (two ADJACENT Accent corners), or a CONCAVE notch (three Accent corners,
    /// one Open) -- the last reachable only via the irregular corner-by-corner interior grower (a
    /// blanket rectangle fill can never leave a single Open corner poking into an otherwise-filled
    /// interior). Never a 2-corner DIAGONAL split: the interior grower only ever grows ONE 4-connected
    /// region from a single seed, so reaching a diagonal corner without first passing through (and thus
    /// including) one of the two shared adjacent corners is topologically impossible -- see
    /// IsElevationBlobReachable's own doc comment for the identical reasoning on the single-terrain
    /// blob case.</summary>
    private static bool IsPoolBankReachable(TileRecord tile, TilesetVocabulary vocab)
    {
        if (tile.GroupIndex != -1) return false;
        if (tile.HasAnyCrosser) return false;
        if (tile.Doors.Count != 0) return false;
        if (string.IsNullOrEmpty(vocab.Open) || string.IsNullOrEmpty(vocab.Accent)) return false;

        var corners = tile.Corners;
        if (!corners.All(c => Eq(c, vocab.Open) || Eq(c, vocab.Accent))) return false;

        var openCount = corners.Count(c => Eq(c, vocab.Open));
        if (openCount == 0 || openCount == 4) return false; // uniform-terrain -- a different bucket

        var heights = tile.CornerHeights;
        int? openHeight = null, accentHeight = null;
        for (var i = 0; i < 4; i++)
        {
            if (Eq(corners[i], vocab.Open))
            {
                if (openHeight.HasValue && openHeight.Value != heights[i]) return false;
                openHeight = heights[i];
            }
            else
            {
                if (accentHeight.HasValue && accentHeight.Value != heights[i]) return false;
                accentHeight = heights[i];
            }
        }
        if (openHeight!.Value - accentHeight!.Value != 1) return false;

        var accentCount = 4 - openCount;
        if (accentCount == 1) return true;
        if (accentCount == 3) return true; // concave notch (irregular interior growth only)
        if (accentCount == 2)
        {
            bool tlA = Eq(corners[0], vocab.Accent), trA = Eq(corners[1], vocab.Accent),
                 brA = Eq(corners[2], vocab.Accent), blA = Eq(corners[3], vocab.Accent);
            return (tlA && trA) || (trA && brA) || (brA && blA) || (blA && tlA);
        }

        return false;
    }

    /// <summary>
    /// Mirrors LayoutReliefPainter's per-corner perturb-and-verify mechanism: an ungrouped, doorless
    /// tile whose corners all use this vocabulary's Open/Accent/Blend terrains, whose normalized
    /// corner-height deltas are all 0 or 1 (the painter only ever toggles a corner between the room
    /// grade and one story up), whose non-blank edges (if any) are ALL this vocabulary's ramp-lane
    /// crosser (lanes are batch-written/batch-verified -- see LayoutReliefPainter.TrySpliceReliefLane
    /// -- so multi-ramp-edge cells need no one-edge intermediate), and -- the honesty core -- whose
    /// corner (terrain, height) field is REACHABLE from a flat painted base through single-corner
    /// mutations where EVERY intermediate cell state has a real height-aware candidate in this
    /// tileset's inventory (a breadth-first search over the at-most-256 per-cell states, probing the
    /// same TileResolver.HasHeightAwareCandidate the production painter's CellResolves verification
    /// uses). The base state is the tile's own labels with Blend corners replaced by Open, all flat --
    /// exactly what the accent/pool painters leave behind before relief runs; mutations are the
    /// painter's own two corner proposals (height 0&lt;-&gt;1 toggle, Open&lt;-&gt;Blend label flip).
    /// A shape with no resolving mutation order stays unreachable and must remain exempt (e.g. tdm01
    /// TILE1452/1453, whose diagonal CityWater/Floor field has no resolving intermediate chain).
    /// </summary>
    private static bool IsTerrainReliefReachable(TileRecord tile, TilesetVocabulary vocab, TileResolver.HeightAwareProbeCache cache)
    {
        if (tile.GroupIndex != -1) return false;
        if (tile.Doors.Count != 0) return false;
        if (string.IsNullOrEmpty(vocab.Open)) return false;

        bool InPalette(string c) =>
            Eq(c, vocab.Open) ||
            (!string.IsNullOrEmpty(vocab.Accent) && Eq(c, vocab.Accent)) ||
            (!string.IsNullOrEmpty(vocab.Blend) && Eq(c, vocab.Blend));

        if (!tile.Corners.All(InPalette)) return false;

        var min = tile.CornerHeights.Min();
        var target = tile.CornerHeights.Select(h => h - min).ToArray();
        if (target.Any(h => h != 0 && h != 1)) return false;

        var usesBlend = !string.IsNullOrEmpty(vocab.Blend) && tile.Corners.Any(c => Eq(c, vocab.Blend));
        if (target.All(h => h == 0) && !usesBlend) return false; // flat, no blend -- CornerEdgeResolver's bucket

        foreach (var edge in tile.Edges)
        {
            if (string.IsNullOrEmpty(edge)) continue;
            if (!Eq(edge, vocab.Ramp)) return false;
        }

        return IsReliefFieldReachable(tile.Corners, target, vocab, cache);
    }

    /// <summary>Breadth-first search over per-cell (labels, heights) states -- see
    /// IsTerrainReliefReachable's doc comment. Shared with the ReliefPiece group classifier, whose
    /// stamping site must be producible by the same painter.</summary>
    private static bool IsReliefFieldReachable(
        IReadOnlyList<string> targetCorners, int[] targetDeltas, TilesetVocabulary vocab, TileResolver.HeightAwareProbeCache cache)
    {
        var baseLabels = new string[4];
        for (var i = 0; i < 4; i++)
        {
            baseLabels[i] = !string.IsNullOrEmpty(vocab.Blend) && Eq(targetCorners[i], vocab.Blend)
                ? vocab.Open
                : targetCorners[i];
        }

        bool Resolves(string[] labels, int[] heights)
        {
            var m = Math.Min(Math.Min(heights[0], heights[1]), Math.Min(heights[2], heights[3]));
            return TileResolver.HasHeightAwareCandidate(
                cache, labels[0], labels[1], labels[2], labels[3], "", "", "", "",
                heights[0] - m, heights[1] - m, heights[2] - m, heights[3] - m);
        }

        string KeyOf(string[] labels, int[] heights) =>
            string.Join("|", labels.Select(l => l.ToUpperInvariant())) + "‖" + string.Join("|", heights);

        var startHeights = new[] { 0, 0, 0, 0 };
        if (!Resolves(baseLabels, startHeights)) return false;

        var goalKey = KeyOf(targetCorners.ToArray(), targetDeltas);
        var startKey = KeyOf(baseLabels, startHeights);
        if (startKey == goalKey) return true;

        var seen = new HashSet<string> { startKey };
        var frontier = new Queue<(string[] Labels, int[] Heights)>();
        frontier.Enqueue((baseLabels, startHeights));
        var found = false;

        void TryVisit(string[] nl, int[] nh)
        {
            if (found) return;
            var key = KeyOf(nl, nh);
            if (!seen.Add(key)) return;
            if (!Resolves(nl, nh)) return;
            if (key == goalKey) { found = true; return; }
            frontier.Enqueue((nl, nh));
        }

        while (frontier.Count > 0 && !found)
        {
            var (labels, heights) = frontier.Dequeue();

            for (var i = 0; i < 4; i++)
            {
                // height toggle
                var toggled = (int[])heights.Clone();
                toggled[i] = heights[i] == 0 ? 1 : 0;
                TryVisit(labels, toggled);

                // blend flip
                if (!string.IsNullOrEmpty(vocab.Blend))
                {
                    string flippedTo = null;
                    if (Eq(labels[i], vocab.Open)) flippedTo = vocab.Blend;
                    else if (Eq(labels[i], vocab.Blend)) flippedTo = vocab.Open;
                    if (flippedTo != null)
                    {
                        var flipped = (string[])labels.Clone();
                        flipped[i] = flippedTo;
                        TryVisit(flipped, heights);
                    }
                }
            }
        }

        return found;
    }

    // ---------------- group mechanisms ----------------

    private enum GroupMechanism
    {
        None,
        FeatureTile,
        ExitGroup,
        SetPieceWallRoom,
        SetPieceWallAlcove,
        SetPieceOpenSetPiece,
        SetPieceCorridorInsert,
        SetPieceCorridorStub,
        SetPieceCorridorStubChain,
        SetPieceReliefPiece,
    }

    /// <summary>Mirrors TileResolver.BuildFeatureLookup's structural eligibility check.</summary>
    private static bool IsFeatureTileEligible(TilesetModel model, TileGroupRecord group)
    {
        if (group.Rows != 1 || group.Columns != 1 || group.TileIds.Count != 1) return false;
        var tileId = group.TileIds[0];
        if (tileId < 0 || tileId >= model.Tiles.Count) return false;
        var tile = model.Tiles[tileId];
        if (!IsFlat(tile)) return false;
        if (tile.HasAnyCrosser) return false;
        if (tile.Doors.Count != 0) return false;
        if (!Eq(tile.PathNode, "A")) return false;
        return true;
    }

    /// <summary>Mirrors GroupExitPlanner.BuildCandidateGroups' structural eligibility check.</summary>
    private static bool IsExitGroupEligible(TilesetModel model, TileGroupRecord group)
    {
        if (group.Rows != 1 || group.Columns != 1 || group.TileIds.Count != 1) return false;
        var tileId = group.TileIds[0];
        if (tileId < 0 || tileId >= model.Tiles.Count) return false;
        var tile = model.Tiles[tileId];
        if (!IsFlat(tile)) return false;
        if (tile.Doors.Count == 0) return false;
        if (tile.HasAnyCrosser) return false;
        return true;
    }

    private static readonly (int Dx, int Dy)[] SlotOffsets = { (0, 1), (1, 0), (0, -1), (-1, 0) };

    /// <summary>Mirrors LayoutGroupStamper.IsHole: true when the group's (row, col) local slot is a -1
    /// hole (no real tile there) rather than a real member.</summary>
    private static bool IsHoleAt(TileGroupRecord group, int row, int col)
    {
        return group.TileIds[row * group.Columns + col] < 0;
    }

    /// <summary>Mirrors LayoutGroupStamper.TryClassifyCorridorInsert (Corridor/Alley/Fence/Bridge
    /// opposite-pair 1x1 dead-straight gate, plus the Doorway-pair pass-through-segment branch --
    /// see IsDoorwayPairCorridorInsertEligible).</summary>
    private static bool IsCorridorInsertEligible(TilesetModel model, TileRecord tile, TilesetVocabulary vocab)
    {
        if (!IsFlat(tile)) return false;

        var allSolid = tile.Corners.All(c => Eq(c, vocab.Solid));
        var allOpen = !string.IsNullOrEmpty(vocab.Open) && tile.Corners.All(c => Eq(c, vocab.Open));
        var allSecondary = !string.IsNullOrEmpty(vocab.Secondary) && tile.Corners.All(c => Eq(c, vocab.Secondary));
        var allAccent = !string.IsNullOrEmpty(vocab.Channel) && tile.Corners.All(c => Eq(c, vocab.Channel));
        if (!allSolid && !allOpen && !allSecondary && !allAccent) return false;

        // vocab.TunnelBody defaults to the canonical "Corridor" (see BuildVocabulary) -- an alternate
        // district-scoped body crosser (e.g. tdc01's "GreyCorridor") is credited here the same way
        // LayoutGroupStamper.CorridorInsertCrossersFor tries it for a Custom-mode composition.
        foreach (var crosser in new[] { vocab.TunnelBody, AlleyCrosser, FenceCrosser, BridgeCrosser })
        {
            // Fence gates splice into a LayoutFenceCarver run in EITHER the primary or (when
            // districted) secondary open terrain -- see LayoutFenceCarver.CarveFences' independent
            // per-terrain passes (e.g. vmr01's Floor-cornered InteriorFenceDoor vs Plaza-cornered
            // ExteriorFenceDoor).
            var terrainMatches = crosser switch
            {
                FenceCrosser => allOpen || allSecondary,
                BridgeCrosser => allAccent,
                _ => allSolid
            };
            if (!terrainMatches) continue;

            var hasCrosser = new bool[4];
            var edgesMatch = true;
            for (var slot = 0; slot < 4; slot++)
            {
                var edge = tile.Edges[slot] ?? string.Empty;
                if (edge.Length == 0) continue;
                if (!Eq(edge, crosser)) { edgesMatch = false; break; }
                hasCrosser[slot] = true;
            }
            if (!edgesMatch) continue;

            var isVerticalPair = hasCrosser[EdgeSlot.Top] && hasCrosser[EdgeSlot.Bottom] && !hasCrosser[EdgeSlot.Left] && !hasCrosser[EdgeSlot.Right];
            var isHorizontalPair = hasCrosser[EdgeSlot.Left] && hasCrosser[EdgeSlot.Right] && !hasCrosser[EdgeSlot.Top] && !hasCrosser[EdgeSlot.Bottom];
            if (isVerticalPair || isHorizontalPair) return true;
        }

        if (allSolid && IsDoorwayPairCorridorInsertEligible(model, tile, vocab)) return true;

        return false;
    }

    /// <summary>Mirrors LayoutGroupStamper.TryClassifyCorridorInsert's Doorway-pair branch (e.g. tdt01
    /// "Door_Trans" TILE151): an all-solid tile with an opposite Doorway edge pair, gated on the
    /// tileset carrying a genuine solid-corner Corridor/Doorway adapter tile (mirrors
    /// LayoutGroupStamper.HasCorridorDoorwayAdapter) so a tileset lacking that adapter never counts
    /// this shape as reachable.</summary>
    private static bool IsDoorwayPairCorridorInsertEligible(TilesetModel model, TileRecord tile, TilesetVocabulary vocab)
    {
        var hasDoorway = new bool[4];
        var doorwayOnly = true;
        for (var slot = 0; slot < 4; slot++)
        {
            var edge = tile.Edges[slot] ?? string.Empty;
            if (edge.Length == 0) continue;
            if (!IsDoorway(edge) && !Eq(edge, vocab.TunnelPort)) { doorwayOnly = false; break; }
            hasDoorway[slot] = true;
        }
        if (!doorwayOnly) return false;

        var isVerticalPair = hasDoorway[EdgeSlot.Top] && hasDoorway[EdgeSlot.Bottom] && !hasDoorway[EdgeSlot.Left] && !hasDoorway[EdgeSlot.Right];
        var isHorizontalPair = hasDoorway[EdgeSlot.Left] && hasDoorway[EdgeSlot.Right] && !hasDoorway[EdgeSlot.Top] && !hasDoorway[EdgeSlot.Bottom];
        if (!isVerticalPair && !isHorizontalPair) return false;

        return HasCorridorDoorwayAdapter(model, vocab);
    }

    /// <summary>True when the tileset carries at least one flat, all-solid-corner tile with exactly
    /// one body-crosser edge and its opposite edge a port-crosser edge (the other two blank) -- mirrors
    /// LayoutGroupStamper.HasCorridorDoorwayAdapter. vocab.TunnelBody/TunnelPort default to the
    /// canonical "Corridor"/"Doorway" pair (see BuildVocabulary), so this is unchanged for every
    /// profile that doesn't declare an alternate Tunnel crosser family.</summary>
    private static bool HasCorridorDoorwayAdapter(TilesetModel model, TilesetVocabulary vocab)
    {
        foreach (var candidate in model.Tiles)
        {
            if (!IsFlat(candidate)) continue;
            if (!candidate.Corners.All(c => Eq(c, vocab.Solid))) continue;

            var corridorSlot = -1;
            var doorwaySlot = -1;
            var onlyThoseTwo = true;
            for (var slot = 0; slot < 4; slot++)
            {
                var edge = candidate.Edges[slot] ?? string.Empty;
                if (edge.Length == 0) continue;
                if (Eq(edge, vocab.TunnelBody)) corridorSlot = slot;
                else if (Eq(edge, vocab.TunnelPort) || IsDoorway(edge)) doorwaySlot = slot;
                else { onlyThoseTwo = false; break; }
            }

            if (!onlyThoseTwo || corridorSlot == -1 || doorwaySlot == -1) continue;
            if (Math.Abs(corridorSlot - doorwaySlot) == 2) return true;
        }

        return false;
    }

    /// <summary>Mirrors LayoutGroupStamper.TryClassifyCorridorStub (Corridor/Alley single-edge dead
    /// end, all-solid corners).</summary>
    private static bool IsCorridorStubEligible(TileRecord tile, TilesetVocabulary vocab)
    {
        if (!IsFlat(tile)) return false;
        if (!tile.Corners.All(c => Eq(c, vocab.Solid))) return false;

        foreach (var crosser in new[] { vocab.TunnelBody, AlleyCrosser })
        {
            var crosserCount = 0;
            var edgesMatch = true;
            for (var slot = 0; slot < 4; slot++)
            {
                var edge = tile.Edges[slot] ?? string.Empty;
                if (edge.Length == 0) continue;
                if (!Eq(edge, crosser)) { edgesMatch = false; break; }
                crosserCount++;
            }
            if (edgesMatch && crosserCount == 1) return true;
        }

        return false;
    }

    /// <summary>Mirrors LayoutGroupStamper.TryClassify's WallRoom/WallAlcove/OpenSetPiece branch (the
    /// multi-tile — and, for OpenSetPiece, also 1x1 — path taken once CorridorInsert/CorridorStub have
    /// been ruled out). Tolerates a -1 hole slot (e.g. tdt01/tds01 "Platform03_2x2") as ordinary plan
    /// space, not a real member -- every classification decision below is derived only from `members`,
    /// mirroring LayoutGroupStamper.TryClassify's hole handling. A door slot is tolerated on a WallRoom
    /// candidate too (production's own hasAnyDoor relaxation -- see LayoutGroupStamper.TryClassify's
    /// own doc comment) -- WriteMember/StampWallRoom never write door data, so an unpopulated slot on a
    /// stamped WallRoom member renders exactly like any other unpopulated Doorway-keyed door-slot tile
    /// already does today.</summary>
    private static GroupMechanism ClassifyMultiTileSetPiece(TilesetModel model, TileGroupRecord group, TilesetVocabulary vocab)
    {
        if (group.Rows <= 0 || group.Columns <= 0) return GroupMechanism.None;
        if (group.TileIds.Count != group.Rows * group.Columns) return GroupMechanism.None;

        // Mirrors LayoutGroupStamper.CorridorStubCrossersFor: the same body-crosser vocabulary a
        // multi-tile CorridorStubChain (e.g. Barrows/tbw01's CorridorDown_1x2) is allowed to carry on a
        // perimeter edge, alongside the canonical Doorway port WallRoom/WallAlcove/OpenSetPiece use.
        bool IsAllowedMemberEdge(string edge) =>
            string.IsNullOrEmpty(edge) || Eq(edge, DoorwayCrosser) || Eq(edge, vocab.TunnelBody) || Eq(edge, AlleyCrosser);

        var members = new List<TileRecord>();
        var positioned = new List<(int Row, int Col, TileRecord Tile)>();
        for (var row = 0; row < group.Rows; row++)
        {
            for (var col = 0; col < group.Columns; col++)
            {
                var tileId = group.TileIds[row * group.Columns + col];
                if (tileId < 0) continue; // hole
                if (tileId >= model.Tiles.Count) return GroupMechanism.None; // out of range -- bad data
                var tile = model.Tiles[tileId];
                if (!IsFlat(tile)) return GroupMechanism.None;
                foreach (var edge in tile.Edges)
                {
                    if (!IsAllowedMemberEdge(edge)) return GroupMechanism.None;
                }
                members.Add(tile);
                positioned.Add((row, col, tile));
            }
        }
        if (members.Count == 0) return GroupMechanism.None; // an all-hole "group" is degenerate

        // Mirrors LayoutGroupStamper.TryClassify's perimeterDoorways/perimeterBodyCrossers computation:
        // a Doorway or body-crosser edge whose neighbor cell falls outside the group's own footprint
        // (out of bounds OR a hole) is a real perimeter opening; a Doorway edge shared between two real
        // members of the SAME group (e.g. tic01 "Turret Interior - Lit/Dark (2x1)", where each member's
        // lone Doorway edge faces the other member) is interior only and does not count. A body-crosser
        // edge shared between two real members (a shape no verified data uses) similarly disqualifies
        // the whole group via hasInteriorBodyCrosser, mirroring TryClassify's own rejection.
        var hasAnyPerimeterDoorway = false;
        var hasAnyPerimeterBodyCrosser = false;
        var hasInteriorBodyCrosser = false;
        foreach (var (row, col, tile) in positioned)
        {
            for (var slot = 0; slot < 4; slot++)
            {
                var edge = tile.GetEdgeAt(0, slot);
                var isDoorway = Eq(edge, DoorwayCrosser);
                var isBodyCrosser = !isDoorway && (Eq(edge, vocab.TunnelBody) || Eq(edge, AlleyCrosser));
                if (!isDoorway && !isBodyCrosser) continue;

                var (dx, dy) = SlotOffsets[slot];
                var neighborRow = row + dy;
                var neighborCol = col + dx;
                var outOfBounds = neighborRow < 0 || neighborRow >= group.Rows ||
                                   neighborCol < 0 || neighborCol >= group.Columns;
                var isPerimeter = outOfBounds || IsHoleAt(group, neighborRow, neighborCol);

                if (isDoorway && isPerimeter) hasAnyPerimeterDoorway = true;
                else if (isBodyCrosser && isPerimeter) hasAnyPerimeterBodyCrosser = true;
                else if (isBodyCrosser) hasInteriorBodyCrosser = true;
            }
        }

        var hasAnyDoorway = members.Any(m => m.Edges.Any(e => Eq(e, DoorwayCrosser)));
        var hasAnyBodyCrosser = members.Any(m => m.Edges.Any(e => !Eq(e, DoorwayCrosser) && (Eq(e, vocab.TunnelBody) || Eq(e, AlleyCrosser))));
        var allCornersSolid = members.All(m => m.Corners.All(c => Eq(c, vocab.Solid)));
        var hasAnyDoor = members.Any(m => m.Doors.Count != 0);

        // CorridorStubChain: mirrors LayoutGroupStamper.TryClassify's own priority (checked ahead of
        // WallRoom/WallAlcove/OpenSetPiece -- mutually exclusive in every verified shape).
        if (hasAnyBodyCrosser)
        {
            if (hasAnyDoorway || !allCornersSolid || hasInteriorBodyCrosser || !hasAnyPerimeterBodyCrosser)
                return GroupMechanism.None;
            return GroupMechanism.SetPieceCorridorStubChain;
        }

        if (hasAnyDoorway)
        {
            if (!allCornersSolid || !hasAnyPerimeterDoorway) return GroupMechanism.None;
            return GroupMechanism.SetPieceWallRoom;
        }

        if (allCornersSolid && hasAnyDoor)
            return GroupMechanism.SetPieceWallAlcove;

        var matchesPrimary = members.All(m => m.Corners.All(c => Eq(c, vocab.Solid) || Eq(c, vocab.Open))) &&
                              members.Any(m => m.Corners.Any(c => Eq(c, vocab.Open)));
        var matchesSecondary = !string.IsNullOrEmpty(vocab.Secondary) &&
                                members.All(m => m.Corners.All(c => Eq(c, vocab.Solid) || Eq(c, vocab.Secondary))) &&
                                members.Any(m => m.Corners.Any(c => Eq(c, vocab.Secondary)));

        if (matchesPrimary || matchesSecondary) return GroupMechanism.SetPieceOpenSetPiece;

        return GroupMechanism.None;
    }

    /// <summary>Mirrors LayoutGroupStamper.TryClassifyReliefPiece + TryPlaceReliefPiece's site
    /// requirement: a RAISED (non-flat, non-uniform-delta) 1x1 group piece whose corner (terrain,
    /// height) field the relief painter can actually paint (the same BFS reachability check
    /// IsTerrainReliefReachable uses -- production stamps a piece only onto a cell whose PAINTED
    /// field exactly matches, so a field the painter can never produce means the piece can never
    /// place). Edges may be blank or ALL equal this vocabulary's own Ramp crosser (mirrors
    /// IsTerrainReliefReachable's identical rule -- e.g. ttf01's "Ramp - City Wall"); a door slot is
    /// tolerated exactly like WallAlcove/OpenSetPiece/WallRoom (never spawns a door object) -- this
    /// closes ttf01's raised gate-tower/breach/moss-wall family and the "Cave"/"SmallCave"/"Cave
    /// Entrance" shape shared by ttf01/ttd01/tdm01. See LayoutGroupStamper.TryClassifyReliefPiece's
    /// own doc comment for the full reasoning.</summary>
    private static bool IsReliefPieceEligible(TileRecord tile, TilesetVocabulary vocab, TileResolver.HeightAwareProbeCache cache)
    {
        if (IsFlat(tile)) return false;
        foreach (var edge in tile.Edges)
        {
            if (string.IsNullOrEmpty(edge)) continue;
            if (!Eq(edge, vocab.Ramp)) return false;
        }
        if (string.IsNullOrEmpty(vocab.Open)) return false;

        var min = tile.CornerHeights.Min();
        var target = tile.CornerHeights.Select(h => h - min).ToArray();
        if (target.All(h => h == 0)) return false; // uniform raised -- normalizes flat, not a step piece
        if (target.Any(h => h != 0 && h != 1)) return false;

        bool InPalette(string c) =>
            Eq(c, vocab.Open) ||
            (!string.IsNullOrEmpty(vocab.Accent) && Eq(c, vocab.Accent)) ||
            (!string.IsNullOrEmpty(vocab.Blend) && Eq(c, vocab.Blend));
        if (!tile.Corners.All(InPalette)) return false;

        return IsReliefFieldReachable(tile.Corners, target, vocab, cache);
    }

    private static GroupMechanism ClassifySetPiece(TilesetModel model, TileGroupRecord group, TilesetVocabulary vocab, TileResolver.HeightAwareProbeCache cache)
    {
        if (group.Rows == 1 && group.Columns == 1 && group.TileIds.Count == 1)
        {
            var soloTileId = group.TileIds[0];
            if (soloTileId >= 0 && soloTileId < model.Tiles.Count)
            {
                var soloTile = model.Tiles[soloTileId];
                if (IsCorridorInsertEligible(model, soloTile, vocab)) return GroupMechanism.SetPieceCorridorInsert;
                if (IsCorridorStubEligible(soloTile, vocab)) return GroupMechanism.SetPieceCorridorStub;
                if (IsReliefPieceEligible(soloTile, vocab, cache)) return GroupMechanism.SetPieceReliefPiece;
            }
        }

        return ClassifyMultiTileSetPiece(model, group, vocab);
    }

    // ---------------- the census itself ----------------

    private sealed class Exemption
    {
        public string Tileset;
        public string TileOrGroup;
        public string Reason;
    }

    /// <summary>
    /// The EXACT, named exemption set (asserted as a full set, not a subset): every tile in every
    /// tileset that this census could not structurally place into a mechanism, with a one-line reason.
    ///
    /// This is EMPTY: true 100% tile coverage across all four generation tilesets. The three
    /// mechanisms that used to leave 10 tiles exempted were closed by:
    /// (A) TileDoorPlanner.BuildTerminatorCandidates tolerating a trivial 1x1 [GROUP]-wrapped
    ///     terminator tile (tds01 "Door_Trans" TILE174, vmr01 "Door_Trans"/"Door_Trans_Exterior"
    ///     TILE152/60) -- see IsSingleCellGroup and IsDoorTransitionReachable's grouped-tile branch.
    /// (B) LayoutGroupStamper.TryClassifyCorridorInsert/TryPlaceCorridorInsert adding a Doorway-pair
    ///     branch (tdt01 "Door_Trans" TILE151, an opposite-Doorway-edge pass-through segment) that
    ///     splices into a straight Corridor chain by rewriting the two flanking plan edges to Doorway
    ///     -- see IsDoorwayPairCorridorInsertEligible/HasCorridorDoorwayAdapter.
    /// (C) LayoutGroupStamper.TryClassify/site-validation tolerating a -1 hole slot in a group's
    ///     rectangular footprint (tdt01/tds01 "Platform03_2x2") as ordinary plan space rather than
    ///     rejecting the whole group -- see ClassifyMultiTileSetPiece's hole handling.
    /// </summary>
    private static readonly HashSet<(string Tileset, string Label)> ExpectedExemptions = new();

    public static IEnumerable<string> TilesetKeys => new[] { "tdt01", "tds01", "zsf01", "vmr01" };

    [TestCaseSource(nameof(TilesetKeys))]
    public void EveryTileIsReachableOrExplicitlyExempted(string tilesetResref)
    {
        var model = LoadTileset(tilesetResref);
        var profileKey = tilesetResref switch
        {
            "tdt01" => StandardTilesetProfiles.Cavern,
            "tds01" => StandardTilesetProfiles.Sewers,
            "zsf01" => StandardTilesetProfiles.Facility,
            "vmr01" => StandardTilesetProfiles.AncientRuin,
            _ => throw new ArgumentOutOfRangeException(nameof(tilesetResref))
        };
        var profile = new StandardTilesetProfiles().BuildTilesetProfiles()[profileKey];
        var vocab = BuildVocabulary(model, profile);
        var probeCache = TileResolver.BuildHeightAwareProbeCache(model);

        var coveredTileIds = new HashSet<int>();
        var mechanismCounts = new Dictionary<string, int>();
        var exemptions = new List<Exemption>();

        void Cover(int tileId, string mechanism)
        {
            coveredTileIds.Add(tileId);
            mechanismCounts[mechanism] = mechanismCounts.GetValueOrDefault(mechanism) + 1;
        }

        // Pass 1: every GROUP, classified once, covers all its member tiles together.
        foreach (var group in model.Groups)
        {
            var memberIds = group.TileIds.Where(id => id >= 0 && id < model.Tiles.Count).ToList();
            if (memberIds.Count == 0) continue;

            var mechanism = GroupMechanism.None;
            if (IsFeatureTileEligible(model, group)) mechanism = GroupMechanism.FeatureTile;
            else if (IsExitGroupEligible(model, group)) mechanism = GroupMechanism.ExitGroup;
            else mechanism = ClassifySetPiece(model, group, vocab, probeCache);

            if (mechanism != GroupMechanism.None)
            {
                foreach (var id in memberIds) Cover(id, mechanism.ToString());
                continue;
            }

            if (ExpectedExemptions.Contains((tilesetResref, "GROUP:" + group.Name)))
            {
                foreach (var id in memberIds)
                    exemptions.Add(new Exemption { Tileset = tilesetResref, TileOrGroup = $"TILE{id} (group '{group.Name}')", Reason = "see ExpectedExemptions doc comment" });
            }
            // Groups that neither classify nor carry a pre-declared exemption fall through: their
            // member tiles are re-evaluated as plain tiles below. IsCornerEdgeResolverReachable always
            // returns false for a grouped tile, but IsDoorTransitionReachable now tolerates a trivial
            // 1x1 all-solid group member (Fix A) -- an uncovered, unexempted member still surfaces as
            // a genuine failure in the final assertion instead of being silently swallowed here.
        }

        // Pass 2: every remaining (ungrouped, or grouped-but-unclassified-and-unexempted) tile.
        for (var tileId = 0; tileId < model.Tiles.Count; tileId++)
        {
            if (coveredTileIds.Contains(tileId)) continue;
            if (exemptions.Any(e => e.TileOrGroup.StartsWith($"TILE{tileId} "))) continue;

            var tile = model.Tiles[tileId];

            if (IsCornerEdgeResolverReachable(model, tile, vocab)) { Cover(tileId, "CornerEdgeResolver"); continue; }
            if (IsDoorTransitionReachable(model, tile)) { Cover(tileId, "DoorTransition"); continue; }

            exemptions.Add(new Exemption { Tileset = tilesetResref, TileOrGroup = $"TILE{tileId}", Reason = "UNCLASSIFIED" });
        }

        // ---- report ----
        var coveragePercent = model.Tiles.Count == 0 ? 100.0 : 100.0 * coveredTileIds.Count / model.Tiles.Count;
        TestContext.WriteLine($"=== {tilesetResref} ({profileKey}) coverage: {coveredTileIds.Count}/{model.Tiles.Count} ({coveragePercent:0.0}%) ===");
        foreach (var kv in mechanismCounts.OrderByDescending(k => k.Value))
            TestContext.WriteLine($"  {kv.Key,-24} {kv.Value,4} tiles");
        TestContext.WriteLine($"  {"Exempted",-24} {exemptions.Count,4} tiles");
        foreach (var e in exemptions.OrderBy(e => e.TileOrGroup, StringComparer.Ordinal))
            TestContext.WriteLine($"    {e.TileOrGroup}: {e.Reason}");

        // ---- assertions ----
        // The exemption set is EMPTY across all four tilesets (true 100% coverage) -- asserted
        // directly here as well as via the exact set-equality check below, so a future regression
        // (a new tile, a shape a mechanism no longer covers) fails loudly in either place.
        ExpectedExemptions.Should().BeEmpty("100% tile coverage means zero exemptions across all four tilesets");

        var unclassified = exemptions.Where(e => e.Reason == "UNCLASSIFIED").Select(e => e.TileOrGroup).ToList();
        unclassified.Should().BeEmpty($"every {tilesetResref} tile must be either reachable or carry a pre-declared, reasoned exemption");

        var actualExemptionLabels = exemptions.Select(e => e.TileOrGroup).ToHashSet();
        var expectedLabelsForThisTileset = model.Groups
            .Where(g => ExpectedExemptions.Contains((tilesetResref, "GROUP:" + g.Name)))
            .SelectMany(g => g.TileIds.Where(id => id >= 0))
            .Select(id => model.Tiles.First(t => t.TileId == id))
            .Select(t => $"TILE{t.TileId} (group '{model.Groups[t.GroupIndex].Name}')")
            .ToHashSet();

        actualExemptionLabels.Should().BeEquivalentTo(expectedLabelsForThisTileset,
            $"the {tilesetResref} exemption set must be EXACT -- any drift must be visible here, not silently absorbed");
        actualExemptionLabels.Should().BeEmpty($"the {tilesetResref} exemption set must be empty -- true 100% coverage");

        (coveredTileIds.Count + exemptions.Count).Should().Be(model.Tiles.Count);
        coveredTileIds.Count.Should().Be(model.Tiles.Count, $"{tilesetResref} must reach 100% tile coverage ({model.Tiles.Count}/{model.Tiles.Count})");
    }

    // ---------------- pilot wave (base-game, non-hak tilesets) ----------------

    private const string HeightExemptionReason = "requires height support";
    private const string AlternateVocabExemptionReason =
        "alternate-palette/decorative vocabulary (terrain or crosser name outside this pilot's wired vocabulary); out of scope for this pilot";
    /// <summary>
    /// Auto-tagged, evidence-backed exemption for a tile a tileset profile has declared in
    /// DungeonTilesetProfile.ExcludedTiles (confirmed placeholder/stub art -- e.g. twc03's 15 "xyz"-
    /// family tiles, see BaseGameTilesetProfiles.FortInteriorLegacy's own doc comment). Applied to the
    /// excluded tile itself AND every sibling member of the same group (a furnished room missing its
    /// own floor/entrance tile is not a usable set piece regardless of whether its OTHER member tiles
    /// are individually fine) -- mirroring HeightExemptionReason/AlternateVocabExemptionReason's own
    /// "tag the whole group" shape.
    /// </summary>
    private const string PlaceholderArtExemptionReason = "confirmed placeholder/stub art (see DungeonTilesetProfile.ExcludedTiles)";

    /// <summary>
    /// Per-tileset terrain names that exist ONLY as an alternate decorative palette or an extra
    /// accent-terrain variant this pilot wave did not wire into BaseGameTilesetProfiles -- e.g.
    /// tdc01's "Grey"/"Dwarven" palettes (GreyFloor/GreyPit/DwarvenFloor/DwarvenPit vs. the wired
    /// "[Tan]" palette's plain Wall/Floor/Pit), or tde01's Water/Sewer/Ice accent-channel variants
    /// beyond the single wired AccentTerrain ("Lava"). A group/tile whose corners use ONLY these
    /// terrains (beyond Solid/Open/Secondary/Accent) is auto-exempted -- see
    /// AlternateVocabExemptionReason -- rather than hand-enumerated tile-by-tile.
    /// </summary>
    private static readonly Dictionary<string, HashSet<string>> PilotAlternateVocabTerrains = new(StringComparer.OrdinalIgnoreCase)
    {
        ["tdc01"] = new(StringComparer.OrdinalIgnoreCase) { "GreyFloor", "GreyPit", "DwarvenFloor", "DwarvenPit" },
        // "Water"/"Sewer"/"Ice"/"Pit" are no longer here: BaseGameTilesetProfiles.
        // DungeonWater/DungeonSewer/DungeonIce/DungeonPit each declare AccentTerrain(<accent>), closing
        // their own "Door - Bridge 1, <Accent>" CorridorInsert(Bridge) gate the same way every other
        // AccentTerrain-only PaletteVariant here does (the ~100 ordinary tiles referencing each accent
        // were already reachable via CornerEdgeResolver regardless of any profile's AccentTerrain
        // declaration -- IsCornerEdgeResolverReachable resolves a tile against its own raw corners,
        // independent of which terrain a profile happens to have designated as "the" accent).
        ["tde01"] = new(StringComparer.OrdinalIgnoreCase),
        ["tin01"] = new(StringComparer.OrdinalIgnoreCase),
        ["tbw01"] = new(StringComparer.OrdinalIgnoreCase),
        // tdm01's hak copy carries three entire alternate-district palettes ([Desert]/[Organic]/[City])
        // beyond the wired "[Cave]" family/accent -- see BaseGameTilesetProfiles.MinesAndCaverns.
        ["tdm01"] = new(StringComparer.OrdinalIgnoreCase)
        {
            "Desert", "DesertWater", "DesertPit", "DesertLava",
            "Organic", "OrganicWater", "OrganicPit", "OrganicSlime",
            "CityWater", "CityCastle", "PADDING",
            "GentleSlope", "GentleDesert", "GentleOrganic",
        },
        ["tdr01"] = new(StringComparer.OrdinalIgnoreCase) { "Plaza" },
        // "Storage"/"Rich"/"Library"/"Jail" are no longer here: BaseGameTilesetProfiles.
        // CastleInteriorStorage/CastleInteriorRich/CastleInteriorLibrary/CastleInteriorJail each declare
        // PrimaryOpenTerrain(<district>), closing their full simple-tile coverage via CornerEdgeResolver
        // the same way every other PaletteVariant's own open-terrain declaration does.
        ["tic01"] = new(StringComparer.OrdinalIgnoreCase) { "Tower", "PADDING" },
        ["tni02"] = new(StringComparer.OrdinalIgnoreCase) { "storage", "rich", "library", "jail", "round" },
        ["tid01"] = new(StringComparer.OrdinalIgnoreCase) { "floor", "2x2", "PADDING" },
        ["tii01"] = new(StringComparer.OrdinalIgnoreCase),
        ["tni01"] = new(StringComparer.OrdinalIgnoreCase) { "livingroom", "kitchen", "shop" },
        ["tsw01"] = new(StringComparer.OrdinalIgnoreCase),
        ["twc03"] = new(StringComparer.OrdinalIgnoreCase),
        // ttd01's hak copy (sw_t_tatooine) adds two village-hut ground palettes, "Svirfneblin" and
        // "Poor": each blends ONLY with the walkable Desert terrain (best coverage 14/16, verified
        // directly -- two missing combos -- and no Cliff blending at all), so no profile can wire
        // either as an open terrain against the composed Cliff solid. This also auto-exempts the
        // eight ungrouped Svirfneblin/Poor door-slot tiles (TILE203/205/207/208/211/212/221/223,
        // bare door slots with no crosser -- TileDoorPlanner's single-Doorway-edge rule can never
        // place them regardless).
        ["ttd01"] = new(StringComparer.OrdinalIgnoreCase) { "Svirfneblin", "Poor" },
        // ttf01's hak copy (sw_t_forest) carries two full unwired district palettes (GoodCastle/
        // EvilCastle, each blending only with the walkable Forest terrain -- out of this wave's scope,
        // the tni01 livingroom/kitchen/shop precedent), one under-covered palette (Marsh, 14/16
        // against Forest). Platform/HighForest are MOSTLY closed by BaseGameTilesetProfiles.
        // ForestPlatform's SolidTerrainOverride("Pit") + PrimaryOpenTerrain("Platform") variant
        // (16/16 against Pit, verified directly), leaving only the genuinely three-terrain "Platform -
        // Cliff Door"/"Platform - Cliff Section" groups (Platform+Cliff+Pit on one group, outside any
        // two-terrain classifier) still tagged via this dictionary. RuralTrees/RuralWater are now
        // MOSTLY closed too by BaseGameTilesetProfiles.ForestRural's AccentTerrain/ReliefBlendTerrain
        // variant (PoolBank/TerrainRelief, verified directly and via a real-generation placement
        // proof) -- but TILE849 (uniform RuralWater, door-bearing, Road-crossered) and TILE1114
        // (Forest/RuralTrees mixed, door-bearing, Road-crossered) still need the TERRAIN entries here:
        // "Road" is deliberately NOT in PilotAlternateVocabCrossers["ttf01"] below (it's the base
        // wired crossroads-gate family, not an alternate one), and a door-bearing tile fails
        // CornerEdgeResolver's Doorway/Bridge/extra-only admission gate regardless of vocab -- verified
        // directly that removing RuralTrees/RuralWater from this set turns exactly these two tiles
        // UNCLASSIFIED. See BaseGameTilesetProfiles.Forest's own doc comment.
        ["ttf01"] = new(StringComparer.OrdinalIgnoreCase)
        {
            "GoodCastle", "EvilCastle", "Marsh", "Platform", "HighForest", "RuralTrees", "RuralWater",
        },
        ["ttf02"] = new(StringComparer.OrdinalIgnoreCase),
        // jac01 (Jacoby's Jungle): Platform/HighForest registered as a PaletteVariant (JunglePlatform)
        // + the base Jungle profile's own "always CornerEdgeResolver-reachable ungrouped tile" note
        // above, not this alternate-vocab bucket -- no terrain needs auto-tagging here.
        ["jac01"] = new(StringComparer.OrdinalIgnoreCase),
        ["fcx01"] = new(StringComparer.OrdinalIgnoreCase),
        // tjsb0 (D20 Secret Base): a single Wall/Floor/lava split, no alternate district palette.
        ["tjsb0"] = new(StringComparer.OrdinalIgnoreCase),
        // tbx78 (D20 Modern Facility): a single Wall/facility split, no alternate district palette.
        ["tbx78"] = new(StringComparer.OrdinalIgnoreCase),
        // tqq01 (Complex laps storage): BaseGameTilesetProfiles.LabStorage only wires the "Inn" district
        // (the .set's own declared Floor terrain) plus generic groups -- the other three parallel
        // room-type districts (Livingroom/Kitchen/Shop) are out of this onboarding pass's scope (no
        // PaletteVariant profile registered for them), the identical descope BaseGameTilesetProfiles.
        // CityInterior2 (tni01) already applies to its own "livingroom"/"kitchen"/"shop" terrains.
        ["tqq01"] = new(StringComparer.OrdinalIgnoreCase) { "Livingroom", "Kitchen", "Shop" },
        // udp2 (D20 Office Interiors UDP): BaseGameTilesetProfiles.OfficeInteriors only wires the
        // "Office_Vinyl" district (the .set's own declared Floor terrain) plus tileset-generic groups --
        // the other six parallel room-type districts are out of this onboarding pass's scope, same
        // descope reasoning as tqq01 above.
        ["udp2"] = new(StringComparer.OrdinalIgnoreCase)
        {
            "Service", "Tiled", "Office_Wood", "Office_Alum", "Foyer_L", "Foyer_U",
        },
    };

    /// <summary>
    /// Per-tileset crosser names outside the shared layout carvers' canonical Doorway/Corridor/
    /// Alley/Fence/Bridge vocabulary (e.g. tdc01's GreyCorridor/DwarvenDoorway/DwarvenCorridor/
    /// ChultDoorway/ChultCorridor district-junction crossers, tde01's MazeMosaic). A group/tile whose
    /// non-blank edges are ALL among these is auto-exempted the same way as PilotAlternateVocabTerrains.
    /// </summary>
    private static readonly Dictionary<string, HashSet<string>> PilotAlternateVocabCrossers = new(StringComparer.OrdinalIgnoreCase)
    {
        // "GreyCorridor" is no longer here: BaseGameTilesetProfiles.CryptGrey declares it as
        // TunnelBodyCrosser (paired with the canonical "Doorway" port), a verified body-only-renamed
        // family (see that profile's own doc comment) -- the census now credits it the same way
        // LayoutGroupStamper/LayoutTunnelCarver do for a Custom-mode composition. "DwarvenCorridor"/
        // "DwarvenDoorway"/"ChultDoorway"/"ChultCorridor" remain unwired: Dwarven renames BOTH halves
        // of the pair with no verified boundary shape (TunnelVocabularyCheck.SupportsTunnels returns
        // false even under the generalized Custom probe), and Chult has no boundary/open-terrain tile
        // at all (every Chult-crossered tile is all-Wall-cornered) -- see BaseGameTilesetProfiles.Crypt.
        ["tdc01"] = new(StringComparer.OrdinalIgnoreCase) { "DwarvenDoorway", "DwarvenCorridor", "ChultDoorway", "ChultCorridor" },
        ["tde01"] = new(StringComparer.OrdinalIgnoreCase) { "MazeMosaic" },
        ["tin01"] = new(StringComparer.OrdinalIgnoreCase),
        // "door_corridor" is no longer here: BaseGameTilesetProfiles.Barrows declares it as
        // DoorSlotCrossers (paired with TunnelCrossers("corridor", "door_corridor")), a verified
        // both-halves-renamed Tunnel body/port family (unlike Crypt Grey's body-only rename) -- the
        // census now credits it the same way TileResolver's generalized crosser+door-slot admission
        // gate does for a real composition. "door_barrow" is ALSO now declared as a DoorSlotCrosser,
        // which closes its ungrouped boundary tile (TILE39, the same shape as TILE13 above) via
        // CornerEdgeResolver -- but SideChamber1 (a 1x1 group, TILE60) stays unwired and this entry
        // stays here to auto-exempt it: MacroLayoutParameters only carries one Tunnel port crosser slot
        // per composition (already claimed by "door_corridor"), and no carver ever writes a "door_barrow"
        // edge for LayoutGroupStamper's CorridorStub site search to attach to -- see
        // BaseGameTilesetProfiles.Barrows' own doc comment.
        ["tbw01"] = new(StringComparer.OrdinalIgnoreCase) { "door_barrow" },
        // "DesertCorridor"/"OrganicCorridor" are no longer here: BaseGameTilesetProfiles.
        // MinesAndCavernsDesert/MinesAndCavernsOrganic declare them as TunnelBodyCrosser (paired with
        // the canonical "Doorway" port), verified body-only-renamed families mirroring Crypt Grey's own
        // shape. "Tracks"/"DesertTracks"/"OrganicTracks" are no longer here either: each is a SECOND,
        // independent alternate body family declared by its own dedicated PaletteVariant profile
        // (BaseGameTilesetProfiles.MinesAndCavernsTracks/MinesAndCavernsDesertTracks/
        // MinesAndCavernsOrganicTracks -- a composition carries only one Tunnel body/port slot, already
        // claimed by Corridor/DesertCorridor/OrganicCorridor in the base district profiles, so this
        // second family needed its own profile rather than being added to an existing one). "DesertFence"/
        // "CityFence" are unrelated (Fence-carver vocabulary, not Tunnel body/port) and stay
        // unwired/out of scope.
        ["tdm01"] = new(StringComparer.OrdinalIgnoreCase)
        {
            "DesertFence", "CityFence",
        },
        ["tdr01"] = new(StringComparer.OrdinalIgnoreCase),
        ["tic01"] = new(StringComparer.OrdinalIgnoreCase) { "Window", "MazeMosaic", "MazeMarble" },
        ["tni02"] = new(StringComparer.OrdinalIgnoreCase),
        ["tid01"] = new(StringComparer.OrdinalIgnoreCase) { "MazeMosaic" },
        ["tii01"] = new(StringComparer.OrdinalIgnoreCase),
        ["tni01"] = new(StringComparer.OrdinalIgnoreCase),
        ["tsw01"] = new(StringComparer.OrdinalIgnoreCase),
        ["twc03"] = new(StringComparer.OrdinalIgnoreCase),
        // ttd01: "Dunes" is the profile's declared RampCrosser (raised dune-face lanes, credited via
        // the relief/ramp classifiers), and Wall/Road/Trench are unwired same-name tunnel families
        // whose flat, door-free tiles all resolve via CornerEdgeResolver regardless (the resolver
        // registers every non-door crosser tile) -- no entries needed.
        ["ttd01"] = new(StringComparer.OrdinalIgnoreCase),
        // ttf01's hak copy carries eight crosser families beyond the wired Bridge channel, the
        // declared Slope ramp crosser, and the resolver-covered Wall/Road/Stream: DlaEdgeFix,
        // StoneBridge, RuralStream, MossWall, CityWall, RuinWall, RuralWallOne/Two. Entries here
        // exempt the few flat door/group tiles on these families (e.g. "Bridge - Footbridge, Rural
        // Stream", "Wall - Gate, Ruin"); their flat door-free tiles resolve via CornerEdgeResolver
        // regardless.
        ["ttf01"] = new(StringComparer.OrdinalIgnoreCase)
        {
            "DlaEdgeFix", "StoneBridge", "RuralStream", "MossWall", "CityWall", "RuinWall",
            "RuralWallOne", "RuralWallTwo",
        },
        ["ttf02"] = new(StringComparer.OrdinalIgnoreCase),
        // jac01: all five crosser families (Wall/Road/Stream/Bridge/Hills) are wired directly on the
        // base Jungle profile (Hills as RampCrosser, Bridge via ChannelTerrain, the rest resolver-
        // covered) -- no unwired crosser family exists, so no entries are needed.
        ["jac01"] = new(StringComparer.OrdinalIgnoreCase),
        // fcx01: "pont" (Bridge-equivalent, gates the holes chasm) has no wired body/port or
        // DoorSlotCrossers vocabulary -- see BaseGameTilesetProfiles.FutCity's own doc comment. "murs"
        // is NOT here: it's wired via DoorSlotCrossers("murs"). "Routes" is no longer here either:
        // BaseGameTilesetProfiles.FutCity/FutCityPlaza now declare it as RoadCrosser (see
        // LayoutRoadCarver/RoadVocabularyCheck) -- the census now credits TILE207-216 via
        // IsCornerEdgeResolverReachable the same way every other declared-and-verified crosser family
        // in this file already is, rather than auto-exempting them as unwired.
        ["fcx01"] = new(StringComparer.OrdinalIgnoreCase) { "pont" },
        // tjsb0: "bridge"/"fence" are both wired vocabulary (Bridge is canonical; fence-crossered doors
        // are all GROUPed, see BaseGameTilesetProfiles.SecretBase's own doc comment) -- no alternates.
        ["tjsb0"] = new(StringComparer.OrdinalIgnoreCase),
        // tbx78: doorway1/doorway2/doorway3/cell/raised are all declared via DoorSlotCrossers (see
        // BaseGameTilesetProfiles.ModernFacility) -- no unwired alternates remain.
        ["tbx78"] = new(StringComparer.OrdinalIgnoreCase),
        // tqq01: Corridor/Doorway are both canonical -- no alternates.
        ["tqq01"] = new(StringComparer.OrdinalIgnoreCase),
        // udp2: "Door"/"Door_Garage_Sm"/"Door_Garage_Lg" are declared via DoorSlotCrossers for
        // CornerEdgeResolver's ungrouped-tile path (see BaseGameTilesetProfiles.OfficeInteriors), but
        // DoorSlotCrossers has NO effect on GROUP classification (LayoutGroupStamper.TryClassifyGroup's
        // IsAllowedMemberEdge only ever allows the literal canonical "Doorway" string or a Tunnel-mode
        // stub crosser) -- verified directly. Every GROUP whose door-bearing member carries "Door" (every
        // district's Entry/SmRm1/SmRm2/MidRm1/MidRm2 pair, plus Elevator1/2/Stairwell_U/UD/D/Restrooms/
        // Break_Room) is therefore auto-exempted here rather than hand-listed one-by-one, the same
        // "unwired crosser family" treatment fcx01's "pont" and ttf01's eight crosser families already
        // get. Hallway1/Hallway2 (district-junction wall crossers, no verified Tunnel vocabulary either)
        // stay here too.
        ["udp2"] = new(StringComparer.OrdinalIgnoreCase)
        {
            "Door", "Door_Garage_Sm", "Door_Garage_Lg", "Hallway1", "Hallway2",
        },
    };

    private static bool UsesOnlyAlternateVocab(TilesetModel model, IEnumerable<TileRecord> members, string tilesetResref)
    {
        var terrains = PilotAlternateVocabTerrains[tilesetResref];
        var crossers = PilotAlternateVocabCrossers[tilesetResref];

        var anyAlternateTerrain = members.Any(m => m.Corners.Any(c => terrains.Contains(c ?? string.Empty)));
        var anyAlternateCrosser = members.Any(m => m.Edges.Any(e => !string.IsNullOrEmpty(e) && crossers.Contains(e)));
        return anyAlternateTerrain || anyAlternateCrosser;
    }

    /// <summary>
    /// Named, reasoned exemptions for the pilot tilesets (tdc01/tde01/tin01, see
    /// BaseGameTilesetProfiles) that are NEITHER height-dependent (tagged automatically, see
    /// HeightExemptionReason) NOR alternate-vocabulary (tagged automatically, see
    /// PilotAlternateVocabTerrains/Crossers): a genuine gap in the shared classification mechanisms
    /// themselves.
    ///
    /// The "*Room01_1x2"/"*Room02_1x2" door-entrance-pair family that used to fill most of this list
    /// (tin01/tic01/tii01/tni01/twc03's blank-wall-tile-plus-Doorway-and-door-slot-entrance-tile
    /// groups) is CLOSED: LayoutGroupStamper.TryClassify's WallRoom branch now tolerates a door slot
    /// the same way WallAlcove/OpenSetPiece already did (see that method's own doc comment) -- a
    /// door-entrance group classifies as SetPieceWallRoom as long as it's still all-solid-cornered with
    /// at least one real PERIMETER Doorway opening (an interior-only Doorway edge shared between two
    /// members of the SAME group, e.g. tic01's Turret Interior Lit/Dark pair, still correctly fails and
    /// stays exempt below). See BaseGameTilesetProfiles.CityInterior/CastleInterior/CastleInterior2/
    /// IllithidInterior/CityInterior2/FortInterior for the SetPiece(...) wiring that surfaces this.
    /// </summary>
    private static readonly HashSet<(string Tileset, string Label)> PilotExpectedExemptions = new()
    {
        // Barrows (tbw01): CorridorDown_1x2/Corridor_Up_1x2/Corridor_Up_1x2_02 (1x2 multi-tile groups
        // whose outer member carries a lone perimeter "corridor" body-crosser edge) now classify via
        // LayoutGroupStamper's dedicated CorridorStubChain kind (a multi-tile CorridorStub splice, not a
        // WallRoom port pairing -- see that class's TryPlaceCorridorStubChain), and TILE13 (an ungrouped
        // boundary tile pairing a door slot with a bare "corridor" edge) now classifies via
        // IsCornerEdgeResolverReachable now that BaseGameTilesetProfiles.Barrows declares "corridor" as
        // an extra DoorSlotCrosser alongside "door_corridor". TILE51 stays exempt: a diagonal-split-
        // corner door tile with NO crosser at all (excluded by the "door implies TileDoorPlanner's
        // inventory" rule, which requires a genuine Doorway edge TileDoorPlanner never finds here) -- a
        // genuinely different, unaddressed door mechanism. See BaseGameTilesetProfiles.Barrows' own doc
        // comment for the door_barrow family this list used to also cover (split into the auto-tagged
        // alternate-vocabulary bucket).
        ("tbw01", "TILE51"),

        // Mines and Caverns (tdm01): "[Cave] Ship - Docked"/"[Cave] Docks (1x2)" don't structurally
        // classify under any current mechanism (their corner/edge shapes don't match WallRoom/
        // WallAlcove/OpenSetPiece/CorridorInsert/CorridorStub). "[Cave] Door - Bridge, Pit"/"Lava" are
        // the same Bridge-gated door shape as the wired "[Cave] Door - Bridge, Water" but on the two
        // unwired accent terrains (this profile's single AccentTerrain slot only wires Water) -- see
        // BaseGameTilesetProfiles.MinesAndCaverns.
        //
        // tdm01's residual auto-tagged "requires height support" bucket (14 tiles) is evidence-backed
        // unreachable, not a mechanism gap the relief pass could close:
        //   - TILE1452/1453 ("half-and-half" CityWater/Floor diagonal-grade banks): the relief BFS
        //     mirror (IsTerrainReliefReachable) finds NO resolving single-corner mutation chain from
        //     the flat CityWater/Floor base -- the tileset carries no intermediate tile for any
        //     construction order, so perturb-and-verify can never commit its way there.
        //   - TILE1427/1471/1472/1590 (Stream-crossered waterfalls) and TILE1591/1592 (Bridge-crossered
        //     raised CityWater banks): their crossers are outside the relief lane vocabulary ("Slope"
        //     here) -- Stream is an unwired one-off family (3 Floor tiles + 1 CityWater tile, no
        //     carver writes it), and Bridge lanes are only ever written by LayoutAccentChannelCarver's
        //     flat channel spans.
        //   - TILE1613 (Road+Slope junction): mixes a second unwired crosser family (Road) into its
        //     Slope lane, so an all-Slope lane splice can never produce it.
        //   - TILE1619: its corners use the literal terrain name "UNUSED" -- authored dead content.
        //   - TILE1456/1665/1695/1758 ("[City]/[Cave]/[Desert]/[Organic] Cave Entrance" 1x1 groups):
        //     raised AND door-slot-bearing -- ReliefPiece stamping is doorless-only and
        //     GroupExitPlanner is flat-only, so no mechanism can place a raised door group.
        ("tdm01", "GROUP:[Cave] Ship - Docked"),
        ("tdm01", "GROUP:[Cave] Docks (1x2)"),
        ("tdm01", "GROUP:[Cave] Door - Bridge, Pit"),
        ("tdm01", "GROUP:[Cave] Door - Bridge, Lava"),

        // Castle Interior (tic01): every "Room - <Type> 1/2 (1x2)" door-entrance pair, the "Room -
        // Storage, Empty (2x1)" pair, and the Room - Bath 1/2 pair now classify as SetPieceWallRoom
        // (see this class's own doc comment on the door-slot relaxation). The Turret Interior Lit/Dark
        // pair is genuinely different and stays exempt: TILE667's only Doorway edge faces its OWN
        // group-mate TILE668 (the shared 2x1 boundary), and TILE677 (Dark's own unique member, sharing
        // TILE668 with Lit) is the same shape -- an interior-only opening with zero real perimeter
        // Doorway edge, which LayoutGroupStamper.IsWallRoomSiteValid's perimeterDoorways check requires
        // at least one of (verified via direct probe: this group's own doc-comment claim that both
        // shapes are identical "half turn / half stack" rooms with no outward-facing entrance at all).
        ("tic01", "GROUP:[Castle] Turret Interior - Lit (2x1)"),
        ("tic01", "GROUP:[Castle] Turret Interior - Dark (2x1)"),

        // Castle Interior 2 (tni02): the door-entrance-pair family and CollapsedRoom2x2 now classify
        // (same relaxation as Castle Interior). Mythallar_3x3 (whose shared member edges carry the
        // plain "corridor" crosser, not Doorway) now classifies too, via LayoutGroupStamper's dedicated
        // CorridorStubChain kind -- see BaseGameTilesetProfiles.CastleInterior2's own doc comment.

        // Fort Interior (twc03): the CURRENT (non-"OLD_") furnished-room groups (StoreRoom_2x2L,
        // Cells_2x2, Kitchen_1x2, Generic_Room_2x1/2x2, Barracks_2x2, Bedroom_02_2x2/03_2x1, Smithy_1x2,
        // Portal_Hall_2x3) all use the canonical "doorway" (case-insensitive) crosser on their entrance
        // tile and now classify as SetPieceWallRoom. The legacy "OLD_"-prefixed superseded groups
        // (OLD_Smithy_1x2, OLD_Kitchen_1x2, OLD_Bedroom_02_2x1/03_2x1, OLD_Barracks_2x2,
        // OLD_Portal_Hall_2x3, OLD_StoreRoom_2x2L_old, OLD_Cells_2x2_old, OLD_Generic_Room_2x1/2x2) and
        // Mythallar_3x3 use the plain "corridor" body crosser directly on their entrance/wall tile
        // instead of a Doorway-family port -- LayoutGroupStamper's CorridorStubChain classification now
        // reaches this shape (see BaseGameTilesetProfiles.FortInterior/FortInteriorLegacy's own doc
        // comments), so they classify as SetPieceCorridorStubChain and are no longer exempt. Large_Door
        // remains exempt: its TILE36 has mixed floor/black corners, so it fails the all-solid
        // CorridorStubChain/CorridorInsert/CorridorStub checks too. TILE23/29 (a solid or mixed boundary
        // tile pairing a door slot with a bare "corridor" edge) and TILE95/96/105/106 (an open-floor gate
        // tile pairing a door slot with one or three "wall" edges) now classify via CornerEdgeResolver
        // now that BaseGameTilesetProfiles.FortInterior/FortInteriorLegacy declare "corridor"/"wall" as
        // extra DoorSlotCrossers. TILE125/127/128 stay exempt: a door slot with NO crosser at all on
        // diagonal-split or single-corner-cut corners -- genuinely unreachable (TileDoorPlanner's
        // TryGetSingleDoorwaySlot requires a genuine Doorway edge, which none of these three have).
        ("twc03", "GROUP:Large_Door"),
        ("twc03", "TILE125"),
        ("twc03", "TILE127"),
        ("twc03", "TILE128"),

        // Desert (ttd01, hak copy) -- see BaseGameTilesetProfiles.Desert's own doc comment for the
        // full reasoning. WallGate01/02 and TrenchBridge01/02 each carry TWO independent crosser
        // families (Wall+Road / Trench+Road) on perpendicular opposite-edge pairs of the SAME tile --
        // a "crossroads" gate shape no current mechanism models (both IsCorridorInsertEligible and
        // ClassifyMultiTileSetPiece's IsAllowedMemberEdge require a single recognized crosser family
        // per tile), and under the inverted composition their corners are OPEN (Desert) anyway, where
        // no crosser-bearing group shape exists at all. Everything else in the hak's flat inventory
        // classifies -- the all-Desert building/decor groups as OpenSetPieces, the ChasmStairs/Exit/
        // CliffStairs/CaveEntrance door tiles as ExitGroups, and the Svirfneblin/Poor door tiles +
        // the residual raised content via the two auto-tagged buckets (see
        // PilotAlternateVocabTerrains["ttd01"] and the profile's own height-evidence comment).
        ("ttd01", "GROUP:WallGate01"),
        ("ttd01", "GROUP:WallGate02"),
        ("ttd01", "GROUP:TrenchBridge01"),
        ("ttd01", "GROUP:TrenchBridge02"),

        // Forest (ttf01, hak copy) -- see BaseGameTilesetProfiles.Forest's own doc comment.
        // "Wall - Gate 1/2, Forest" (Wall+Road) and "Bridge - Stream 1/2, Forest" (Stream+Road) are
        // the identical two-crosser-family crossroads cells as Desert's. "Tower - Archer, Forest
        // Wall" (TILE678, an opposite-Wall pair) and "Tower - Archer, Forest Wall Corner" (TILE677,
        // an ADJACENT-Wall pair) sit on all-OPEN (Forest) corners: CorridorInsert's body-crosser
        // shapes require all-SOLID corners (and the composed solid is Cliff, which no crosser family
        // crosses -- see the wave comment in BaseGameTilesetProfiles), and 677's adjacent-pair turn
        // shape is one CorridorInsert never matches under ANY family regardless. "Tower - Guard,
        // Pit" (TILE963, uniform Pit, doorless, pathnode-restricted) and "Island (3x3)" (uniform
        // Pit, doorless) sit on accent/channel terrain with no door slot: matchesPrimary requires at
        // least one Open corner even under BaseGameTilesetProfiles.ForestPlatform's own
        // SolidTerrainOverride("Pit") composition (see that profile's doc comment -- it closed the
        // sibling "Ship - Air, Above Pit (3x1)" group precisely because THAT group has a door slot,
        // reaching WallAlcove's allCornersSolid+hasAnyDoor branch instead), so a doorless uniform-Pit
        // group stays genuinely unreachable (the same gap as tdm01's Ship - Docked/Docks).
        // "Temple - Elven 2 (3x3)" (Pit+Forest mixed, still no single Solid+Open pair covers both
        // since Forest is Open under the base profile but Pit is Open under ForestPlatform -- neither
        // profile's Solid matches the other's Open on the same group). "Bridge - Pit/Log/Rickety
        // (1x3)" (Forest|Pit|Forest) and "Bridge - Forest Water (1x3)" (Forest|Water) are crosser-
        // FREE channel-spanning decor -- nothing carves the fixed 3-cell accent span they'd need to
        // straddle. "Cave - Cliff (2x3)" mixes THREE terrains (Cliff/Water/Forest), outside every
        // two-terrain classifier (the Crypt Dwarven Cave Entrance gap). "House - Treehouse 3 (2x2)"
        // mixes Water+Forest (accent-mixed corners).
        ("ttf01", "GROUP:Wall - Gate 1, Forest"),
        ("ttf01", "GROUP:Wall - Gate 2, Forest"),
        ("ttf01", "GROUP:Bridge - Stream 1, Forest"),
        ("ttf01", "GROUP:Bridge - Stream 2, Forest"),
        ("ttf01", "GROUP:Tower - Archer, Forest Wall"),
        ("ttf01", "GROUP:Tower - Archer, Forest Wall Corner"),
        ("ttf01", "GROUP:Tower - Guard, Pit"),
        ("ttf01", "GROUP:Island (3x3)"),
        ("ttf01", "GROUP:Temple - Elven 2 (3x3)"),
        ("ttf01", "GROUP:Bridge - Pit (1x3)"),
        ("ttf01", "GROUP:Bridge - Log (1x3)"),
        ("ttf01", "GROUP:Bridge - Rickety (1x3)"),
        ("ttf01", "GROUP:Bridge - Forest Water (1x3)"),
        ("ttf01", "GROUP:Cave - Cliff (2x3)"),
        ("ttf01", "GROUP:House - Treehouse 3 (2x2)"),

        // Forest - Facelift (ttf02, BIF-only vanilla) -- see BaseGameTilesetProfiles.ForestFacelift's
        // own doc comment. WallGate01/02 (Wall+Road) and StreamBridge01/02 (Stream+Road) are the
        // same crossroads cells; Island_Tree (uniform-Pit 3x3 with one Bridge edge that never
        // triggers CorridorStubChain, Bridge not being a body crosser) and Island_Connector (1x1,
        // Forest+Pit mixed) are accent-terrain groups no mechanism stamps -- the same gap as ttf01's
        // Island (3x3). The all-Forest decor (Ruin01/02, Camp02, Graveyard_1x2, Meeting_Area,
        // Grove01_3x3) classifies as OpenSetPieces under the inverted composition.
        ("ttf02", "GROUP:WallGate01"),
        ("ttf02", "GROUP:WallGate02"),
        ("ttf02", "GROUP:StreamBridge01"),
        ("ttf02", "GROUP:StreamBridge02"),
        ("ttf02", "GROUP:Island_Tree"),
        ("ttf02", "GROUP:Island_Connector"),

        // Jacoby's Jungle (jac01) -- see BaseGameTilesetProfiles.Jungle's own doc comment for the full
        // writeup. WallGate01/02 (Wall+Road) and StreamBridge01/02 (Stream+Bridge) are the same two-
        // independent-crosser-family crossroads shape as Desert/Forest/Forest-Facelift's own gate
        // groups. "Hills w/Road" (TILE184) carries BOTH Hills AND Road edges on one raised cell -- the
        // same dual-crosser conflict as ttd01's TILE255 (Dunes+Road). "Pit Tower" (all-Pit, no door)
        // and "AirshipAbovePit_3x1" (all-Pit, one door) sit purely on the Bridge-gated channel terrain
        // with no Solid or Open corner anywhere in the group -- the same channel-only-group gap as
        // ttf01's "Island"/"Island_Tree" family. "CarrackD_4x1" and "CaravelFloating_3x1" (both
        // all-Water) are the same accent-terrain-only-group gap (AccentTerrain is a painted overlay,
        // not a Solid/Open composition member). "Platform Cliff Dwellings 2x3" and "Platform Cliff
        // Door" mix Cliff+Pit+Platform (three terrains -- no two-terrain classifier reaches either),
        // the same residual as ttf01's own "Platform - Cliff Door"/"Platform - Cliff Section".
        ("jac01", "GROUP:WallGate01"),
        ("jac01", "GROUP:WallGate02"),
        ("jac01", "GROUP:StreamBridge01"),
        ("jac01", "GROUP:StreamBridge02"),
        ("jac01", "GROUP:Hills w/Road"),
        ("jac01", "GROUP:Pit Tower"),
        ("jac01", "GROUP:CarrackD_4x1"),
        ("jac01", "GROUP:CaravelFloating_3x1"),
        ("jac01", "GROUP:Platform Cliff Dwellings 2x3"),
        // "Log Bridge_1x3"/"Suspension Bridge_1x3": a uniformly-Pit-cornered middle tile (no Solid or
        // Open corner) flanked by two half-Forest/half-Pit bank tiles -- ClassifyMultiTileSetPiece's
        // Solid/Open binary never triggers once any one member is pure-channel, unlike PitStair's
        // single-tile half-and-half shape. "Walkthrough Tree" (TILE292) is a single-tile GROUP with
        // all-OPEN (Forest) corners and an opposite-edge Road pair: CorridorInsert requires all-SOLID
        // corners, and being GROUP-wrapped excludes it from CornerEdgeResolver -- the same shape as
        // ttf01's "Tower - Archer, Forest Wall/Corner" residual.
        ("jac01", "GROUP:Log Bridge_1x3"),
        ("jac01", "GROUP:Suspension Bridge_1x3"),
        ("jac01", "GROUP:Walkthrough Tree"),

        // D20 Futuristic City SW (fcx01) -- see BaseGameTilesetProfiles.FutCity's own doc comment for
        // the full writeup. "platform1" (2x2, uniformly holes-cornered, one door-bearing member) has no
        // wired path: the doorless pure-Solid shape "b_tower02"/"d_tower02" use classifies fine, but a
        // door-bearing one does not (verified directly). "b_wall_door"/"d_wall_door" (1x1, pure-Open-
        // cornered, a "murs" crosser edge on two opposite sides, one door) also fail: DoorSlotCrossers
        // only ever credits ungrouped tiles (GroupIndex != -1 excludes this GROUP structurally), and no
        // GROUP-level mechanism recognizes an Open-cornered piece carrying a non-canonical crosser plus
        // a door. "b_road_door"/"d_road_door" (TILE235/236, verified directly via ZZ-style probe) are
        // the identical shape one crosser richer -- "murs" on the Top/Bottom edges (a wall gate) PLUS
        // "Routes" on Right/Left (the road passing through the gate) -- and fail for the same reason:
        // no GROUP-level mechanism recognizes a door-bearing 1x1 carrying this non-canonical crosser
        // pair, regardless of "Routes" now being wired as RoadCrosser for LayoutRoadCarver's own
        // open-terrain (crosser-free-of-doors) lane cells. Wiring a wall-gate-with-road-door mechanism
        // is out of this pass's scope (LayoutRoadCarver never carves through "murs" walls at all).
        ("fcx01", "GROUP:platform1"),
        ("fcx01", "GROUP:b_wall_door"),
        ("fcx01", "GROUP:d_wall_door"),
        ("fcx01", "GROUP:b_road_door"),
        ("fcx01", "GROUP:d_road_door"),

        // D20 Secret Base (tjsb0): Caveentrance (2x1, TILE172/173) mixes THREE terrains on its two
        // members (lava/floor/wall diagonal splits), crosser-free, one door -- outside every
        // two-terrain classifier (WallAlcove/OpenSetPiece need a uniform Solid or Open corner set; this
        // group has neither), the same "Cave - Cliff (2x3)" gap ttf01 already documents. See
        // BaseGameTilesetProfiles.SecretBase's own doc comment.
        ("tjsb0", "GROUP:Caveentrance"),

        // D20 Modern Facility (tbx78): every group below carries a "doorway1"/"doorway2" perimeter edge
        // on its door-bearing member -- LayoutGroupStamper.TryClassifyGroup's IsAllowedMemberEdge only
        // ever allows the literal canonical "Doorway" string or a Tunnel-mode stub crosser, never a
        // DoorSlotCrossers-declared alias (that only credits CornerEdgeResolver's ungrouped-tile path).
        // See BaseGameTilesetProfiles.ModernFacility's own doc comment for the full writeup -- this closes
        // essentially this tileset's ENTIRE room/utility group vocabulary except removed_panel/
        // giant_cage/pillar, a genuine, verified solver-incompatibility for group content (ordinary flat
        // tile coverage is unaffected -- CornerEdgeResolver still resolves 64/84 plain tiles fine).
        ("tbx78", "GROUP:ladder_up"),
        ("tbx78", "GROUP:ladder_dwn"),
        ("tbx78", "GROUP:room2x1"),
        ("tbx78", "GROUP:stairs_up"),
        ("tbx78", "GROUP:room"),
        ("tbx78", "GROUP:stairs_dwn"),
        ("tbx78", "GROUP:elevator"),
        ("tbx78", "GROUP:room3x1"),
        ("tbx78", "GROUP:door_transition"),

        // D20 Office Interiors UDP (udp2): the identical IsAllowedMemberEdge gap as tbx78 above, against
        // the "Door" crosser name -- auto-exempted via PilotAlternateVocabCrossers["udp2"] (see that
        // dictionary's own doc comment) rather than hand-listed here, since it closes every district's
        // Entry/SmRm1/SmRm2/MidRm1/MidRm2 pair uniformly. Only the Office_Vinyl district's own
        // Win/WinCrnr/Firepl/Stair_UD/U/D/Stair2_UD/U/D (crosser-free) and the tileset-generic
        // Hallway1_Entry/Hallway2_Entry stay reachable. See BaseGameTilesetProfiles.OfficeInteriors' own
        // doc comment.
    };

    public static IEnumerable<string> PilotTilesetKeys => new[]
    {
        "tdc01", "tde01", "tin01",
        "tbw01", "tdm01", "tdr01", "tic01", "tni02", "tid01", "tii01", "tni01", "tsw01", "twc03",
        "ttd01", "ttf01", "ttf02",
        "jac01",
        "fcx01",
        "tjsb0", "tbx78", "tqq01", "udp2",
    };

    [TestCaseSource(nameof(PilotTilesetKeys))]
    public void PilotEveryTileIsReachableOrExplicitlyExempted(string tilesetResref)
    {
        var model = LoadTileset(tilesetResref);
        var profileKey = tilesetResref switch
        {
            "tdc01" => BaseGameTilesetProfiles.Crypt,
            "tde01" => BaseGameTilesetProfiles.Dungeon,
            "tin01" => BaseGameTilesetProfiles.CityInterior,
            "tbw01" => BaseGameTilesetProfiles.Barrows,
            "tdm01" => BaseGameTilesetProfiles.MinesAndCaverns,
            "tdr01" => BaseGameTilesetProfiles.Ruins,
            "tic01" => BaseGameTilesetProfiles.CastleInterior,
            "tni02" => BaseGameTilesetProfiles.CastleInterior2,
            "tid01" => BaseGameTilesetProfiles.DrowInterior,
            "tii01" => BaseGameTilesetProfiles.IllithidInterior,
            "tni01" => BaseGameTilesetProfiles.CityInterior2,
            "tsw01" => BaseGameTilesetProfiles.Steamworks,
            "twc03" => BaseGameTilesetProfiles.FortInterior,
            "ttd01" => BaseGameTilesetProfiles.Desert,
            "ttf01" => BaseGameTilesetProfiles.Forest,
            "ttf02" => BaseGameTilesetProfiles.ForestFacelift,
            "jac01" => BaseGameTilesetProfiles.Jungle,
            "fcx01" => BaseGameTilesetProfiles.FutCity,
            "tjsb0" => BaseGameTilesetProfiles.SecretBase,
            "tbx78" => BaseGameTilesetProfiles.ModernFacility,
            "tqq01" => BaseGameTilesetProfiles.LabStorage,
            "udp2" => BaseGameTilesetProfiles.OfficeInteriors,
            _ => throw new ArgumentOutOfRangeException(nameof(tilesetResref))
        };
        // A tile/group counts as reachable if ANY profile sharing this TilesetResref composes it --
        // not just the primary one keyed by profileKey above. This closes the "alternate-palette"
        // exemption bucket honestly: registering a palette-variant profile (e.g. "crypt_grey" alongside
        // "crypt", both TilesetResref "tdc01" -- see BaseGameTilesetProfiles.IsPaletteVariant) is a real
        // structural unlock the census now recognizes, the same way an "optional config" tile was always
        // exactly as reachable as a currently-wired one per this class's own doc comment. The primary
        // profile is always included (a resref with no variants yields a single-entry list, unchanged
        // behavior from before this loop existed).
        var profilesForResref = new BaseGameTilesetProfiles().BuildTilesetProfiles().Values
            .Where(p => Eq(p.TilesetResref, tilesetResref))
            .ToList();
        var allVocabs = profilesForResref.Select(p => BuildVocabulary(model, p)).ToList();
        // Union across every profile sharing this TilesetResref, mirroring allVocabs' own "any variant
        // profile" reachability rule in reverse: a tile confirmed as placeholder/stub art (see
        // DungeonTilesetProfile.ExcludedTiles) is never genuinely reachable regardless of which profile
        // variant a future change might wire it through.
        var excludedTiles = profilesForResref.SelectMany(p => p.ExcludedTiles).ToHashSet();
        // Built once per tileset and shared by every relief BFS probe below -- see
        // TileResolver.HeightAwareProbeCache's own doc comment on why per-probe rebuilds are not ok.
        var probeCache = TileResolver.BuildHeightAwareProbeCache(model);

        var coveredTileIds = new HashSet<int>();
        var mechanismCounts = new Dictionary<string, int>();
        var exemptions = new List<Exemption>();
        // Some Wave-2 hak mega-sets (e.g. tdm01) reuse the SAME physical tile id across two named
        // groups (an alternate-district variant sharing art with its sibling, e.g. "[Cave] Stairs -
        // Up, Water (2x2)" / "[Cave] Stairs - Up, Lava (2x2)" both listing tile 126) -- TilesetSetParser
        // resolves TileRecord.GroupIndex to whichever group claims it FIRST, but this census still walks
        // every group's OWN declared TileIds regardless of which group "owns" a shared tile per
        // GroupIndex. Deduplicating exemptions by tile id here (mirroring coveredTileIds' HashSet
        // idempotency) keeps a shared tile from being counted twice when both of its owning groups
        // independently exempt it (e.g. both alternate-vocabulary), which would otherwise break the
        // covered+exempt == total-tiles invariant even though every tile is still honestly accounted
        // for exactly once.
        var exemptedTileIds = new HashSet<int>();

        void Cover(int tileId, string mechanism)
        {
            coveredTileIds.Add(tileId);
            mechanismCounts[mechanism] = mechanismCounts.GetValueOrDefault(mechanism) + 1;
        }

        void Exempt(int tileId, string label, string reason)
        {
            if (coveredTileIds.Contains(tileId)) return;
            if (!exemptedTileIds.Add(tileId)) return;
            exemptions.Add(new Exemption { Tileset = tilesetResref, TileOrGroup = label, Reason = reason });
        }

        foreach (var group in model.Groups)
        {
            var members = group.TileIds.Where(id => id >= 0 && id < model.Tiles.Count).Select(id => model.Tiles[id]).ToList();
            if (members.Count == 0) continue;
            var memberIds = members.Select(m => m.TileId).ToList();

            // Confirmed placeholder/stub art: checked BEFORE mechanism classification (not merely as a
            // fallback) -- a group whose signature tile is broken art is never a usable set piece
            // regardless of whether its shape would otherwise classify, and no profile still wires it
            // (see BaseGameTilesetProfiles.FortInteriorLegacy, which removed these groups' SetPiece
            // calls entirely).
            if (memberIds.Any(excludedTiles.Contains))
            {
                foreach (var id in memberIds) Exempt(id, $"TILE{id} (group '{group.Name}')", PlaceholderArtExemptionReason);
                continue;
            }

            var mechanism = GroupMechanism.None;
            if (IsFeatureTileEligible(model, group)) mechanism = GroupMechanism.FeatureTile;
            else if (IsExitGroupEligible(model, group)) mechanism = GroupMechanism.ExitGroup;
            else
            {
                foreach (var candidateVocab in allVocabs)
                {
                    mechanism = ClassifySetPiece(model, group, candidateVocab, probeCache);
                    if (mechanism != GroupMechanism.None) break;
                }
            }

            if (mechanism != GroupMechanism.None)
            {
                foreach (var id in memberIds) Cover(id, mechanism.ToString());
                continue;
            }

            if (members.Any(m => !IsFlat(m)))
            {
                foreach (var id in memberIds) Exempt(id, $"TILE{id} (group '{group.Name}')", HeightExemptionReason);
                continue;
            }

            if (UsesOnlyAlternateVocab(model, members, tilesetResref))
            {
                foreach (var id in memberIds) Exempt(id, $"TILE{id} (group '{group.Name}')", AlternateVocabExemptionReason);
                continue;
            }

            if (PilotExpectedExemptions.Contains((tilesetResref, "GROUP:" + group.Name)))
            {
                foreach (var id in memberIds)
                    Exempt(id, $"TILE{id} (group '{group.Name}')", "see PilotExpectedExemptions doc comment");
            }
        }

        for (var tileId = 0; tileId < model.Tiles.Count; tileId++)
        {
            if (coveredTileIds.Contains(tileId)) continue;
            if (exemptedTileIds.Contains(tileId)) continue;

            var tile = model.Tiles[tileId];

            if (excludedTiles.Contains(tileId)) { Exempt(tileId, $"TILE{tileId}", PlaceholderArtExemptionReason); continue; }

            if (allVocabs.Any(candidateVocab => IsCornerEdgeResolverReachable(model, tile, candidateVocab)))
            { Cover(tileId, "CornerEdgeResolver"); continue; }
            if (IsDoorTransitionReachable(model, tile)) { Cover(tileId, "DoorTransition"); continue; }

            var vocabMechanism = string.Empty;
            foreach (var candidateVocab in allVocabs)
            {
                if (IsElevationBlobReachable(tile, candidateVocab)) { vocabMechanism = "ElevationBlob"; break; }
                if (IsElevationRampReachable(tile, candidateVocab)) { vocabMechanism = "ElevationRamp"; break; }
                if (IsPoolBankReachable(tile, candidateVocab)) { vocabMechanism = "PoolBank"; break; }
                if (IsTerrainReliefReachable(tile, candidateVocab, probeCache)) { vocabMechanism = "TerrainRelief"; break; }
            }
            if (vocabMechanism.Length != 0) { Cover(tileId, vocabMechanism); continue; }

            if (!IsFlat(tile)) { Exempt(tileId, $"TILE{tileId}", HeightExemptionReason); continue; }
            if (UsesOnlyAlternateVocab(model, new[] { tile }, tilesetResref)) { Exempt(tileId, $"TILE{tileId}", AlternateVocabExemptionReason); continue; }
            if (PilotExpectedExemptions.Contains((tilesetResref, "TILE" + tileId))) { Exempt(tileId, $"TILE{tileId}", "see PilotExpectedExemptions doc comment"); continue; }

            Exempt(tileId, $"TILE{tileId}", "UNCLASSIFIED");
        }

        // ---- report ----
        var coveragePercent = model.Tiles.Count == 0 ? 100.0 : 100.0 * coveredTileIds.Count / model.Tiles.Count;
        TestContext.WriteLine($"=== PILOT {tilesetResref} ({profileKey}) coverage: {coveredTileIds.Count}/{model.Tiles.Count} ({coveragePercent:0.0}%) ===");
        foreach (var kv in mechanismCounts.OrderByDescending(k => k.Value))
            TestContext.WriteLine($"  {kv.Key,-24} {kv.Value,4} tiles");
        foreach (var kv in exemptions.GroupBy(e => e.Reason).OrderByDescending(g => g.Count()))
            TestContext.WriteLine($"  EXEMPT: {kv.Key,-90} {kv.Count(),4} tiles");

        // ---- assertions ----
        // Every tile must be either reachable, or carry an honest, reasoned exemption (automatic
        // height/alternate-vocabulary tagging, or a pre-declared PilotExpectedExemptions entry) -- an
        // honest gap list is acceptable here (unlike the original four), an un-reasoned UNCLASSIFIED
        // tile is not.
        var unclassified = exemptions.Where(e => e.Reason == "UNCLASSIFIED").Select(e => e.TileOrGroup).ToList();
        unclassified.Should().BeEmpty($"every {tilesetResref} tile must be either reachable or carry an honest, reasoned exemption");

        // The manually-curated PilotExpectedExemptions subset (excludes the two auto-tagged
        // categories) must be EXACT -- any drift must be visible here, not silently absorbed.
        var manualExemptionLabels = exemptions
            .Where(e => e.Reason == "see PilotExpectedExemptions doc comment")
            .Select(e => e.TileOrGroup)
            .ToHashSet();
        var expectedManualLabels = model.Groups
            .Where(g => PilotExpectedExemptions.Contains((tilesetResref, "GROUP:" + g.Name)))
            .SelectMany(g => g.TileIds.Where(id => id >= 0))
            .Select(id => model.Tiles.First(t => t.TileId == id))
            .Where(t => IsFlat(t) && !UsesOnlyAlternateVocab(model, new[] { t }, tilesetResref))
            .Select(t => $"TILE{t.TileId} (group '{model.Groups[t.GroupIndex].Name}')")
            .ToHashSet();

        // Ungrouped-tile manual exemptions (e.g. Barrows' TILE13/TILE51, Fort Interior's plain door+
        // crosser tiles): a bare "TILE{n}" PilotExpectedExemptions key, honored only for a genuinely
        // ungrouped (GroupIndex == -1), flat, non-alternate-vocab tile -- mirrors the group-keyed
        // reconstruction above so ungrouped gaps get the same EXACT, no-silent-drift guarantee.
        foreach (var tile in model.Tiles)
        {
            if (tile.GroupIndex != -1) continue;
            if (!IsFlat(tile)) continue;
            if (UsesOnlyAlternateVocab(model, new[] { tile }, tilesetResref)) continue;
            if (PilotExpectedExemptions.Contains((tilesetResref, "TILE" + tile.TileId)))
                expectedManualLabels.Add($"TILE{tile.TileId}");
        }

        manualExemptionLabels.Should().BeEquivalentTo(expectedManualLabels,
            $"the {tilesetResref} manually-curated pilot exemption set must be EXACT -- any drift must be visible here, not silently absorbed");

        (coveredTileIds.Count + exemptions.Count).Should().Be(model.Tiles.Count);
    }
}
