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

    /// <summary>
    /// Mirrors LayoutGroupStamper.IsBodyCrosserName: true when <paramref name="edge"/> is a name this
    /// composition's body-crosser vocabulary already claims (canonical "Corridor"/"Alley", or
    /// vocab.TunnelBody when a Custom-mode alternate is declared) -- a tileset can legitimately declare
    /// its OWN body-crosser name as a DoorSlotCrosser too (tbw01 declares "corridor" itself, for one
    /// ungrouped boundary tile that pairs a door slot with a bare body-crosser edge) without that name
    /// ever meaning "Doorway port" for GROUP classification, where WallRoom (port) and
    /// CorridorStubChain (body) are mutually-exclusive branches keyed on exactly this distinction.
    /// Checked first by IsDoorwayEdge/TryMatchDoorwayEdge so body-crosser identity always wins.
    /// </summary>
    private static bool IsBodyCrosserName(string edge, TilesetVocabulary vocab) =>
        Eq(edge, CorridorCrosser) || Eq(edge, AlleyCrosser) || Eq(edge, vocab.TunnelBody);

    /// <summary>
    /// Mirrors LayoutGroupStamper.IsDoorwayEdge: true for the canonical "Doorway" crosser OR any of
    /// this profile's own declared alternate door-slot crossers (vocab.ExtraDoorSlotCrossers),
    /// EXCLUDING any name that already belongs to this composition's body-crosser vocabulary (see
    /// IsBodyCrosserName) -- generalizes every GROUP-path "is this a door edge" check below the same
    /// way IsCornerEdgeResolverReachable already treats ExtraDoorSlotCrossers for the ungrouped path
    /// (see IsExtra above, which deliberately has NO such exclusion -- the ungrouped admission gate
    /// is a broader OR-relaxation, not a WallRoom-vs-CorridorStubChain distinction), so this census
    /// actually reflects LayoutGroupStamper's post-generalization behavior instead of the
    /// pre-generalization literal-only gate.
    /// </summary>
    private static bool IsDoorwayEdge(string edge, TilesetVocabulary vocab) =>
        IsDoorway(edge) || (!IsBodyCrosserName(edge, vocab) && IsExtra(edge, vocab));

    /// <summary>Mirrors LayoutGroupStamper.TryMatchDoorwayEdge: same recognition as
    /// <see cref="IsDoorwayEdge(string, TilesetVocabulary)"/> but also returns which specific crosser
    /// name matched (canonical or a declared alternate), needed wherever the matched name itself is
    /// threaded onward (HasCorridorDoorwayAdapter's re-key search).</summary>
    private static bool TryMatchDoorwayEdge(string edge, TilesetVocabulary vocab, out string matched)
    {
        if (IsDoorway(edge)) { matched = DoorwayCrosser; return true; }
        if (!IsBodyCrosserName(edge, vocab))
        {
            foreach (var candidate in vocab.ExtraDoorSlotCrossers)
            {
                if (Eq(edge, candidate)) { matched = candidate; return true; }
            }
        }
        matched = null;
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
        string matchedDoorwayCrosser = null;
        for (var slot = 0; slot < 4; slot++)
        {
            var edge = tile.Edges[slot] ?? string.Empty;
            if (edge.Length == 0) continue;
            var matched = Eq(edge, vocab.TunnelPort) ? vocab.TunnelPort : null;
            if (matched == null && !TryMatchDoorwayEdge(edge, vocab, out matched)) { doorwayOnly = false; break; }
            if (matchedDoorwayCrosser == null) matchedDoorwayCrosser = matched;
            else if (!Eq(matchedDoorwayCrosser, matched)) { doorwayOnly = false; break; }
            hasDoorway[slot] = true;
        }
        if (!doorwayOnly) return false;

        var isVerticalPair = hasDoorway[EdgeSlot.Top] && hasDoorway[EdgeSlot.Bottom] && !hasDoorway[EdgeSlot.Left] && !hasDoorway[EdgeSlot.Right];
        var isHorizontalPair = hasDoorway[EdgeSlot.Left] && hasDoorway[EdgeSlot.Right] && !hasDoorway[EdgeSlot.Top] && !hasDoorway[EdgeSlot.Bottom];
        if (!isVerticalPair && !isHorizontalPair) return false;

        return HasCorridorDoorwayAdapter(model, vocab, matchedDoorwayCrosser);
    }

    /// <summary>True when the tileset carries at least one flat, all-solid-corner tile with exactly
    /// one body-crosser edge and its opposite edge carrying <paramref name="doorwayCrosser"/> (the
    /// other two blank) -- mirrors LayoutGroupStamper.HasCorridorDoorwayAdapter. vocab.TunnelBody
    /// defaults to the canonical "Corridor" (see BuildVocabulary), so this is unchanged for every
    /// profile that doesn't declare an alternate Tunnel crosser family. <paramref name="doorwayCrosser"/>
    /// generalizes the port search to whichever door-slot crosser the candidate group's own tile
    /// actually carries (canonical "Doorway", the profile's declared TunnelPortCrosser, or one of its
    /// DoorSlotCrossers alternates -- see TryMatchDoorwayEdge), mirroring LayoutGroupStamper's own
    /// post-generalization HasCorridorDoorwayAdapter signature.</summary>
    private static bool HasCorridorDoorwayAdapter(TilesetModel model, TilesetVocabulary vocab, string doorwayCrosser)
    {
        if (string.IsNullOrEmpty(doorwayCrosser)) return false;

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
                else if (Eq(edge, doorwayCrosser)) doorwaySlot = slot;
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
            string.IsNullOrEmpty(edge) || IsDoorwayEdge(edge, vocab) || Eq(edge, vocab.TunnelBody) || Eq(edge, AlleyCrosser);

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
                var isDoorway = IsDoorwayEdge(edge, vocab);
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

        var hasAnyDoorway = members.Any(m => m.Edges.Any(e => IsDoorwayEdge(e, vocab)));
        var hasAnyBodyCrosser = members.Any(m => m.Edges.Any(e => !IsDoorwayEdge(e, vocab) && (Eq(e, vocab.TunnelBody) || Eq(e, AlleyCrosser))));
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
            // Mirrors LayoutGroupStamper.TryClassify's own mixed/open-member fallthrough: a doorway
            // edge implies SetPieceWallRoom only when every corner is solid; a mixed shape is tolerated
            // ONLY when every doorway edge is interior (never perimeter) -- see production's own doc
            // comment on this exact branch for the WriteMember/EdgeCrosserGrid reasoning. Falls through
            // to the OpenSetPiece corner-match check below when that holds (e.g. udp2's "*_Entry 2x1"
            // family, tbx78's "elevator").
            if (allCornersSolid)
            {
                if (!hasAnyPerimeterDoorway) return GroupMechanism.None;
                return GroupMechanism.SetPieceWallRoom;
            }
            if (hasAnyPerimeterDoorway) return GroupMechanism.None;
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
        // ttf01's hak copy (sw_t_forest): "GoodCastle"/"EvilCastle"/"Marsh" are NO LONGER here --
        // confirmed DEAD entries, not real gaps (re-probed directly): every GoodCastle/EvilCastle/
        // Marsh-touching tile was ALREADY reachable regardless of vocab (the ~10 ungrouped simple
        // tiles per castle faction and Marsh's 11 ungrouped tiles via CornerEdgeResolver, the six
        // 1x1 castle door/breach GROUPS via IsExitGroupEligible, both vocab-independent structural
        // rules) -- removing all three terrain names from this set changes ttf01's census numbers not
        // at all, verified directly. BaseGameTilesetProfiles.ForestGoodCastle/ForestEvilCastle/
        // ForestMarsh now additionally wire this content for REAL generation-time placement (not just
        // census credit) -- see BaseGameTilesetProfiles.Forest's own doc comment for the full
        // census-vs-practice writeup and each variant's own doc comment for its placement-proof
        // evidence.
        //
        // Platform/HighForest are MOSTLY closed by BaseGameTilesetProfiles.ForestPlatform's
        // SolidTerrainOverride("Pit") + PrimaryOpenTerrain("Platform") variant (16/16 against Pit,
        // verified directly), leaving only the genuinely three-terrain "Platform - Cliff Section"
        // group (Platform+Cliff+Pit on one group, outside any two-terrain classifier) still tagged via
        // this dictionary -- "Platform - Cliff Door" is NOT also tagged despite mixing Platform+Cliff
        // (a prior pass's comment here was WRONG, re-verified directly and fixed on
        // BaseGameTilesetProfiles.ForestPlatform's own doc comment): it already satisfies
        // IsExitGroupEligible's vocab-independent structural rule. RuralTrees/RuralWater are now MOSTLY closed
        // too by BaseGameTilesetProfiles.ForestRural's AccentTerrain/ReliefBlendTerrain variant
        // (PoolBank/TerrainRelief, verified directly and via a real-generation placement proof) -- but
        // TILE849 (uniform RuralWater, door-bearing, Road-crossered) and TILE1114 (Forest/RuralTrees
        // mixed, door-bearing, Road-crossered) still need the TERRAIN entries here: "Road" is
        // deliberately NOT in PilotAlternateVocabCrossers["ttf01"] below (it's the base wired
        // crossroads-gate family, not an alternate one), and a door-bearing tile fails
        // CornerEdgeResolver's Doorway/Bridge/extra-only admission gate regardless of vocab -- verified
        // directly that removing RuralTrees/RuralWater from this set turns exactly these two tiles
        // UNCLASSIFIED. See BaseGameTilesetProfiles.Forest's own doc comment.
        ["ttf01"] = new(StringComparer.OrdinalIgnoreCase)
        {
            "Platform", "HighForest", "RuralTrees", "RuralWater",
        },
        ["ttf02"] = new(StringComparer.OrdinalIgnoreCase),
        // jac01 (Jacoby's Jungle): Platform/HighForest registered as a PaletteVariant (JunglePlatform)
        // + the base Jungle profile's own "always CornerEdgeResolver-reachable ungrouped tile" note
        // above, not this alternate-vocab bucket -- no terrain needs auto-tagging here.
        ["jac01"] = new(StringComparer.OrdinalIgnoreCase),
        // ttr01 (Rural Grass): GoodCastle/EvilCastle/Water are all registered as real PaletteVariant
        // profiles (RuralGrassGoodCastle/RuralGrassEvilCastle/RuralGrassWater), and Forest/GentleHill
        // never appear on any GROUP (verified directly), so every ungrouped tile carrying them was
        // already CornerEdgeResolver-reachable regardless of declared vocabulary. "Trees" IS needed
        // here: TILE60/TILE74 (Grass/Trees mixed, door-bearing, Stream/Road crosser respectively) fail
        // CornerEdgeResolver's admission gate regardless of vocab (a door-bearing mixed-terrain tile,
        // the same shape ttf01's own TILE849/1114 RuralTrees/RuralWater note documents) -- no
        // Trees-based composition is warranted (Trees carries no GROUP content beyond the
        // accent-terrain-only "Ship - Air, Above Trees" boat group, already separately exempt). See
        // BaseGameTilesetProfiles.RuralGrass's own doc comment.
        ["ttr01"] = new(StringComparer.OrdinalIgnoreCase) { "Trees" },
        // tts01 (Rural Winter*): the identical shape as ttr01 (verified directly against tts01's own
        // .set data, not assumed) -- GoodCastle/EvilCastle/Water are registered PaletteVariant profiles
        // (RuralWinterGoodCastle/RuralWinterEvilCastle/RuralWinterWater), and GentleHill never appears
        // on any GROUP, so those are already CornerEdgeResolver-reachable regardless of vocabulary.
        // "Trees" IS needed here for the same reason as ttr01: TILE60/TILE74 (Snow/Trees mixed,
        // door-bearing, Stream/Road crosser respectively -- the same TileIds as ttr01's own copy) fail
        // CornerEdgeResolver's admission gate regardless of vocab. See
        // BaseGameTilesetProfiles.RuralWinter's own doc comment.
        ["tts01"] = new(StringComparer.OrdinalIgnoreCase) { "Trees" },
        // tno01 (Castle Exterior, Rural*): "cliff"/"castlewall"/"keep"/"water" are all registered as
        // real profiles (base CastleExteriorRural + the CastleWall/Keep/Water PaletteVariants), so
        // ungrouped tiles carrying them were already CornerEdgeResolver-reachable regardless of this
        // bucket. "trees" IS needed: it carries no GROUP content at all (1 uniform tile, 0 grouped --
        // verified directly) and every ungrouped grass/trees/water blend tile (e.g. TILE14-19/25-47) is
        // doorless, so no district composition is warranted for it -- the same starved-minor-terrain
        // shape ttr01/tts01's own "Trees" entries document.
        ["tno01"] = new(StringComparer.OrdinalIgnoreCase) { "trees" },
        ["fcx01"] = new(StringComparer.OrdinalIgnoreCase),
        // tjsb0 (D20 Secret Base): a single Wall/Floor/lava split, no alternate district palette.
        ["tjsb0"] = new(StringComparer.OrdinalIgnoreCase),
        // tbx78 (D20 Modern Facility): a single Wall/facility split, no alternate district palette.
        ["tbx78"] = new(StringComparer.OrdinalIgnoreCase),
        // tqq01 (Complex laps storage): "Livingroom"/"Kitchen"/"Shop" are no longer here --
        // BaseGameTilesetProfiles.LabStorageLivingroom/LabStorageKitchen/LabStorageShop now register
        // each district's own group family as SetPieces (see BaseGameTilesetProfiles.LabStorage's own
        // doc comment for the census-vs-practice writeup: this bucket had ALREADY read 100% via
        // terrain-independent mechanisms even before this registration, so removing these entries
        // changes no coverage number -- it only stops the exemption dictionary from documenting a "gap"
        // that was never real).
        ["tqq01"] = new(StringComparer.OrdinalIgnoreCase),
        // udp2 (D20 Office Interiors UDP): "Service"/"Tiled"/"Office_Wood"/"Office_Alum"/"Foyer_L"/
        // "Foyer_U" are no longer here: BaseGameTilesetProfiles.OfficeInteriorsService/Tiled/OfficeWood/
        // OfficeAlum/FoyerL/FoyerU each declare PrimaryOpenTerrain(<district>), closing their full
        // simple-tile coverage via CornerEdgeResolver the same way every other PaletteVariant's own
        // open-terrain declaration does -- see BaseGameTilesetProfiles.OfficeInteriorsService's own doc
        // comment.
        ["udp2"] = new(StringComparer.OrdinalIgnoreCase),
        // [CEP] Dungeon (zde01): byte-identical tile data to tde01 (see BaseGameTilesetProfiles.
        // CepDungeon's own doc comment) -- Water/Sewer/Ice/Pit are covered the same way tde01's own
        // entry above is, by the CepDungeonWater/Sewer/Ice/Pit PaletteVariant profiles each declaring
        // AccentTerrain(<accent>).
        ["zde01"] = new(StringComparer.OrdinalIgnoreCase),
        // [CEP] City Interior 1 (zin01): Elven/Sigil are covered by their own PaletteVariant profiles
        // (CepCityInteriorElven/Sigil) declaring PrimaryOpenTerrain(<district>) -- see
        // BaseGameTilesetProfiles.CepCityInterior's own doc comment. Workshop is wired directly on the
        // base profile (no PrimaryOpenTerrain override needed).
        ["zin01"] = new(StringComparer.OrdinalIgnoreCase),
        // tcn01 (City Exterior*): Building/EvilCastle/GoodCastle (and each district's own
        // Field*/Gothic*/Sigil* equivalents) are composed only as ordinary SetPiece OBSTACLE terrain
        // (buildings/castle walls stamped as rooms/set pieces within the open Cobble-family street
        // space), never as this profile's Open/Secondary/Accent terrain -- so any tile/group touching
        // ONLY these plus the district's own Cobble is legitimately outside the wired vocabulary. This
        // closes 42 ungrouped door-bearing Cobble/Building (and FieldCobble/FieldBuilding,
        // GothicCobble/GothicBuilding, SigilCobble/SigilBuilding) boundary tiles -- a plain building
        // entrance door with no crosser at all, structurally the same "door-bearing mixed-terrain tile,
        // no CornerEdgeResolver/TileDoorPlanner mechanism applies" shape ttf01/ttr01's own RuralTrees/
        // RuralWater entries document -- plus "[City] Ship - Air, Above Buildings (3x1)" (all-Building
        // corners, the "Above Water"/"Docked" siblings' unwired cousin). SigilHill is Sigil's own
        // starved minor terrain (no GROUP touches it beyond the wired SigilCastle/SigilCobble/
        // SigilChasm trio). PADDING is the universal area-border fill terrain every onboarded tileset's
        // own entry already carries.
        ["tcn01"] = new(StringComparer.OrdinalIgnoreCase)
        {
            "Building", "EvilCastle", "GoodCastle",
            "FieldBuilding", "FieldEvilCastle", "FieldGoodCastle",
            "GothicBuilding", "GothicEvilCastle", "GothicGoodCastle",
            "SigilHill", "SigilBuilding",
            "PADDING",
        },
        // tti01 (Frozen Wastes*): no starved/unwired terrain remains -- Pit and Floor are both wired
        // (base profile's Solid/Open pair) and EvilCastle is wired too (FrozenWastesEvilCastle's own
        // Solid/Open pair) -- every terrain this tileset declares is composed by some profile sharing
        // its TilesetResref.
        ["tti01"] = new(StringComparer.OrdinalIgnoreCase),
        // ttz01 (Tropical*): "trees" is the identical starved, GROUP-free minor terrain RuralGrass's
        // own "Trees" already documents (1 pure tile, pathnode 'T', no GROUP anywhere touches it,
        // verified directly) -- grass/sand/water are all wired (base/Sand/Water/SandWater profiles).
        ["ttz01"] = new(StringComparer.OrdinalIgnoreCase) { "trees" },
        // ttu01 (Underdark*): Water is the wired AccentTerrain (base profile); Chasm is its unwired
        // sibling accent (the identical "Door - Bridge, <accent>"/"Ship - Air, Above <accent>" shape on
        // the other hazard-gap terrain -- see MinesAndCaverns' own Pit/Lava precedent). Drow/
        // Svirfneblin/Poor are minor per-building doorway-threshold terrains (one pure tile each) that
        // only ever appear on ten ungrouped, flat, door-bearing, CROSSER-FREE tiles -- TileResolver's
        // door-slot admission gate requires a crosser to credit a door at all, so these can never
        // structurally resolve regardless of vocabulary. See BaseGameTilesetProfiles.Underdark's own
        // doc comment.
        ["ttu01"] = new(StringComparer.OrdinalIgnoreCase) { "Chasm", "Drow", "Svirfneblin", "Poor" },
        // trs02 (Early Winter 2): Chasm is the wired SecondaryOpenTerrain (base profile); Mountain is
        // the wired SolidTerrainOverride on the EarlyWinterMountain variant. Grass2/Water/Trees stay
        // unwired this pass (time-boxed scope) -- see BaseGameTilesetProfiles.EarlyWinter's own doc
        // comment. "Dirt" is declared in the .set terrain palette but never appears on ANY tile corner
        // (verified directly, zero occurrences) -- included here defensively even though it can never
        // actually trigger.
        ["trs02"] = new(StringComparer.OrdinalIgnoreCase) { "Grass2", "Water", "Trees", "Dirt" },
        // tcm02 (Medieval City 2): "Building" fails every terrain pairing in the 16-combo matrix (2/16
        // or 8/16 against every other terrain, never 16/16) -- it cannot function as this tileset's
        // Solid, Open, or Secondary terrain under any composition, so it is composed only as an
        // ordinary decorative facade corner on house/shop/estate GROUPs. "Trees" and "Castle" are NOT
        // here: Trees never appears on any GROUP (only ungrouped grass/water blend tiles, already
        // CornerEdgeResolver-reachable regardless of vocab), and every Castle-cornered GROUP (CastleSmall
        // Door/CastleHugeGate/CastleTowerGate1-2/PrisonTower/CastleWell/CastleSmallDoor2/
        // CastleHugeGateGrass) classifies via IsExitGroupEligible/IsFeatureTileEligible's
        // terrain-agnostic rule -- see BaseGameTilesetProfiles.MedievalCity's own doc comment.
        ["tcm02"] = new(StringComparer.OrdinalIgnoreCase) { "Building" },
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
        // ttr01: Road/Slope/HighBridge are wired directly on the RuralGrass/RuralGrassWater profiles
        // (RoadCrosser/RampCrosser). Stream/Wall1/Wall2 have no dedicated wiring -- their doorless,
        // ungrouped tiles resolve via CornerEdgeResolver directly regardless, and the two door-bearing
        // "Wall - Road Gate, Rural 1/2" dual-crosser cells are handled via PilotExpectedExemptions, not
        // this bucket -- see BaseGameTilesetProfiles.RuralGrass's own doc comment.
        ["ttr01"] = new(StringComparer.OrdinalIgnoreCase),
        // tts01: Road/Slope are wired directly on the RuralWinter/RuralWinterWater profiles
        // (RoadCrosser/RampCrosser) -- no HighBridge crosser exists in this tileset at all. Stream/
        // Wall1/Wall2 have no dedicated wiring -- their doorless, ungrouped tiles resolve via
        // CornerEdgeResolver directly regardless, and the door-bearing "Wall - Road Gate, Winter 1/2"
        // dual-crosser cells (plus the new doorless "Wall - Over Stream, Winter 1/2" dual-crosser
        // cells) are handled via PilotExpectedExemptions, not this bucket -- see
        // BaseGameTilesetProfiles.RuralWinter's own doc comment.
        ["tts01"] = new(StringComparer.OrdinalIgnoreCase),
        // tno01: ridge/road are wired directly on the base CastleExteriorRural profile (RampCrosser/
        // RoadCrosser). stonewall/smallwall/sandbank/river/bridge have no dedicated crosser-slot wiring
        // -- their doorless, ungrouped tiles resolve via CornerEdgeResolver directly regardless, and the
        // door-bearing solo groups that use them (GrassLowWall_gate1/2, DirtLowWall_gate1/2,
        // CastleCrosser_Grass_Breach, Smallwall Break, Smallwall Stairs_Dirt/Grass) are wired as
        // SetPieces directly on the base profile. "lists"/"listssmall" carry no GROUP and no door-
        // bearing tile at all -- see BaseGameTilesetProfiles.CastleExteriorRural's own doc comment.
        ["tno01"] = new(StringComparer.OrdinalIgnoreCase),
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
        // udp2: "Door"/"Door_Garage_Sm"/"Door_Garage_Lg" are declared via DoorSlotCrossers, which (post
        // the "accept profile-declared door crossers in group classification" fix) generalizes GROUP
        // classification's IsDoorwayEdge the same way it always has for CornerEdgeResolver's ungrouped-
        // tile path. STALE-COMMENT UPDATE (re-probed 2026-07-16, dc9663ff6 entry-pair pass): the seven
        // district "*_Entry 2x1" pairs (Service/Tiled/Office_Vinyl/Office_Wood/Office_Alum/Foyer_L/
        // Foyer_U) described below as still landing here are WRONG -- they were closed by the mixed/
        // open-member interior-doorway tolerance (see LayoutGroupStamper.TryClassify's own doc comment)
        // in the same pass that produced this file's PilotExpectedExemptions entries for them; they are
        // wired as SetPieces (BaseGameTilesetProfiles.OfficeInteriors/OfficeInteriorsService etc.) and no
        // longer reach this fallback bucket at all. Only "Hallway1"/"Hallway2" remain here, for a THIRD,
        // freshly re-verified reason (probed directly, not merely inferred from IsAllowedMemberEdge):
        // declaring them as DoorSlotCrossers DOES make "Hallway1_Entry 2x1"/"Hallway2_Entry 2x1"
        // structurally classify as WallRoom (verified: census rises to 229/229 with the declaration
        // added) -- both are genuine all-Wall-cornered WallRoom shapes whose sole crosser edge (Doors=1
        // on the Wall-cornered member, a real door slot) faces the group's own perimeter, identical in
        // shape to tbx78's already-closed doorway1/2/3 family. But a real MeasureIsolatedGroupHits
        // placement probe (OpenSetPiecePlacementRateTests' own isolation technique, Halls layout, 150
        // seeds each) measured 0/150 for BOTH groups even once classifiable: this tileset has no
        // OpenLane boundary tile shape (SupportsWallRoomOpenLaneBoundary) supporting ANY WallRoom
        // perimeter attachment at all -- the SAME tileset-wide structural fact already documented on
        // BaseGameTilesetProfiles.OfficeInteriors for its other declared door crossers (SmRm1/SmRm2/
        // Elevator1/2/Stairwell_U/UD/D/Restrooms/Break_Room, also classify-but-never-place). Declaring
        // Hallway1/Hallway2 would only inject dead RNG draws (Stamp shuffles candidate anchors for every
        // classified group each seed, even ones that can never find a site) into every udp2 composition
        // for zero placed content -- per this project's established convention, NOT wired; the
        // exemption stays, now with a placement-rate proof instead of a stale classification-gap guess.
        // Coverage stays 225/229 (98.3%) -- genuinely, verifiably as good as this tileset gets short of
        // a room-size/boundary-shape engine change out of scope for a single-tileset pass.
        ["udp2"] = new(StringComparer.OrdinalIgnoreCase)
        {
            "Hallway1", "Hallway2",
        },
        // zde01: byte-identical crosser vocabulary to tde01 (Bridge/Corridor/Fence/Doorway/Ramp/
        // MazeMosaic) -- MazeMosaic is the same out-of-scope alternate family tde01 already carries.
        ["zde01"] = new(StringComparer.OrdinalIgnoreCase) { "MazeMosaic" },
        // zin01: Corridor/Doorway are canonical; Window/ElvenHallway/SigilHallway are all declared
        // (Window is resolver-covered on flat door-free tiles, ElvenHallway/SigilHallway via
        // DoorSlotCrossers on the Elven/Sigil variants) -- no alternates expected, pending the census run.
        ["zin01"] = new(StringComparer.OrdinalIgnoreCase),
        // tcn01 (City Exterior*): Wall/Stream/Alley have no wired body/port/road vocabulary this pass
        // -- see BaseGameTilesetProfiles.CityExterior's own doc comment for the full evidence writeup
        // (Alley is a Building-embedded back-alley crosser, Wall/Stream are property-line/canal
        // dividers through open Cobble, none of which any current mechanism recognizes for this
        // composition).
        ["tcn01"] = new(StringComparer.OrdinalIgnoreCase) { "Wall", "Stream", "Alley" },
        // tti01 (Frozen Wastes*): 0 crossers declared in the .set data at all -- there is no crosser
        // vocabulary of any kind (wired or otherwise) for this tileset to have an alternate family of.
        ["tti01"] = new(StringComparer.OrdinalIgnoreCase),
        // ttz01 (Tropical*): Wall1/Wall2/Stream/Road are all declared crossers with SOME wired role
        // (Road via RoadCrosser, the other three via the WallRoom-eligible-but-Tunnel-vocab-starved
        // gate/bridge groups below, handled as named PilotExpectedExemptions entries the same way
        // ttr01/tts01's own Wall1/Wall2/Stream are) -- no blanket alternate-vocab crosser bucket needed.
        ["ttz01"] = new(StringComparer.OrdinalIgnoreCase),
        // ttu01 (Underdark*): Wall is wired as RoadCrosser; Bridge is wired via AccentTerrain("Water")'s
        // CorridorInsert shape ("Door - Bridge, Water"). RuinWall's own gate family and "Door - Wall"
        // (both open-cornered 1x1 groups with a perimeter crosser edge -- no mechanism admits either
        // shape, see BaseGameTilesetProfiles.Underdark's own doc comment) are handled as named
        // PilotExpectedExemptions entries instead of a blanket bucket here. Stream/Slope touch no GROUP
        // at all -- every Stream/Slope-crossered tile is an ordinary ungrouped tile, already
        // CornerEdgeResolver-reachable regardless of vocabulary.
        ["ttu01"] = new(StringComparer.OrdinalIgnoreCase),
        // trs02 (Early Winter 2): Street is ALSO wired as RoadCrosser (a separate, real lane-carving
        // mechanism, orthogonal to GROUP classification) -- that does not stop it from also gating real
        // GROUP content with no declared DoorSlotCrosser this pass (StreetCave1-3/SmallCastle/
        // InnerCornerCave4/MageTower's "street" edges), the same as Wall/Ridge/Stream. "path" is a
        // fifth, rare crosser found on exactly one group member, CliffPath1's TILE582, not in the
        // tileset's own 4-crosser summary at all (verified directly). Matches ttr01/tts01/ttz01's own
        // identical Wall1/Wall2/Stream/Road gate-family precedent -- see
        // BaseGameTilesetProfiles.EarlyWinter's own doc comment.
        ["trs02"] = new(StringComparer.OrdinalIgnoreCase) { "Wall", "Ridge", "Stream", "Street", "path" },
        // tcm02 (Medieval City 2): "Wall" (Battlement*/CornerTower*/Drawbridge*/RiverWall1/Stable/
        // CliffWallCave) has no verified body/port/road vocabulary, and declaring it as a
        // DoorSlotCrosser does not help -- every carrier is a 1x1 group, and a non-Solid-cornered 1x1
        // group's own doorway-equivalent edge is always "perimeter" (no sibling member to be interior
        // toward), which ClassifyMultiTileSetPiece's mixed-shape tolerance explicitly rejects (verified
        // directly). "Stream" (streamWillow/Bridge1/Bridge2/CliffBridge1-2/CliffWillow), "Road"
        // (RuinedCart), and "Rock" (only ever paired with non-flat groups already auto height-exempt)
        // carry no wired vocabulary this pass either. "path" is a fifth, rare crosser found on exactly
        // one group member, CliffPath1's sole member tile, not in the tileset's own 5-crosser summary at
        // all (verified directly) -- the same rare-crosser quirk trs02's own "path" entry documents.
        ["tcm02"] = new(StringComparer.OrdinalIgnoreCase) { "Wall", "Stream", "Road", "Rock", "path" },
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
        // not a Solid/Open composition member). "Platform Cliff Dwellings 2x3" mixes Cliff+Pit+Platform
        // (three terrains -- no two-terrain classifier reaches it), the same residual as ttf01's own
        // "Platform - Cliff Section". "Platform Cliff Door" is NOT also exempt despite the same
        // Platform+Cliff mixed-corner shape ttf01's own "Platform - Cliff Door" has (STALE-COMMENT FIX,
        // re-verified directly, the identical bug class -- see BaseGameTilesetProfiles.Forest's own
        // doc comment): both are a flat, crosser-free, door-bearing 1x1 group, so both already satisfy
        // IsExitGroupEligible's vocab-independent structural rule regardless of their mixed corners.
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

        // Rural Grass (ttr01) -- see BaseGameTilesetProfiles.RuralGrass's own doc comment for the full
        // placement-honesty writeup. This tileset declares no canonical "Doorway"/"Corridor" crosser,
        // so Complex/Halls/Organic all downgrade Tunnel corridors to OpenLane -- and every group below
        // is WallRoom-classify-eligible ONLY (a kind that hangs off a Tunnel corridor's wall face,
        // which never carves here), verified via a direct isolated-placement probe at 0/100 across all
        // three layouts for every one of them. Registering them as SetPieces would be dead weight, so
        // none are wired -- they stay census-exempt here instead (structural classification is real;
        // real placement is not, and the project's placement-honesty convention treats that as an
        // exemption, not a closure). "Footbridge"/Stream and "Ruined Cart"/Road: solo, all-Grass, one
        // door-implying crosser edge. "Tower - Archer, Rural Wall 1/2" and their "... Corner" siblings
        // (four groups): solo, all-Grass, one Wall1/Wall2 edge. "Wall - Gate, Rural 1/2": solo,
        // all-Grass, one Wall1/Wall2 edge plus a real door. "Wall - Road Gate, Rural 1/2": the same
        // shape, PLUS a second independent Road edge -- the identical dual-crosser-crossroads gap as
        // jac01's WallGate01/02 and ttd01/ttf01's own WallGate/TrenchBridge families (doubly exempt:
        // WallRoom-eligible in principle but unplaceable regardless, same as its single-crosser
        // siblings). On the RuralGrassWater PaletteVariant: "Door - Bridge"/Road and "Door - Bridge,
        // High"/HighBridge (solo, all-Water, one door-implying crosser edge) and "Ship - Docked 2
        // (2x2)" (Water, one Road edge, one real member + three holes) are the identical WallRoom
        // ceiling over a Water solid instead of Grass. "Ship - Air, Above Trees (3x1)" stays exempt for
        // a DIFFERENT reason: a uniform all-Trees door-bearing group sitting purely on an AccentTerrain
        // no profile composes as Solid or Open (Trees carries no other GROUP content to justify a
        // dedicated composition) -- the accent-terrain-only-group gap jac01's CarrackD_4x1/
        // CaravelFloating_3x1 document (its all-Water sibling "Ship - Air, Above Water (3x1)" DOES
        // classify AND place for real: RuralGrassWater composes Water as a real Solid terrain, and
        // allCornersSolid + a real door on TILE573 satisfies WallAlcove -- a different, Tunnel-
        // independent mechanism -- regardless of any crosser; verified 100% isolated placement).
        ("ttr01", "GROUP:Footbridge"),
        ("ttr01", "GROUP:Ruined Cart"),
        ("ttr01", "GROUP:Tower - Archer, Rural Wall 1"),
        ("ttr01", "GROUP:Tower - Archer, Rural Wall 1 Corner"),
        ("ttr01", "GROUP:Tower - Archer, Rural Wall 2"),
        ("ttr01", "GROUP:Tower - Archer, Rural Wall 2 Corner"),
        ("ttr01", "GROUP:Wall - Gate, Rural 1"),
        ("ttr01", "GROUP:Wall - Gate, Rural 2"),
        ("ttr01", "GROUP:Wall - Road Gate, Rural 1"),
        ("ttr01", "GROUP:Wall - Road Gate, Rural 2"),
        ("ttr01", "GROUP:Door - Bridge"),
        ("ttr01", "GROUP:Door - Bridge, High"),
        ("ttr01", "GROUP:Ship - Air, Above Trees (3x1)"),
        // "Ship - Floating (2x1)"'s TileIds are [229, 179], and "Ship - Docked 2 (2x2)"'s are
        // [230, 180, -1, 179] -- TILE179/TILE180 are SHARED physical tiles whose OWN GroupIndex
        // resolves to "Ship - Docked 1 (2x2)" (claimed first, per TilesetSetParser's "whichever group
        // claims it FIRST" rule -- the same tdm01-precedent sharing shape this file's own
        // exemptedTileIds dedup comment documents) and are already Cover()'ed there (a genuine
        // OpenSetPiece, Grass+Water mixed corners). Registering the bare TILE229/TILE230 (not the whole
        // GROUP) avoids re-pulling in the already-covered TILE179/TILE180 under their own group names.
        // TILE229/TILE230 themselves (all-Water, no door on 229, one Road edge with no door on 230)
        // have no path: OpenSetPiece needs an Open corner, WallAlcove needs a door, WallRoom needs a
        // Doorway-equivalent edge (and even granting one, the same Tunnel-corridor-dependent WallRoom
        // ceiling this profile's own doc comment documents applies) -- none apply.
        ("ttr01", "TILE229"),
        ("ttr01", "TILE230"),

        // Rural Winter* (tts01) -- the winter reskin sibling of ttr01, verified directly against
        // tts01's own .set data (same TileIds for every shared shape). See
        // BaseGameTilesetProfiles.RuralWinter's own doc comment for the full group-inventory-delta
        // writeup. This tileset ALSO declares no canonical "Doorway"/"Corridor" crosser, so the
        // identical WallRoom-classify-eligible-but-Tunnel-vocab-starved shape applies: a direct
        // isolated-placement probe (ProbeTool, 100 seeds x Complex/Halls/Organic) measured 0/100 on
        // every one of the groups below. "Footbridge"/Stream and "Ruined Cart"/Road: solo, all-Snow,
        // one door-implying crosser edge. "Tower - Archer, Winter Wall 1/2" and their "... Corner"
        // siblings (four groups): solo, all-Snow, one Wall1/Wall2 edge. "Wall - Gate, Winter 1/2": solo,
        // all-Snow, one Wall1/Wall2 edge plus a real door. "Wall - Road Gate, Winter 1/2": the same
        // shape, PLUS a second independent Road edge -- the identical dual-crosser-crossroads gap
        // ttr01's own "Wall - Road Gate, Rural 1/2" documents. "Wall - Over Stream, Winter 1/2" is a
        // tts01-only addition with no ttr01 counterpart: the same dual-crosser shape again (Stream +
        // Wall1/Wall2 instead of Road + Wall1/Wall2), doorless this time -- doorless doesn't matter
        // (the Wall1/Wall2 crosser edge alone is what makes "Tower - Archer, Winter Wall 1/2" above
        // WallRoom-eligible too), so it is doubly exempt for the identical reason. On the
        // RuralWinterWater PaletteVariant: "Door - Bridge"/Road (solo, all-Water, one door-implying
        // crosser edge) is the identical WallRoom ceiling over a Water solid instead of Snow -- ttr01's
        // own "Door - Bridge, High"/HighBridge sibling has no tts01 counterpart at all (tts01 has no
        // HighBridge crosser), so there is no matching entry to carry over. "Ship - Air, Above Trees
        // (3x1)" stays exempt for the identical accent-terrain-only-group reason ttr01's own doc comment
        // gives (Trees carries no other GROUP content to justify a dedicated composition; the all-Water
        // sibling "Ship - Air, Above Water (3x1)" DOES classify AND place for real via WallAlcove, same
        // mechanism, verified 100% isolated placement). "Ship - Docked 2 (2x2)"'s own doc comment lives
        // with the TILE230 entry below, the same shape ttr01's own doc comment uses (no GROUP-level
        // entry needed -- see there).
        ("tts01", "GROUP:Footbridge"),
        ("tts01", "GROUP:Ruined Cart"),
        ("tts01", "GROUP:Tower - Archer, Winter Wall 1"),
        ("tts01", "GROUP:Tower - Archer, Winter Wall 1 Corner"),
        ("tts01", "GROUP:Tower - Archer, Winter Wall 2"),
        ("tts01", "GROUP:Tower - Archer, Winter Wall 2 Corner"),
        ("tts01", "GROUP:Wall - Gate, Winter 1"),
        ("tts01", "GROUP:Wall - Gate, Winter 2"),
        ("tts01", "GROUP:Wall - Road Gate, Winter 1"),
        ("tts01", "GROUP:Wall - Road Gate, Winter 2"),
        ("tts01", "GROUP:Wall - Over Stream, Winter 1"),
        ("tts01", "GROUP:Wall - Over Stream, Winter 2"),
        ("tts01", "GROUP:Door - Bridge"),
        ("tts01", "GROUP:Ship - Air, Above Trees (3x1)"),
        // "Ship - Floating (2x1)"'s TileIds are [229, 179], and "Ship - Docked 2 (2x2)"'s are
        // [230, 180, -1, 179] -- the identical shared-tile-id shape ttr01's own doc comment documents,
        // same TileIds, verified directly. TILE179/TILE180 are SHARED physical tiles already Cover()'ed
        // under "Ship - Docked 1 (2x2)" (claimed first). Registering the bare TILE229/TILE230 avoids
        // re-pulling the already-covered TILE179/TILE180 under their own group names; neither has a
        // path of its own (same reasoning as ttr01's own writeup).
        ("tts01", "TILE229"),
        ("tts01", "TILE230"),

        // Castle Exterior, Rural* (tno01) -- see BaseGameTilesetProfiles.CastleExteriorRural's own doc
        // comment for the composition/variant writeup. Six proof-backed exemption families:
        //
        // (1) The sandbank family: every group carrying a "sandbank" member edge
        // (Boat_cliff_Landed, Cave Sandbank Entry 1x1, cliff_caveentry_1x2, cliff_path1 -- BOTH
        // same-named copies share the same failing shape -- CliffPath_3x3, Shipwreck_clifs).
        // ClassifyMultiTileSetPiece rejects any member edge outside the doorway/body vocabulary
        // (IsAllowedMemberEdge), and "sandbank" is neither canonical nor declared: these are
        // cliff+grass shoreline paths, not placeable set pieces under any tno01 composition.
        ("tno01", "GROUP:Boat_cliff_Landed"),
        ("tno01", "GROUP:Cave Sandbank Entry 1x1"),
        ("tno01", "GROUP:cliff_caveentry_1x2"),
        ("tno01", "GROUP:cliff_path1"),
        ("tno01", "GROUP:CliffPath_3x3"),
        ("tno01", "GROUP:Shipwreck_clifs"),
        //
        // (2) Solo gates on non-tunnel crosser families (stonewall/smallwall/river/road) -- the
        // identical shape ttr01's own "Wall - Gate"/"Footbridge" exemptions document. An open-cornered
        // solo group carrying a non-tunnel crosser edge fails every classification branch
        // (CorridorInsert only splices Corridor/Alley/Fence/Bridge; the WallRoom/OpenSetPiece path
        // rejects the member edge). The two all-SOLID-cornered road gates (CliffRoad_gate all-cliff,
        // WaterRoad_gate all-water) WOULD classify WallRoom under a DoorSlotCrossers("road")
        // declaration -- measured directly under exactly that probe profile: 0/100 isolated on every
        // one of Complex/Halls/Organic for both (this tileset has no Tunnel vocabulary, so the wall
        // faces WallRoom needs never carve, and the OpenLane boundary fallback never corner-matches
        // them either) -- declared here with the rate proof instead of wired as dead RNG weight.
        ("tno01", "GROUP:CastleCrosser_Grass_Breach"),
        ("tno01", "GROUP:Smallwall Break"),
        ("tno01", "GROUP:Smallwall Stairs_Dirt"),
        ("tno01", "GROUP:Smallwall Stairs_Grass"),
        ("tno01", "GROUP:GrassLowWall_gate1"),
        ("tno01", "GROUP:GrassLowWall_gate2"),
        ("tno01", "GROUP:DirtLowWall_gate1"),
        ("tno01", "GROUP:DirtLowWall_gate2"),
        ("tno01", "GROUP:Footbridge_Dirt"),
        ("tno01", "GROUP:Footbridge_Grass"),
        ("tno01", "GROUP:CliffRoad_gate"),
        ("tno01", "GROUP:WaterRoad_gate"),
        //
        // (3) All-solid-cornered DOORLESS solo boats (Boat_cliff/Floating Island all-cliff pathnode
        // P, Boat_water all-water pathnode T): with neither a door (WallAlcove's trigger) nor a
        // doorway/body crosser (WallRoom/CorridorStub's) nor an open corner (OpenSetPiece's), no
        // classification branch applies -- the identical shape ttr01's own "Ship - Floating"
        // exemption documents.
        ("tno01", "GROUP:Boat_cliff"),
        ("tno01", "GROUP:Floating Island"),
        ("tno01", "GROUP:Boat_water"),
        //
        // (4) THREE-terrain castle gate groups (castlewall+dirt+grass on the same group):
        // LayoutGroupStamper's OpenSetPiece corner rule is a strict TWO-terrain match (solid+open, or
        // solid+secondary), so a three-terrain group fails classification under every tno01
        // composition -- the same "no mechanism models this" class as ttd01's own crossroads-gate
        // exemption. The four drawbridge pieces stack a fourth terrain (water or cliff) AND "road"
        // member edges on top of the same conflict.
        ("tno01", "GROUP:Castle Gate Walkable 2x1"),
        ("tno01", "GROUP:CastleGate2 2x1"),
        ("tno01", "GROUP:CastleWall Entrance"),
        ("tno01", "GROUP:CastleWall Entrance Walkable"),
        ("tno01", "GROUP:CastleWall4"),
        ("tno01", "GROUP:CastleWall4 Walkable"),
        ("tno01", "GROUP:Drawbridge 1x2"),
        ("tno01", "GROUP:Drawbridge_cliff_1x2"),
        ("tno01", "GROUP:drawbridge_passage"),
        ("tno01", "GROUP:drawbridge_passage_cliff"),
        //
        // (5) CaveWall2x1 (castlewall+cliff, doorless, crosser-free): cliff is a SOLID material in
        // this tileset's own base composition and is never walkable (pathnodes P/H/W/I) -- no
        // composition can cast it as the Open (or Secondary) side of the two-terrain OpenSetPiece
        // rule, and with both terrains being wall materials neither WallAlcove (no door) nor WallRoom
        // (no doorway edge) applies. A castle-wall-meets-rock adapter, not a placeable piece.
        ("tno01", "GROUP:CaveWall2x1"),
        //
        // (6) Ungrouped door-bearing tiles, two shapes -- the same genuinely-unreachable classes
        // ttd01's Svirfneblin/Poor door tiles and zin01's TILE541/551/846/879 document:
        //   (6a) 45 crosser-FREE door tiles on mixed corners (the keep-wall door family: keep+grass,
        //        keep+dirt, keep+castlewall, keep+cliff, keep+water blends, plus TILE625 dirt+water):
        //        TileResolver's admission gate excludes door-bearing crosser-free tiles (TileDoorPlanner's
        //        inventory instead), and TileDoorPlanner's single-Doorway-edge rule can never fire --
        //        tno01 declares no "Doorway" crosser anywhere.
        //   (6b) 31 door tiles carrying a non-door-implying crosser (smallwall/stonewall/river/road
        //        gate cells, incl. the road+stonewall crossroads TILE869/872/899): the crosser+door
        //        admission gate requires a Doorway/Bridge/declared-extra edge, and no DoorSlotCrossers
        //        vocabulary is declared (nor warranted -- the wall families never carve; see the
        //        GROUP-level road-gate 0/100 probe above).
        ("tno01", "TILE218"), ("tno01", "TILE228"), ("tno01", "TILE235"), ("tno01", "TILE241"),
        ("tno01", "TILE625"), ("tno01", "TILE675"), ("tno01", "TILE755"), ("tno01", "TILE785"),
        ("tno01", "TILE789"), ("tno01", "TILE791"), ("tno01", "TILE794"), ("tno01", "TILE799"),
        ("tno01", "TILE801"), ("tno01", "TILE803"), ("tno01", "TILE805"), ("tno01", "TILE810"),
        ("tno01", "TILE812"), ("tno01", "TILE818"), ("tno01", "TILE819"), ("tno01", "TILE820"),
        ("tno01", "TILE821"), ("tno01", "TILE822"), ("tno01", "TILE828"), ("tno01", "TILE829"),
        ("tno01", "TILE830"), ("tno01", "TILE832"), ("tno01", "TILE867"), ("tno01", "TILE868"),
        ("tno01", "TILE869"), ("tno01", "TILE872"), ("tno01", "TILE878"), ("tno01", "TILE899"),
        ("tno01", "TILE900"), ("tno01", "TILE901"), ("tno01", "TILE913"), ("tno01", "TILE915"),
        ("tno01", "TILE919"), ("tno01", "TILE920"), ("tno01", "TILE989"), ("tno01", "TILE1025"),
        ("tno01", "TILE1026"), ("tno01", "TILE1027"), ("tno01", "TILE1028"), ("tno01", "TILE1030"),
        ("tno01", "TILE1031"), ("tno01", "TILE1032"), ("tno01", "TILE1034"), ("tno01", "TILE1035"),
        ("tno01", "TILE1037"), ("tno01", "TILE1038"), ("tno01", "TILE1039"), ("tno01", "TILE1040"),
        ("tno01", "TILE1042"), ("tno01", "TILE1043"), ("tno01", "TILE1044"), ("tno01", "TILE1045"),
        ("tno01", "TILE1046"), ("tno01", "TILE1080"), ("tno01", "TILE1083"), ("tno01", "TILE1085"),
        ("tno01", "TILE1087"), ("tno01", "TILE1088"), ("tno01", "TILE1089"), ("tno01", "TILE1095"),
        ("tno01", "TILE1113"), ("tno01", "TILE1114"), ("tno01", "TILE1116"), ("tno01", "TILE1117"),
        ("tno01", "TILE1120"), ("tno01", "TILE1122"), ("tno01", "TILE1123"), ("tno01", "TILE1202"),
        ("tno01", "TILE1206"), ("tno01", "TILE1207"), ("tno01", "TILE1209"), ("tno01", "TILE1215"),

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

        // D20 Modern Facility (tbx78): LayoutGroupStamper's group classification (IsAllowedMemberEdge/
        // TryClassify's WallRoom rule) now reads MacroLayoutParameters.DoorSlotCrossers the same way
        // CornerEdgeResolver's ungrouped-tile path always has (see LayoutGroupStamper.IsDoorwayEdge),
        // so every "doorway1"/"doorway2"-crossered group here classifies as WallRoom now -- see
        // BaseGameTilesetProfiles.ModernFacility's own doc comment. "elevator" (TILE66/67) is CLOSED too
        // (no longer exempt here): TILE66 mixes Solid ("wall") and Open ("facility") corners on the SAME
        // tile while also carrying a "doorway2" edge, but that edge faces TILE67 -- its own group-mate,
        // an interior seam, never the group's own perimeter (verified directly against the raw .set
        // data) -- so it now falls through TryClassify's mixed/open-member tolerance and classifies as
        // SetPieceOpenSetPiece (see LayoutGroupStamper.TryClassify's own doc comment on that branch).
        // Coverage: 82/84 -> 84/84 (100%).

        // D20 Office Interiors UDP (udp2): the identical IsAllowedMemberEdge/DoorSlotCrossers gap as
        // tbx78 above, against the "Door" crosser name, is closed the same way -- Office_Vinyl_Entry/
        // SmRm1/SmRm2/MidRm1/MidRm2, Elevator1/2, Stairwell_U/UD/D, Restrooms, and Break_Room all now
        // classify as WallRoom (see BaseGameTilesetProfiles.OfficeInteriors' own doc comment) and are
        // wired: coverage rose from 156/229 to 193/229. The remaining ~36-tile gap (still auto-exempted
        // via PilotAlternateVocabCrossers["udp2"], test unaffected) is the six other district palettes
        // (Service/Tiled/Office_Wood/Office_Alum/Foyer_L/Foyer_U) no profile here composes with at all --
        // each needs its own PaletteVariant (mirroring zin01's Elven/Sigil pattern) before its
        // district-specific terrain/crosser vocabulary is even attempted, out of this pass's scope.
        //
        // District-closure follow-up: the six variant PaletteVariant profiles above close that ~36-tile
        // gap (see BaseGameTilesetProfiles.OfficeInteriorsService's own doc comment), leaving only each
        // district's own "*_Entry 2x1" pair (Service/Tiled/Office_Vinyl/Office_Wood/Office_Alum/Foyer_L/
        // Foyer_U, 14 tiles) plus the tileset-generic Hallway1_Entry/Hallway2_Entry (4 tiles) exempted --
        // the SAME mixed/open-member-with-interior-doorway-edge shape as tbx78's "elevator" above. The
        // seven district Entry pairs are now CLOSED (no longer exempt anywhere): each pairs an all-Wall
        // member with an open (district-terrain) member whose sole "Door" edge faces its own group-mate
        // -- interior, never perimeter (verified directly against every district's raw .set data) -- so
        // they now classify as SetPieceOpenSetPiece via the same mixed/open-member tolerance. See
        // BaseGameTilesetProfiles.OfficeInteriors/OfficeInteriorsService's own doc comments for the
        // SetPiece(...) wiring. Hallway1_Entry/Hallway2_Entry stay exempt (still in
        // PilotAlternateVocabCrossers["udp2"]'s "Hallway1"/"Hallway2" bucket), RE-PROBED 2026-07-16: this
        // was originally recorded as an IsAllowedMemberEdge declaration gap, but declaring Hallway1/
        // Hallway2 as DoorSlotCrossers DOES make both groups classify as WallRoom (verified: census rises
        // to 229/229) -- the real, empirically-measured reason they stay exempt is a placement-rate
        // impossibility instead: 0/150 seeds each in isolation (MeasureIsolatedGroupHits probe), because
        // this tileset has no OpenLane boundary shape supporting ANY WallRoom perimeter attachment (the
        // same tileset-wide fact already covering SmRm1/SmRm2/Elevator1/2/etc. above) -- see
        // PilotAlternateVocabCrossers["udp2"]'s own doc comment for the full writeup. Coverage: 211/229
        // (92.1%) -> 225/229 (98.3%), genuinely final for this tileset.

        // [CEP] City Interior 1 (zin01) -- see BaseGameTilesetProfiles.CepCityInterior's own doc
        // comment. RE-PROBED 2026-07-16 (100% closure campaign): "Window" is a genuine .set CROSSER
        // TYPE (CROSSER1, same section as Corridor/Doorway/ElvenHallway/SigilHallway) but was never
        // declared as a DoorSlotCrosser anywhere -- IsAllowedMemberEdge rejected any group carrying a
        // Window edge outright regardless of shape. Declaring it on the base profile
        // (BaseGameTilesetProfiles.CepCityInterior) closed the five "Room - <Type> <N>, Window (1x2)"
        // pairs (Living Room/Kitchen/Inn) and "[City] Window - Porthole 3" -- all six are all-Wall-
        // cornered WallRoom shapes whose Window edge sits on the group's true perimeter (a "window on
        // the far wall" pattern), the same allCornersSolid+hasAnyDoorway path any ordinary Doorway-
        // ported WallRoom already used. Placement proof:
        // OpenSetPiecePlacementRateTests.WindowCrosseredGroupsOnCepCityInterior_NowPlaceInIsolation (all
        // six clear 28-49% isolated, 150/150 successes). It ALSO closed TILE790/TILE881 for free: they
        // are the two members of "[Elven] Tree House - Grass, Window (3x3)" (Elven-variant-only, all
        // nine members all-Wall-cornered) that this census's cross-profile union check now classifies as
        // WallRoom via the BASE profile's vocabulary alone (WallRoom classification only needs
        // SolidTerrain corners + a recognized doorway-family edge -- it never checks PrimaryOpenTerrain,
        // so the base profile's Window declaration is sufficient even though this group is registered as
        // a SetPiece ONLY on the Elven variant). Verified this has ZERO runtime/RNG effect on the Elven
        // composition: LayoutGroupStamper.Stamp only ever iterates parameters.SetPieces.Keys for the
        // SPECIFIC profile in use, and the base profile never registers this Elven-only group name, so
        // it is never added to the base composition's stamp candidate list. A real isolated placement
        // probe against the ELVEN profile (with "Window" temporarily added to ITS OWN DoorSlotCrossers,
        // to check whether the group could ever place under its own composition) measured 0/150 -- a 3x3
        // footprint finds no legal WallRoom site in zin01's room-size envelope -- so "Window" is
        // deliberately NOT added to CepCityInteriorElven's own DoorSlotCrossers (that would only inject
        // dead RNG draws into every Elven-variant seed for zero placed content, per this project's
        // established convention); TILE790/881 get their census credit for free from the base profile's
        // declaration without paying that cost. Two Window-crossered groups still correctly stay exempt
        // ("[City] Window - Porthole 1/2" below) because they mix Window with the Corridor body crosser
        // on the same tile -- the identical hasAnyBodyCrosser-vs-hasAnyDoorway rejection
        // LayoutGroupStamper.TryClassify already applies everywhere else. "[City] Window - Home" also
        // stays exempt: mixed Wall/Home corners (not all-solid) with its sole Window edge on the
        // group's true 1x1 perimeter -- the same single-tile "no interior seam available" ceiling as
        // "[Sigil] Corridor - Entry" below. Coverage: 939/961 (97.7%) -> 952/961 (99.1%). The residual 9
        // fall into three genuine, verified gap classes:
        //
        // (1) The three remaining Window-crossered groups above (Home/Porthole 1/Porthole 2).
        ("zin01", "GROUP:[City] Window - Home"),
        ("zin01", "GROUP:[City] Window - Porthole 1"),
        ("zin01", "GROUP:[City] Window - Porthole 2"),
        //
        // (2) District-renamed hallway crossers (ElvenHallway/SigilHallway) on GROUPed tiles: declared
        // via DoorSlotCrossers on the Elven/Sigil variant profiles. LayoutGroupStamper's group
        // classification now reads MacroLayoutParameters.DoorSlotCrossers the same way
        // CornerEdgeResolver's ungrouped-tile path always has (see LayoutGroupStamper.IsDoorwayEdge), so
        // Elven's "Room - Round"/"Stairs" family and Sigil's "Corridor - Stairs Down/Up" family (every
        // ElvenHallway/SigilHallway-crossered GROUP either district has) now classify as WallRoom -- this
        // closes the whole category except the one below.
        // "[Sigil] Corridor - Entry" (TILE929) is the one tile where "SigilFloor" is ALSO used as a
        // crosser name (alongside SigilHallway) -- both ARE declared (CepCityInteriorSigil's
        // DoorSlotCrossers("SigilHallway", "SigilFloor")) and DO now reach the GROUP mechanism (a prior
        // pass's comment describing a "DoorSlotCrossers-doesn't-credit-GROUPs gap" here was stale --
        // that gap was closed tileset-wide by the "accept profile-declared door crossers in group
        // classification" fix). RE-PROBED 2026-07-16: the real, current reason is shape, not
        // declaration. TILE929's corners are Wall|SigilFloor|Wall|SigilFloor (mixed, not all-solid) with
        // BOTH edges (Right=SigilFloor, Left=SigilHallway) landing on its own 1x1 footprint's perimeter
        // -- there is no second group member for either edge to face instead, so neither can ever be the
        // INTERIOR-only shape the mixed/open-member tolerance requires (see
        // LayoutGroupStamper.TryClassify's own doc comment); a group with any doorway-family edge on the
        // true perimeter of a non-all-solid footprint is rejected outright before OpenSetPiece's corner-
        // match rule is ever tried. A single-tile group can structurally never supply the "interior seam"
        // this tolerance needs -- a genuine geometric ceiling, not a missing declaration. Kept exempt.
        ("zin01", "GROUP:[Sigil] Corridor - Entry"),
        //
        // (3) Workshop district has no PrimaryOpenTerrain declaration anywhere. RE-PROBED 2026-07-16
        // (direct .set audit of every Workshop-cornered tile, 37 rows): the OTHER Workshop-district
        // groups (Exit-Corner 1/2, Stairs Both/Down/Up) all carry a real door slot on an all-solid
        // footprint, so they classify as WallAlcove without ever needing an open-terrain match. "[Workshop]
        // Smithy" (TILE876: Wall|Wall|Workshop|Wall corners, no crosser, Doors=0) is the one Workshop
        // group with NEITHER a door NOR a crosser -- it falls to the OpenSetPiece corner-match rule
        // (not WallAlcove, which requires allCornersSolid; a prior pass's comment mislabeled this),
        // which needs its lone open corner to equal a declared PrimaryOpenTerrain, and none here
        // declares "Workshop" (only Inn/ElvenFloor/SigilFloor). A Workshop PaletteVariant IS
        // structurally viable -- the audit found 5 fully-open all-Workshop simple tiles (TILE850/868/
        // 877/890/897, all PathNode=A, no pathnode restriction) plus a real Doorway-crossered boundary
        // family (TILE851-863 etc., Workshop corners paired with a literal Doorway edge, canonical
        // vocabulary, no DoorSlotCrossers declaration needed) -- so a room built from this terrain could
        // both open fully and connect via ordinary doors. But the entire remaining Workshop-district
        // group inventory (Exit-Corner/Stairs) already classifies today without this variant, so the
        // ONLY thing a new PaletteVariant would buy is this one doorless decorative alcove -- not worth
        // the added profile (its own OnboardedTilesetPipelineTests/TunnelVocabularyCheckTests
        // registration, placement-rate proof, and layout-hash-pin exposure) for a single census tile,
        // per this project's established cost/benefit convention (mirrors the original judgment call,
        // now backed by the actual corner/pathnode inventory instead of an assumption). Kept exempt.
        ("zin01", "GROUP:[Workshop] Smithy"),
        //
        // (4) Four raw ungrouped tiles carrying a door slot but NO crosser at all, on a diagonal or
        // checkerboard-split corner pattern -- the same genuinely-unreachable shape as Barrows' TILE51
        // and Fort Interior's TILE125/127/128 (TileDoorPlanner's single-Doorway-edge rule requires a
        // real Doorway edge, which none of these four have). Verified directly: TILE541
        // (Wall|Wall|Wall|Kitchen, one door), TILE551/846/879 (checkerboard-alternating Kitchen/Wall or
        // Workshop/Wall corners, two doors each).
        ("zin01", "TILE541"),
        ("zin01", "TILE551"),
        ("zin01", "TILE846"),
        ("zin01", "TILE879"),
        //
        // tcn01 (City Exterior*): see BaseGameTilesetProfiles.CityExterior's own doc comment for the
        // full composition writeup. Four genuinely-unreachable shapes, none closed by any current
        // mechanism or by the alternate-vocabulary auto-tagging above:
        //
        // (1) "[Sigil] Final Area (7x7)" -- a 49-tile finale/boss-chamber set piece mixing THREE
        // terrains (SigilCobble open floor, SigilChasm accent pit, SigilCastle solid border).
        // ClassifyMultiTileSetPiece's OpenSetPiece rule is a strict two-terrain (Solid+Open or
        // Solid+Secondary) corner match with no Accent-terrain allowance -- the identical
        // three-terrain-group ceiling BaseGameTilesetProfiles.CastleExteriorRuralCastleWall's own
        // "CastleWall4/CastleGate2/Drawbridge" doc comment documents. Structurally excluded from every
        // composition; kept wired (SetPiece call safe regardless, per this codebase's "TryClassify
        // re-verifies independently" convention).
        ("tcn01", "GROUP:[Sigil] Final Area (7x7)"),
        //
        // (2) Docked-ship hull groups (City/Fieldstone/Gothic's own Dock-family crosser) mark a
        // continuous "keel line" of Dock/FieldDock/GothicDock crosser running the length of the hull --
        // BOTH an interior edge (shared between two real hull members) AND a perimeter edge (facing the
        // group's own boundary) carry the SAME crosser. ClassifyMultiTileSetPiece's CorridorStubChain
        // branch requires hasAnyPerimeterBodyCrosser with NO hasInteriorBodyCrosser at all (mirrors
        // LayoutGroupStamper.TryClassify's own rejection) -- verified directly against each group's real
        // tile-by-tile edge layout (ProbeTool) that every one of these hulls carries at least one
        // interior Dock-family edge, disqualifying the whole group regardless of TunnelCrossers choice.
        ("tcn01", "GROUP:[City] Ship - Small, Docked (2x2)"),
        ("tcn01", "GROUP:[City] Ship - Merchant, Docked (3x2)"),
        ("tcn01", "GROUP:[City] Ship - Weathered, Docked (3x2)"),
        // Also carries a "Road" edge on one member (TILE653, a genuine tileset-authoring quirk -- "Road"
        // is not a declared CROSSER TYPES entry at all) which independently fails
        // ClassifyMultiTileSetPiece's IsAllowedMemberEdge gate regardless of the keel-line issue above.
        ("tcn01", "GROUP:[City] Ship - Carrack, Docked (4x2)"),
        ("tcn01", "GROUP:[Fieldstone] Ship - Small, Docked (2x2)"),
        ("tcn01", "GROUP:[Gothic] Ship - Small, Docked (2x2)"),
        //
        // (3) All-Water-cornered, doorless, crosser-free hull/boat groups -- the identical shape
        // BaseGameTilesetProfiles.CastleExteriorRuralWater's own "Boat_water" doc comment documents: no
        // door means WallAlcove never triggers, no crosser means CorridorStubChain never triggers, and
        // no Open corner (every corner is Water==Solid under this composition) means OpenSetPiece never
        // triggers either -- genuinely no classification branch applies.
        ("tcn01", "GROUP:[City] Boat"),
        ("tcn01", "GROUP:[City] Ship - Small, Floating (1x2)"),
        ("tcn01", "GROUP:[City] Ship - Galleon 1 (5x1)"),
        ("tcn01", "GROUP:[City] Ship - Galleon 2 (5x1)"),
        ("tcn01", "GROUP:[City] Ship - Longship, Floating (3x2)"),
        ("tcn01", "GROUP:[City] Ship - Weathered, Undockable (3x1)"),
        ("tcn01", "GROUP:[Fieldstone] Boat"),
        ("tcn01", "GROUP:[Fieldstone] Ship - Small, Floating (1x2)"),
        ("tcn01", "GROUP:[Gothic] Boat"),
        ("tcn01", "GROUP:[Gothic] Ship - Small, Floating (1x2)"),
        //
        // (4) "Door - Bridge" (all three districts): Bridge/FieldBridge/GothicBridge are each an
        // independently-verified second real Tunnel body/port pair (TunnelVocabularyCheck confirmed
        // TRUE for all three, exactly like Dock/FieldDock/GothicDock -- see the base profile's own doc
        // comment), but a DungeonTilesetProfile carries only one Tunnel body/port slot, and Dock is
        // wired here (richer real content: it touches far more of the fleet than Bridge, which only
        // ever appears on this one boundary-port group per district). Bridge-crossered content is a
        // real, structurally-valid alternate vocabulary this pass doesn't also wire.
        ("tcn01", "GROUP:[City] Door - Bridge"),
        ("tcn01", "GROUP:[Fieldstone] Door - Bridge"),
        ("tcn01", "GROUP:[Gothic] Door - Bridge"),
        // City's OWN "Ship - Air, Above Buildings" alternate-vocab entry above already closes the
        // Building-cornered airship (auto-tagged); no manual entry needed for it here.

        // ttz01 (Tropical*), verified directly against PilotEveryTileIsReachableOrExplicitlyExempted's
        // own UNCLASSIFIED report (29 tiles across 12 GROUPs, all genuinely doorless/vocab-starved
        // shapes -- no wiring change closes any of them):
        //
        // (1) "Mysterious_Cave" (2x2, 4 members): the ONLY grass+sand mixed-terrain GROUP in this
        // tileset (2 members pure-grass, 2 members half-grass/half-sand) -- no composition among the
        // four onboarded profiles (grass/grass, sand/sand, water/grass, water/sand) ever pairs grass
        // and sand together, so this group always has at least one corner matching neither the
        // composition's Solid nor Open terrain. See Tropical's own doc comment.
        ("ttz01", "GROUP:Mysterious_Cave"),
        //
        // (2) "ShipDocked02_2x2"/"WeatheredDocked02_3x2"/"MerchantDocked02_3x2" (all-Water, TropicalWater):
        // each carries a "road" edge on at least one member (a dock plank meeting the shoreline road) --
        // the identical ClassifyMultiTileSetPiece.IsAllowedMemberEdge gate failure tcn01's own "[City]
        // Ship - Carrack, Docked (4x2)" entry documents (a road-crossered member with no wired Tunnel
        // body vocabulary this composition declares). MerchantDocked02_3x2 has a door on one member too,
        // but the edge gate rejects the whole group before the door ever matters.
        ("ttz01", "GROUP:ShipDocked02_2x2"),
        ("ttz01", "GROUP:WeatheredDocked02_3x2"),
        ("ttz01", "GROUP:MerchantDocked02_3x2"),
        //
        // (3) "ShipFloating_2x1"/"WeatheredFloating_3x1" (all-Water, doorless, crosser-free,
        // TropicalWater): the identical all-Solid-cornered-doorless gap tcn01's own "[City] Boat"/"Ship
        // - Small, Floating (1x2)" family documents -- no door means WallAlcove never triggers, no
        // crosser means CorridorStubChain never triggers, and no Open corner (every corner is
        // Water==Solid under this composition) means OpenSetPiece never triggers either.
        ("ttz01", "GROUP:ShipFloating_2x1"),
        ("ttz01", "GROUP:WeatheredFloating_3x1"),
        //
        // (4) "Footbridge"/"Ruined_Cart"/"Wall1Gate"/"Wall1GateRoad"/"Wall2Gate"/"Wall2GateRoad"
        // (all-grass, flat): the identical WallRoom-eligible-but-Tunnel-vocab-starved family ttr01/
        // tts01's own "Footbridge"/"Ruined Cart"/"Wall - Gate, Rural 1/2"/"Wall - Road Gate, Rural 1/2"
        // already document -- this tileset has no canonical Doorway/Corridor crosser at all (verified
        // directly, Complex downgrades to OpenLane unconditionally), so these Stream/Road/Wall1/Wall2
        // gate and bridge tiles never get a wall mass to hang a WallRoom off of. Wall1GateRoad/
        // Wall2GateRoad additionally carry TWO independent crosser families (Wall1/Wall2 AND Road) on
        // perpendicular edge pairs of the same tile -- the same "crossroads" shape ttd01's own
        // WallGate01/02 documents, an independent, second reason they can never classify.
        ("ttz01", "GROUP:Footbridge"),
        ("ttz01", "GROUP:Ruined_Cart"),
        ("ttz01", "GROUP:Wall1Gate"),
        ("ttz01", "GROUP:Wall1GateRoad"),
        ("ttz01", "GROUP:Wall2Gate"),
        ("ttz01", "GROUP:Wall2GateRoad"),
        //
        // (5) "Bridge_Door" (1x1, all-Water, TropicalWater): a door-bearing tile carrying a "road" edge
        // on two opposite sides -- the same "Door - Bridge" shape (4) above documents: this composition
        // never declares "road" as a Tunnel body/port crosser, so IsAllowedMemberEdge (single-tile
        // groups go through the same gate as ClassifySetPiece's own edge check) rejects it regardless
        // of the door.
        ("ttz01", "GROUP:Bridge_Door"),

        // ttu01 (Underdark*), verified directly against PilotEveryTileIsReachableOrExplicitlyExempted's
        // own UNCLASSIFIED report:
        //
        // (1) "Ship - Longboat, Docked"/"Ship - Drow Boat, Docked" (2x2, Water/mixed corners)/
        // "Dock (1x2)" (shares its two tiles with Ship - Longboat, Docked)/"Ship - Drow Boat (1x2)"
        // (1x2, all-Water): the identical "naval Docked piece with a bare Accent (Water) corner, no
        // Solid/Open corner, no crosser, no door" gap MinesAndCaverns' own "[Cave] Ship - Docked"
        // documents -- OpenSetPiece only ever matches Solid/Open corners, never a bare Accent terrain.
        ("ttu01", "GROUP:Ship - Longboat, Docked"),
        ("ttu01", "GROUP:Ship - Drow Boat, Docked"),
        ("ttu01", "GROUP:Dock (1x2)"),
        ("ttu01", "GROUP:Ship - Drow Boat (1x2)"),
        //
        // (2) "Ship - Air, Above Water (3x1)" (all-Water, doorless, crosser-free): the same bare-Accent
        // gap as (1) -- no Solid/Open corner means OpenSetPiece never triggers, no door means WallAlcove
        // never triggers, no crosser means CorridorStubChain never triggers.
        ("ttu01", "GROUP:Ship - Air, Above Water (3x1)"),
        //
        // (3) "Door - Wall" (1x1, all-Floor/Open corners, "Wall" edges on an opposite pair, 1 door) and
        // "Ruin - Gates"/"Ruin - House 5"/"Ruin - Entrance, Straight 1"/"Ruin - Entrance, Corner"/
        // "Ruin - Entrance, Straight 2" (1x1, all-Floor/Open corners, "RuinWall" edges, some door-bearing):
        // an OPEN-cornered 1x1 group with a perimeter crosser edge. LayoutGroupStamper's WallRoom and
        // CorridorStubChain both require ALL-SOLID corners for a door/body-crosser edge to count; the
        // mixed-shape doorway branch explicitly rejects any PERIMETER doorway-like edge (a 1x1 group's
        // edges are always perimeter, since it has no sibling member to be interior-facing toward) --
        // verified directly (declaring "Wall"/"RuinWall" as DoorSlotCrossers does not change the
        // outcome, it only proves the group reaches -- and is rejected by -- that exact branch). No
        // mechanism admits an open-ground gate/arch shape today. See BaseGameTilesetProfiles.Underdark's
        // own doc comment.
        ("ttu01", "GROUP:Door - Wall"),
        ("ttu01", "GROUP:Ruin - Gates"),
        ("ttu01", "GROUP:Ruin - House 5"),
        ("ttu01", "GROUP:Ruin - Entrance, Straight 1"),
        ("ttu01", "GROUP:Ruin - Entrance, Corner"),
        ("ttu01", "GROUP:Ruin - Entrance, Straight 2"),

        // tcm02 (Medieval City 2), verified directly against
        // PilotEveryTileIsReachableOrExplicitlyExempted's own UNCLASSIFIED report:
        //
        // (1) "Small_Cog" (2x1, all-Water, doorless, crosser-free): the same bare-Solid "naval piece
        // with no Open corner, no door, no crosser" gap tcn01's own "[City] Boat"/ttz01's own
        // "ShipFloating_2x1" family documents -- OpenSetPiece never triggers without an Open corner,
        // WallAlcove never triggers without a door.
        ("tcm02", "GROUP:Small_Cog"),
        //
        // (2) "CliffRockFormation" (2x2, all-Chasm, doorless, crosser-free): all corners equal
        // MedievalCityCliffs' own Solid (Chasm) with zero Open (Grass) corners present -- the identical
        // bare-Solid, no-door, no-crosser gap as (1), just on the Cliffs variant's own Solid terrain
        // instead of the base profile's Water.
        ("tcm02", "GROUP:CliffRockFormation"),
        //
        // (3) "CliffPond" (2x1, Chasm+Cobble, doorless, crosser-free): mixes Chasm with Cobble, a
        // terrain PAIR neither registered profile composes (base is Water/Cobble, Cliffs is
        // Chasm/Grass) -- every corner would need to match one of {Water, Cobble} or {Chasm, Grass},
        // and Chasm+Cobble matches neither pair.
        ("tcm02", "GROUP:CliffPond"),
        //
        // (4) "Grass_boat_docked" (1x2, Water+Grass, doorless, crosser-free), "DockedShip1x4_Grass"
        // (4x2, Water+Grass, 1 door, crosser-free), and "DockedShip1x3_Grass" (3x2, Water+Grass, 1
        // door, crosser-free): the identical unregistered-terrain-pair gap as (3) -- Water+Grass
        // matches neither the base profile's Water/Cobble pair nor the Cliffs variant's Chasm/Grass
        // pair.
        ("tcm02", "GROUP:Grass_boat_docked"),
        ("tcm02", "GROUP:DockedShip1x4_Grass"),
        ("tcm02", "GROUP:DockedShip1x3_Grass"),
        //
        // (5) "Willow1" (1x1, Grass+Water corners, pathNode B, doorless, crosser-free): fails
        // IsFeatureTileEligible (pathNode is not 'A') and the same unregistered Water+Grass pair as (4)
        // blocks OpenSetPiece.
        ("tcm02", "GROUP:Willow1"),
        //
        // (6) "Pub2x1" (1x2, Cobble+Grass, 2 doors, crosser-free): mixes Cobble with Grass, a terrain
        // pair neither registered profile composes (base is Water/Cobble; Cliffs is Chasm/Grass) --
        // Cobble+Grass matches neither pair, and the door-bearing WallAlcove path requires ALL corners
        // equal Solid, which this mixed footprint never satisfies either.
        ("tcm02", "GROUP:Pub2x1"),
        //
        // (7) 17 ungrouped Castle+Cobble / Castle+Grass boundary tiles (TILE1182/1188/1192/1193/1195/
        // 1196/1199/1220/1223/1276/1277/1279/1280/1283/1301/1302/1305), each a single physical door
        // slot with NO crosser edge at all (all four edges blank): the identical "plain building
        // entrance door with no crosser, no CornerEdgeResolver/TileDoorPlanner mechanism applies" shape
        // tcn01's own 42-tile Cobble/Building boundary-door bucket and ttf01/ttr01's own RuralTrees/
        // RuralWater entries document -- IsCornerEdgeResolverReachable's own "door implies a crosser"
        // guard excludes a crosser-free door tile outright, and IsDoorTransitionReachable requires a
        // genuine Doorway edge (never present here) rather than a bare physical door slot.
        ("tcm02", "TILE1182"),
        ("tcm02", "TILE1188"),
        ("tcm02", "TILE1192"),
        ("tcm02", "TILE1193"),
        ("tcm02", "TILE1195"),
        ("tcm02", "TILE1196"),
        ("tcm02", "TILE1199"),
        ("tcm02", "TILE1220"),
        ("tcm02", "TILE1223"),
        ("tcm02", "TILE1276"),
        ("tcm02", "TILE1277"),
        ("tcm02", "TILE1279"),
        ("tcm02", "TILE1280"),
        ("tcm02", "TILE1283"),
        ("tcm02", "TILE1301"),
        ("tcm02", "TILE1302"),
        ("tcm02", "TILE1305"),
    };

    public static IEnumerable<string> PilotTilesetKeys => new[]
    {
        "tdc01", "tde01", "tin01",
        "tbw01", "tdm01", "tdr01", "tic01", "tni02", "tid01", "tii01", "tni01", "tsw01", "twc03",
        "ttd01", "ttf01", "ttf02",
        "jac01",
        "ttr01",
        "tts01",
        "tno01",
        "fcx01",
        "tjsb0", "tbx78", "tqq01", "udp2",
        "zde01",
        "zin01",
        "tcn01",
        "tti01",
        "ttz01",
        "ttu01",
        "trs02",
        "tcm02",
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
            "ttr01" => BaseGameTilesetProfiles.RuralGrass,
            "tts01" => BaseGameTilesetProfiles.RuralWinter,
            "tno01" => BaseGameTilesetProfiles.CastleExteriorRural,
            "fcx01" => BaseGameTilesetProfiles.FutCity,
            "tjsb0" => BaseGameTilesetProfiles.SecretBase,
            "tbx78" => BaseGameTilesetProfiles.ModernFacility,
            "tqq01" => BaseGameTilesetProfiles.LabStorage,
            "udp2" => BaseGameTilesetProfiles.OfficeInteriors,
            "zde01" => BaseGameTilesetProfiles.CepDungeon,
            "zin01" => BaseGameTilesetProfiles.CepCityInterior,
            "tcn01" => BaseGameTilesetProfiles.CityExterior,
            "tti01" => BaseGameTilesetProfiles.FrozenWastes,
            "ttz01" => BaseGameTilesetProfiles.Tropical,
            "ttu01" => BaseGameTilesetProfiles.Underdark,
            "trs02" => BaseGameTilesetProfiles.EarlyWinter,
            "tcm02" => BaseGameTilesetProfiles.MedievalCity,
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

        // Diagnostic detail for genuinely-unaccounted tiles: prints one shape line per UNCLASSIFIED
        // entry (corners/edges/heights/doors/owning group) so a failing onboarding pass can read the
        // gap list straight out of the test output instead of re-deriving it with an offline probe.
        foreach (var e in exemptions.Where(e => e.Reason == "UNCLASSIFIED"))
        {
            var idText = e.TileOrGroup.Substring(4);
            var spaceIdx = idText.IndexOf(' ');
            var unclassifiedId = int.Parse(spaceIdx < 0 ? idText : idText.Substring(0, spaceIdx));
            var t = model.Tiles[unclassifiedId];
            var grpName = t.GroupIndex >= 0 ? model.Groups[t.GroupIndex].Name : "(ungrouped)";
            TestContext.WriteLine(
                $"  UNCLASSIFIED TILE{unclassifiedId}: corners=[{string.Join(",", t.Corners)}] edges=[{string.Join(",", t.Edges)}] heights=[{string.Join(",", t.CornerHeights)}] doors={t.Doors.Count} pathNode={t.PathNode} group='{grpName}'");
        }

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

        // Bare "TILE{n}" manual exemptions (e.g. Barrows' TILE13/TILE51, Fort Interior's plain door+
        // crosser tiles): honored for a flat, non-alternate-vocab tile -- mirrors the group-keyed
        // reconstruction above so these gaps get the same EXACT, no-silent-drift guarantee. Usually an
        // ungrouped (GroupIndex == -1) tile, but a GROUPed tile can also carry a bare "TILE{n}" entry
        // (e.g. zin01's TILE790/TILE881, two members of a 9-tile GROUP whose group-level classification
        // fails and falls through to independent per-tile probing -- see this file's own per-tile
        // fallback loop) as long as its own GROUP isn't ALSO separately exempted (which would already
        // reconstruct it above with the "(group '...')" suffix, and the runtime Exempt() call would
        // never reach the bare-tile path for it).
        foreach (var tile in model.Tiles)
        {
            if (tile.GroupIndex != -1 && PilotExpectedExemptions.Contains((tilesetResref, "GROUP:" + model.Groups[tile.GroupIndex].Name)))
                continue;
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
