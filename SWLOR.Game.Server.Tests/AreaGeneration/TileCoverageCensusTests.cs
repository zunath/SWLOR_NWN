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
    }

    private static TilesetVocabulary BuildVocabulary(TilesetModel model, DungeonTilesetProfile profile)
    {
        return new TilesetVocabulary
        {
            Solid = model.DefaultTerrain,
            Open = string.IsNullOrEmpty(profile.PrimaryOpenTerrain) ? model.FloorTerrain : profile.PrimaryOpenTerrain,
            Secondary = profile.SecondaryOpenTerrain ?? string.Empty,
            Accent = profile.AccentTerrain ?? string.Empty,
            Channel = !string.IsNullOrEmpty(profile.ChannelTerrain) ? profile.ChannelTerrain : (profile.AccentTerrain ?? string.Empty),
        };
    }

    // ---------------- ungrouped tile mechanisms ----------------

    private static bool IsDoorway(string edge) => Eq(edge, DoorwayCrosser);
    private static bool IsBridge(string edge) => Eq(edge, BridgeCrosser);

    /// <summary>Mirrors TileResolver.BuildCandidateLookup's registration rule for a single tile: flat,
    /// ungrouped, and either crosser-free, or crosser-bearing with any door slot facing a Doorway/Bridge
    /// edge. Uses the real public TileResolver.HasCandidate hook so this is checking actual production
    /// behavior, not a re-guessed copy of it.</summary>
    private static bool IsCornerEdgeResolverReachable(TilesetModel model, TileRecord tile)
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
                if (!hasDoorwayEdge && !hasBridgeEdge) continue;
            }

            if (TileResolver.HasCandidate(model, tl, tr, br, bl, top, right, bottom, left))
                return true;
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

        foreach (var crosser in new[] { CorridorCrosser, AlleyCrosser, FenceCrosser, BridgeCrosser })
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
            if (!IsDoorway(edge)) { doorwayOnly = false; break; }
            hasDoorway[slot] = true;
        }
        if (!doorwayOnly) return false;

        var isVerticalPair = hasDoorway[EdgeSlot.Top] && hasDoorway[EdgeSlot.Bottom] && !hasDoorway[EdgeSlot.Left] && !hasDoorway[EdgeSlot.Right];
        var isHorizontalPair = hasDoorway[EdgeSlot.Left] && hasDoorway[EdgeSlot.Right] && !hasDoorway[EdgeSlot.Top] && !hasDoorway[EdgeSlot.Bottom];
        if (!isVerticalPair && !isHorizontalPair) return false;

        return HasCorridorDoorwayAdapter(model, vocab);
    }

    /// <summary>True when the tileset carries at least one flat, all-solid-corner tile with exactly
    /// one Corridor edge and its opposite edge Doorway (the other two blank) -- mirrors
    /// LayoutGroupStamper.HasCorridorDoorwayAdapter.</summary>
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
                if (Eq(edge, CorridorCrosser)) corridorSlot = slot;
                else if (IsDoorway(edge)) doorwaySlot = slot;
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

        foreach (var crosser in new[] { CorridorCrosser, AlleyCrosser })
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
    /// mirroring LayoutGroupStamper.TryClassify's hole handling.</summary>
    private static GroupMechanism ClassifyMultiTileSetPiece(TilesetModel model, TileGroupRecord group, TilesetVocabulary vocab)
    {
        if (group.Rows <= 0 || group.Columns <= 0) return GroupMechanism.None;
        if (group.TileIds.Count != group.Rows * group.Columns) return GroupMechanism.None;

        var members = new List<TileRecord>();
        foreach (var tileId in group.TileIds)
        {
            if (tileId < 0) continue; // hole
            if (tileId >= model.Tiles.Count) return GroupMechanism.None; // out of range -- bad data
            var tile = model.Tiles[tileId];
            if (!IsFlat(tile)) return GroupMechanism.None;
            foreach (var edge in tile.Edges)
            {
                if (!string.IsNullOrEmpty(edge) && !Eq(edge, DoorwayCrosser)) return GroupMechanism.None;
            }
            members.Add(tile);
        }
        if (members.Count == 0) return GroupMechanism.None; // an all-hole "group" is degenerate

        var hasAnyDoorway = members.Any(m => m.Edges.Any(e => Eq(e, DoorwayCrosser)));
        var allCornersSolid = members.All(m => m.Corners.All(c => Eq(c, vocab.Solid)));
        var hasAnyDoor = members.Any(m => m.Doors.Count != 0);

        if (hasAnyDoorway)
        {
            if (!allCornersSolid || hasAnyDoor) return GroupMechanism.None;
            // At least one Doorway edge must be a perimeter opening (face outward) -- true for every
            // real WallRoom shape in the verified inventory (a fully interior Doorway edge, shared
            // between two members, would be unusual and isn't relied on here).
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

    private static GroupMechanism ClassifySetPiece(TilesetModel model, TileGroupRecord group, TilesetVocabulary vocab)
    {
        if (group.Rows == 1 && group.Columns == 1 && group.TileIds.Count == 1)
        {
            var soloTileId = group.TileIds[0];
            if (soloTileId >= 0 && soloTileId < model.Tiles.Count)
            {
                var soloTile = model.Tiles[soloTileId];
                if (IsCorridorInsertEligible(model, soloTile, vocab)) return GroupMechanism.SetPieceCorridorInsert;
                if (IsCorridorStubEligible(soloTile, vocab)) return GroupMechanism.SetPieceCorridorStub;
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
            else mechanism = ClassifySetPiece(model, group, vocab);

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

            if (IsCornerEdgeResolverReachable(model, tile)) { Cover(tileId, "CornerEdgeResolver"); continue; }
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
        ["tde01"] = new(StringComparer.OrdinalIgnoreCase) { "Water", "Sewer", "Ice", "Pit" },
        ["tin01"] = new(StringComparer.OrdinalIgnoreCase),
    };

    /// <summary>
    /// Per-tileset crosser names outside the shared layout carvers' canonical Doorway/Corridor/
    /// Alley/Fence/Bridge vocabulary (e.g. tdc01's GreyCorridor/DwarvenDoorway/DwarvenCorridor/
    /// ChultDoorway/ChultCorridor district-junction crossers, tde01's MazeMosaic). A group/tile whose
    /// non-blank edges are ALL among these is auto-exempted the same way as PilotAlternateVocabTerrains.
    /// </summary>
    private static readonly Dictionary<string, HashSet<string>> PilotAlternateVocabCrossers = new(StringComparer.OrdinalIgnoreCase)
    {
        ["tdc01"] = new(StringComparer.OrdinalIgnoreCase) { "GreyCorridor", "DwarvenDoorway", "DwarvenCorridor", "ChultDoorway", "ChultCorridor" },
        ["tde01"] = new(StringComparer.OrdinalIgnoreCase) { "MazeMosaic" },
        ["tin01"] = new(StringComparer.OrdinalIgnoreCase),
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
    /// themselves. Currently just tin01's five "*Room01_1x2"/"*Room02_1x2" door-entrance pairs (see
    /// the doc comment on BaseGameTilesetProfiles.CityInterior) -- each pairs a blank wall tile with a
    /// tile carrying BOTH a Doorway edge crosser AND a door slot on the same tile, which
    /// LayoutGroupStamper's WallRoom classification excludes (requires no door slot) and which isn't a
    /// trivial 1x1 group either (so the door-transition tolerance doesn't apply).
    /// </summary>
    private static readonly HashSet<(string Tileset, string Label)> PilotExpectedExemptions = new()
    {
        ("tin01", "GROUP:Livingroom01_1x2"),
        ("tin01", "GROUP:Livingroom02_1x2"),
        ("tin01", "GROUP:KitchenRoom01_1x2"),
        ("tin01", "GROUP:KitchenRoom02_1x2"),
        ("tin01", "GROUP:InnRoom01_1x2"),
        ("tin01", "GROUP:InnRoom02_1x2"),
        ("tin01", "GROUP:ShopRoom01_1x2"),
        ("tin01", "GROUP:ShopRoom02_1x2"),
        ("tin01", "GROUP:Bordello"),
    };

    public static IEnumerable<string> PilotTilesetKeys => new[] { "tdc01", "tde01", "tin01" };

    [TestCaseSource(nameof(PilotTilesetKeys))]
    public void PilotEveryTileIsReachableOrExplicitlyExempted(string tilesetResref)
    {
        var model = LoadTileset(tilesetResref);
        var profileKey = tilesetResref switch
        {
            "tdc01" => BaseGameTilesetProfiles.Crypt,
            "tde01" => BaseGameTilesetProfiles.Dungeon,
            "tin01" => BaseGameTilesetProfiles.CityInterior,
            _ => throw new ArgumentOutOfRangeException(nameof(tilesetResref))
        };
        var profile = new BaseGameTilesetProfiles().BuildTilesetProfiles()[profileKey];
        var vocab = BuildVocabulary(model, profile);

        var coveredTileIds = new HashSet<int>();
        var mechanismCounts = new Dictionary<string, int>();
        var exemptions = new List<Exemption>();

        void Cover(int tileId, string mechanism)
        {
            coveredTileIds.Add(tileId);
            mechanismCounts[mechanism] = mechanismCounts.GetValueOrDefault(mechanism) + 1;
        }

        void Exempt(int tileId, string label, string reason)
        {
            exemptions.Add(new Exemption { Tileset = tilesetResref, TileOrGroup = label, Reason = reason });
        }

        foreach (var group in model.Groups)
        {
            var members = group.TileIds.Where(id => id >= 0 && id < model.Tiles.Count).Select(id => model.Tiles[id]).ToList();
            if (members.Count == 0) continue;
            var memberIds = members.Select(m => m.TileId).ToList();

            var mechanism = GroupMechanism.None;
            if (IsFeatureTileEligible(model, group)) mechanism = GroupMechanism.FeatureTile;
            else if (IsExitGroupEligible(model, group)) mechanism = GroupMechanism.ExitGroup;
            else mechanism = ClassifySetPiece(model, group, vocab);

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
            if (exemptions.Any(e => e.TileOrGroup.StartsWith($"TILE{tileId} ") || e.TileOrGroup == $"TILE{tileId}")) continue;

            var tile = model.Tiles[tileId];

            if (IsCornerEdgeResolverReachable(model, tile)) { Cover(tileId, "CornerEdgeResolver"); continue; }
            if (IsDoorTransitionReachable(model, tile)) { Cover(tileId, "DoorTransition"); continue; }

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

        manualExemptionLabels.Should().BeEquivalentTo(expectedManualLabels,
            $"the {tilesetResref} manually-curated pilot exemption set must be EXACT -- any drift must be visible here, not silently absorbed");

        (coveredTileIds.Count + exemptions.Count).Should().Be(model.Tiles.Count);
    }
}
