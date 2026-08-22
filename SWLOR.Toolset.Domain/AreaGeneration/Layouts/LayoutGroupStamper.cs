#nullable disable
using System;
using System.Collections.Generic;
using System.Linq;
using SWLOR.Toolset.Domain.AreaGeneration.Tileset;

namespace SWLOR.Toolset.Domain.AreaGeneration.Layouts
{
    /// <summary>
    /// Stamps pre-designed tileset "group" set pieces (multi-tile, and 1x1) into a macro layout: wall-
    /// bounded rooms hanging off Tunnel-mode corridors ("WallRoom", e.g. zsf01's Cell/Bedroom),
    /// floor-level decorative pieces dropped into open room interiors ("OpenSetPiece", e.g. vmr01's
    /// Amphitheater), and 1x1 doorway tiles substituted into a straight Tunnel-mode corridor segment
    /// ("CorridorInsert", e.g. tdt01/tds01 BigDoor01/02). Runs in MacroLayoutGenerator after
    /// AssignTransitions (transitions are already anchored to open room tiles, so set pieces can avoid
    /// them) and before ValidateInvariants (so a bad stamp fails loudly instead of silently corrupting
    /// a layout).
    ///
    /// Every candidate site is fully validated before any grid data is written; a group that can't
    /// find a legal site for a given instance is simply skipped (0..maxCount placed for that group)
    /// rather than ever leaving the layout in a bad state or failing generation outright.
    ///
    /// Orientation is always 0 (v1: no group rotation). TileGroupRecord.TileIds is row-major with row
    /// 0 the group's southernmost row and column 0 its westernmost column at orientation 0 — identical
    /// to ResolvedLayout.Tiles' own bottom-up row-major indexing. Pinned empirically against
    /// czs220_maintlvl's placed "Bedroom" group (zsf01 TILE69/70, Rows=2 Columns=1, TileIds=[70,69]):
    /// at orientation 2 (180 degrees) TILE69 (row 1) sits south and TILE70 (row 0) sits north — the
    /// unrotated row0=south is flipped to north by the half turn — and at orientation 1 (90 degrees
    /// CCW) TILE69 (row 1, unrotated north) lands west while TILE70 (row 0, unrotated south) lands
    /// east, matching the engine's standard CCW90 rotation (north -> west) already documented on
    /// TileRecord.GetCornerAt. If this pin ever needs to change, fix ONLY the anchor/footprint math
    /// below — nothing else in this file assumes a direction.
    /// </summary>
    internal static class LayoutGroupStamper
    {
        private const string DoorwayCrosser = "Doorway";
        private const string CorridorCrosser = "Corridor";
        private const string FenceCrosser = "Fence";
        private const string AlleyCrosser = "Alley";
        private const string BridgeCrosser = "Bridge";

        /// <summary>
        /// Crosser names TryClassifyCorridorInsert checks a 1x1 group's tile against, in priority
        /// order. Corridor/Alley/Custom-body inserts (BigDoor01/02, BigDoorAlley, tdc01 "[Grey] Door -
        /// Big 1/2") sit on fully solid corners, the same wall-embedded tunnel body chain LayoutTunnelCarver
        /// carves for the composed CorridorCrosserType; Fence inserts (FenceDoor01/02, Interior/
        /// ExteriorFenceDoor) sit on this layout's own open terrain, matching LayoutFenceCarver's
        /// fully-open fence run; Bridge inserts (BridgeDoor/BridgeDoor01) sit on the accent/channel
        /// terrain, splicing into a LayoutAccentChannelCarver span (see TryClassifyCorridorInsert's
        /// accent-terrain resolution). The Custom-mode body crosser (see
        /// MacroLayoutParameters.TunnelBodyCrosser) is appended when configured -- see
        /// CorridorInsertCrossersFor -- so a tileset-declared alternate body family (e.g. "GreyCorridor")
        /// is tried alongside the two hardcoded canonical names, never in place of them (Fence/Bridge
        /// inserts remain available even when Custom mode is active).
        /// </summary>
        private static readonly string[] CorridorInsertCrossers = { CorridorCrosser, AlleyCrosser, FenceCrosser, BridgeCrosser };

        /// <summary>
        /// Crosser names TryPlaceCorridorStub extends an existing Tunnel-mode chain with, in priority
        /// order. Matches a dead-end (single-edge, not an opposite pair) wall-embedded set piece —
        /// e.g. tdt01 StairsDown01/StairsUp01, tds01 StairsDown/StairsUp, vmr01
        /// InteriorStairsDown/InteriorStairsUp/ExteriorStairsDown/ExteriorStairsUp, tdc01 "[Grey] Stairs
        /// - Down/Up" — onto an all-solid cell adjacent to an existing Corridor/Alley/Custom-body chain
        /// cell. See CorridorStubCrossersFor for how the Custom-mode body crosser is appended.
        /// </summary>
        private static readonly string[] CorridorStubCrossers = { CorridorCrosser, AlleyCrosser };

        /// <summary>
        /// Effective CorridorInsertCrossers for this composition: the two hardcoded canonical names
        /// plus, when the layout is composed with a Custom-mode tileset-declared body crosser (see
        /// MacroLayoutParameters.TunnelBodyCrosser), that name too -- so a district-scoped alternate
        /// corridor family (e.g. tdc01's "GreyCorridor", tdm01's "DesertCorridor"/"OrganicCorridor")
        /// gets the same CorridorInsert/CorridorStub set-piece treatment the canonical Corridor family
        /// already has, without hardcoding any specific tileset's naming.
        /// </summary>
        private static string[] CorridorInsertCrossersFor(MacroLayoutParameters parameters)
        {
            return parameters.CorridorCrosserType == CorridorCrosserType.Custom &&
                   !string.IsNullOrEmpty(parameters.TunnelBodyCrosser)
                ? new[] { CorridorCrosser, AlleyCrosser, FenceCrosser, BridgeCrosser, parameters.TunnelBodyCrosser }
                : CorridorInsertCrossers;
        }

        /// <summary>See <see cref="CorridorInsertCrossersFor"/>; the CorridorStub analogue.</summary>
        private static string[] CorridorStubCrossersFor(MacroLayoutParameters parameters)
        {
            return parameters.CorridorCrosserType == CorridorCrosserType.Custom &&
                   !string.IsNullOrEmpty(parameters.TunnelBodyCrosser)
                ? new[] { CorridorCrosser, AlleyCrosser, parameters.TunnelBodyCrosser }
                : CorridorStubCrossers;
        }

        // Slot -> (Dx, Dy) step to the neighboring cell across that edge. Matches EdgeSlot's
        // Top=0/Right=1/Bottom=2/Left=3 ordering and the "Top is the +Y (north) side" convention
        // documented on TilesetModel.TileRecord — also the convention pinned above for group rows.
        private static readonly (int Dx, int Dy)[] SlotOffsets = { (0, 1), (1, 0), (0, -1), (-1, 0) };

        /// <summary>
        /// True when <paramref name="edge"/> is a name a body-crosser vocabulary already claims for
        /// THIS composition -- canonical "Corridor"/"Alley", or this composition's own Custom-mode
        /// TunnelBodyCrosser (e.g. Barrows/tbw01's "corridor"). DoorSlotCrossers is declared for
        /// TileResolver's ungrouped-tile ADMISSION gate (an OR-relaxation: "this edge may carry a
        /// resolvable door slot"), which is safe to blend broadly; but a tileset can legitimately
        /// declare its OWN body-crosser name as a DoorSlotCrosser too (tbw01 declares "corridor" itself,
        /// for one ungrouped boundary tile -- TILE13 -- that pairs a door slot with a bare body-crosser
        /// edge instead of the port crosser) without that name ever meaning "Doorway port" for GROUP
        /// classification, where WallRoom (port) and CorridorStubChain (body) are mutually-exclusive
        /// branches keyed on exactly this distinction (e.g. tbw01's CorridorDown_1x2 family, whose outer
        /// member's lone "corridor" edge must stay a body crosser, never get reclassified as a doorway
        /// port). Checked first by IsDoorwayEdge/TryMatchDoorwayEdge so body-crosser identity always
        /// wins a naming collision.
        /// </summary>
        private static bool IsBodyCrosserName(string edge, MacroLayoutParameters parameters) =>
            Eq(edge, CorridorCrosser) || Eq(edge, AlleyCrosser) ||
            (!string.IsNullOrEmpty(parameters.TunnelBodyCrosser) && Eq(edge, parameters.TunnelBodyCrosser));

        /// <summary>
        /// True when <paramref name="edge"/> is either the canonical "Doorway" crosser or one of the
        /// tileset profile's own declared alternate door-slot crosser names
        /// (DungeonTilesetProfile.DoorSlotCrossers, threaded through as
        /// MacroLayoutParameters.DoorSlotCrossers) -- EXCLUDING any name that already belongs to this
        /// composition's body-crosser vocabulary (see IsBodyCrosserName; body-crosser identity always
        /// wins). Generalizes every "is this a door edge" check below the same way TileResolver's
        /// ungrouped-tile path already treats DoorSlotCrossers (see TileResolver's own DoorSlotCrossers
        /// doc comment) — a tileset that renames its door-slot crosser (e.g. tbx78's "doorway1"/
        /// "doorway2"/"doorway3", udp2's "Door") is recognized here identically to one using the
        /// literal string, so a WallRoom/WallAlcove/CorridorInsert group built from such tiles
        /// classifies instead of being silently rejected by IsAllowedMemberEdge. A profile declaring
        /// nothing (DoorSlotCrossers empty/null) reduces to exactly Eq(edge, DoorwayCrosser) —
        /// byte-identical to pre-generalization behavior (pinned by RoomSupplyScalingIsolationTests'
        /// non-declaring-profile SHA256 checks).
        /// </summary>
        private static bool IsDoorwayEdge(string edge, MacroLayoutParameters parameters)
        {
            if (Eq(edge, DoorwayCrosser)) return true;
            if (IsBodyCrosserName(edge, parameters)) return false;
            return parameters.DoorSlotCrossers != null && parameters.DoorSlotCrossers.Any(c => Eq(edge, c));
        }

        /// <summary>
        /// True when <paramref name="edge"/> is the canonical "Corridor" tunnel-body crosser, or this
        /// composition's own Custom-mode TunnelBodyCrosser (e.g. tdc01's "GreyCorridor", tdm01's
        /// "DesertCorridor"/"OrganicCorridor") -- the Corridor-only analogue of IsBodyCrosserName (no
        /// Alley) used by the Doorway-pair CorridorInsert splice (TryPlaceDoorwayCorridorInsert/
        /// IsValidFlankingChainCell) and the WallRoom perimeter-neighbor check (IsWallRoomSiteValid) to
        /// recognize a genuine wall-embedded tunnel chain cell regardless of which body-crosser family
        /// this district composed with. Deliberately excludes Alley: those two call sites already only
        /// ever matched canonical "Corridor" pre-generalization, and this fix must stay a no-op for
        /// that existing behavior.
        /// </summary>
        private static bool IsCorridorTunnelBodyEdge(string edge, MacroLayoutParameters parameters) =>
            Eq(edge, CorridorCrosser) ||
            (parameters.CorridorCrosserType == CorridorCrosserType.Custom &&
             !string.IsNullOrEmpty(parameters.TunnelBodyCrosser) && Eq(edge, parameters.TunnelBodyCrosser));

        /// <summary>
        /// IsStraightCorridorCell against the effective tunnel-body crosser for this composition: tries
        /// the canonical "Corridor" family first, then this composition's own Custom-mode
        /// TunnelBodyCrosser (see IsCorridorTunnelBodyEdge) when the canonical check doesn't match. Two
        /// concrete-string passes rather than a predicate because IsStraightCorridorCell requires the
        /// SAME crosser value on both opposite edges (a chain is homogeneous; it never mixes the
        /// canonical and Custom-mode names on one straight segment).
        /// </summary>
        private static bool IsStraightTunnelBodyCell(
            EdgeCrosserGrid crossers, MacroLayoutParameters parameters, (int X, int Y) cell, out bool isVertical)
        {
            if (IsStraightCorridorCell(crossers, cell, CorridorCrosser, out isVertical)) return true;

            return parameters.CorridorCrosserType == CorridorCrosserType.Custom &&
                   !string.IsNullOrEmpty(parameters.TunnelBodyCrosser) &&
                   IsStraightCorridorCell(crossers, cell, parameters.TunnelBodyCrosser, out isVertical);
        }

        /// <summary>
        /// Same recognition as <see cref="IsDoorwayEdge"/>, but also returns the SPECIFIC crosser
        /// string that matched (canonical "Doorway" or whichever declared alternate) rather than just
        /// a bool. Needed wherever the matched name itself must be threaded onward instead of assumed
        /// literal -- e.g. TryClassifyCorridorInsert's Doorway-pair branch, which must re-key its
        /// flanking cells to the SAME name the group's own tile actually carries (see
        /// TryPlaceDoorwayCorridorInsert), and HasCorridorDoorwayAdapter, which must search for an
        /// adapter tile pairing Corridor with that SAME name, not always literal "Doorway".
        /// </summary>
        private static bool TryMatchDoorwayEdge(string edge, MacroLayoutParameters parameters, out string matched)
        {
            if (Eq(edge, DoorwayCrosser)) { matched = DoorwayCrosser; return true; }
            if (!IsBodyCrosserName(edge, parameters) && parameters.DoorSlotCrossers != null)
            {
                foreach (var c in parameters.DoorSlotCrossers)
                {
                    if (Eq(edge, c)) { matched = c; return true; }
                }
            }
            matched = null;
            return false;
        }

        private sealed class GroupMember
        {
            public int LocalRow;
            public int LocalCol;
            public TileRecord Tile;
        }

        private enum GroupKind { WallRoom, OpenSetPiece, CorridorInsert, WallAlcove, CorridorStub, CorridorStubChain, ReliefPiece }

        private sealed class ClassifiedGroup
        {
            public TileGroupRecord Group;
            public List<GroupMember> Members;
            public GroupKind Kind;

            /// <summary>
            /// (LocalRow, LocalCol, Slot) for every Doorway edge whose neighbor cell falls outside the
            /// group's own footprint — the openings that must face a real tunnel corridor cell.
            /// WallRoom only.
            /// </summary>
            public List<(int Row, int Col, int Slot)> PerimeterDoorways;

            /// <summary>
            /// CorridorStubChain only: (LocalRow, LocalCol, Slot) for every body-crosser (Corridor/
            /// Alley/Custom-body) edge whose neighbor cell falls outside the group's own footprint --
            /// the SAME shape PerimeterDoorways tracks for WallRoom, but for a multi-tile group that
            /// splices directly onto an existing Tunnel-mode chain using its OWN body crosser as the
            /// opening (e.g. Barrows/tbw01's CorridorDown_1x2, whose outer member carries a lone
            /// "corridor" edge instead of a Doorway port) rather than a Doorway/port pairing -- see
            /// TryPlaceCorridorStubChain.
            /// </summary>
            public List<(int Row, int Col, int Slot)> PerimeterBodyCrossers;

            /// <summary>
            /// CorridorInsert/CorridorStub only: which crosser name ("Corridor", "Alley", "Fence", or
            /// "Bridge") the segment this group's tile fits into carries. Selects which crosser
            /// TryPlaceCorridorInsert/TryPlaceCorridorStub searches the layout for and which terrain
            /// (solid, open, or accent) the candidate cell must have.
            /// </summary>
            public string InsertCrosser;

            /// <summary>
            /// CorridorStub only: the tile's own (unrotated) edge slot carrying its single crosser
            /// edge — the slot TryPlaceCorridorStub must rotate to face back at the chain cell it
            /// splices onto.
            /// </summary>
            public int StubEdgeSlot;

            /// <summary>
            /// OpenSetPiece only: which open terrain this piece's own corners represent (primary
            /// OpenTerrain or MacroLayoutParameters.SecondaryOpenTerrain) -- a piece whose corners are a
            /// mix of solid and ONE open terrain only. TryPlaceOpenSetPiece restricts candidate sites to
            /// rooms whose LayoutRoom.OpenTerrain matches, so e.g. vmr01's Floor-cornered
            /// InteriorMosaic_2x2 only ever stamps into a Floor district room, never a Plaza one.
            /// </summary>
            public string OpenSetPieceTerrain;

            /// <summary>
            /// WallAlcove only: (LocalRow, LocalCol, Slot) for every edge of a door-carrying member
            /// whose neighbor cell falls outside the group's own footprint (out of bounds OR a hole) --
            /// the SAME "perimeter" shape PerimeterDoorways/PerimeterBodyCrossers track for
            /// WallRoom/CorridorStubChain, but scoped to only the member(s) that actually carry the
            /// group's door slot(s). See IsWallAlcoveSiteValid's own doc comment for why the network
            /// touch is restricted to these specific edges rather than any footprint cell's any side.
            /// </summary>
            public List<(int Row, int Col, int Slot)> DoorMemberPerimeterEdges;
        }

        internal static void Stamp(MacroLayout layout, MacroLayoutParameters parameters, TilesetModel tileset, System.Random random)
        {
            if (tileset == null || parameters.SetPieces == null || parameters.SetPieces.Count == 0)
                return;

            var nextRoomId = layout.Rooms.Count == 0 ? 0 : layout.Rooms.Max(r => r.Id) + 1;
            // Computed once per Stamp call (TileResolver.HasCandidate rebuilds its lookup fresh each
            // call, so this must not run per placement attempt) -- see
            // SupportsWallRoomOpenLaneBoundary's own doc comment.
            var openLaneWallRoomSupported = SupportsWallRoomOpenLaneBoundary(tileset, parameters);

            // Classification (FindGroup/TryClassify) consumes no RNG, so splitting it out of the
            // placement loop below is behavior-identical for the default name-ordered path.
            var stampOrder = new List<(string Name, int MaxCount, ClassifiedGroup Classified)>();
            foreach (var groupName in parameters.SetPieces.Keys.OrderBy(k => k, StringComparer.Ordinal))
            {
                var maxCount = EffectiveMaxCount(parameters, parameters.SetPieces[groupName]);
                if (maxCount <= 0) continue;

                var group = FindGroup(tileset, groupName);
                if (group == null) continue;

                if (!TryClassify(tileset, group, parameters, out var classified))
                    continue;

                stampOrder.Add((groupName, maxCount, classified));
            }

            // Set-piece-heavy compositions (see MacroLayoutParameters.SetPieceRoomSupplyScaling)
            // stamp LARGEST footprint first instead of the default group-name order: large groups
            // need the area's scarcest resource (big rooms with big contiguous open interiors), and
            // name order let an early-alphabetized 2x2 workhorse fragment every such interior before
            // a 3x3+ tower ever searched for a site (measured on fcx01/futcity_plaza at 32x32:
            // raising the 2x2 Tower04's budget under name order moved group share DOWN, 0.050 ->
            // 0.046, because Tower06/d_build placements collapsed 13.8 -> 1.8 tiles/area). Largest-
            // first matches the hand-built city pattern -- big towers anchor a block, small
            // structures infill -- and 2x2/1x1 groups still place freely in the fragments afterward.
            // Ties break on the same ordinal name order, and non-declaring compositions keep the
            // original name order verbatim (RoomSupplyScalingIsolationTests pins that byte-identity).
            if (parameters.SetPieceRoomSupplyScaling)
            {
                stampOrder = stampOrder
                    .OrderByDescending(e => e.Classified.Group.Rows * e.Classified.Group.Columns)
                    .ThenBy(e => e.Name, StringComparer.Ordinal)
                    .ToList();
            }

            // Hand-derived building-mass ceiling for contiguous-block city compositions above the
            // tuning baseline: hand-built promenade-family building-tile share tops out at 0.284
            // of area tiles (measured band 0.170-0.284, the
            // CityBlockContiguityTests gate). The attempt budget above is deliberately an
            // over-request bounded by real site supply -- but site supply itself moves with the
            // street network's geometry (the fewest-turns road carver freed interior stamp sites
            // one 32x32 seed used to lose to staircase lanes, measuring 0.335), so the mass share
            // needs its own explicit hand-band governor. Checked with the candidate group's own
            // footprint area so the ceiling is never overshot mid-group. Inert at or below the
            // 20x20 baseline and for every non-contiguity composition (same guard as the other
            // city-only scaling knobs -- their outputs stay byte-identical).
            var areaTiles = parameters.Width * parameters.Height;
            var massCapTiles = parameters.BuildingBlockContiguity &&
                               areaTiles > LayoutParameterConstraints.RoomSupplyBaselineTiles
                ? (int)(0.284 * areaTiles)
                : int.MaxValue;

            foreach (var (_, maxCount, classified) in stampOrder)
            {
                for (var i = 0; i < maxCount; i++)
                {
                    if (classified.Kind == GroupKind.OpenSetPiece && massCapTiles != int.MaxValue &&
                        layout.PinnedTiles.Count + classified.Group.Rows * classified.Group.Columns > massCapTiles)
                        break;

                    var placed = classified.Kind switch
                    {
                        GroupKind.WallRoom => TryPlaceWallRoom(layout, parameters, classified, random, ref nextRoomId, openLaneWallRoomSupported),
                        GroupKind.OpenSetPiece => TryPlaceOpenSetPiece(layout, parameters, classified, random),
                        GroupKind.CorridorInsert => TryPlaceCorridorInsert(layout, parameters, classified, random),
                        GroupKind.WallAlcove => TryPlaceWallAlcove(layout, parameters, classified, random, ref nextRoomId),
                        GroupKind.CorridorStub => TryPlaceCorridorStub(layout, parameters, classified, random),
                        GroupKind.CorridorStubChain => TryPlaceCorridorStubChain(layout, parameters, tileset, classified, random, ref nextRoomId),
                        GroupKind.ReliefPiece => TryPlaceReliefPiece(layout, classified, random),
                        _ => false
                    };

                    // A failed search means the grid state can't improve for this group without
                    // human/seed changes; further attempts on the same state would only repeat it.
                    if (!placed) break;
                }
            }
        }

        /// <summary>
        /// Scales a configured SetPiece budget up for larger road-declaring (city) compositions only --
        /// gated on parameters.RoadCrosser being set, i.e. only FutCity/FutCityPlaza today. Every other
        /// registered tileset's SetPieces budget was individually tuned (and measured/commented) against
        /// its own hand-built reference at the machinery's usual 16-24 test sizes and stays untouched.
        ///
        /// Fifteen hand-built fcx01 areas with a real street network measure ~19.9 building tiles per
        /// 100 area tiles. The configured per-tileset
        /// budgets (e.g. FutCity's Tower00: 3) were tuned against a 20x20 baseline; a 32x32 area has
        /// 2.56x the floor space (1024 vs 400 tiles) and, on PackedRooms/Complex-style layouts, a
        /// correspondingly larger population of SetPieceRoomCornerFloor-sized rooms to host them, so
        /// scaling the budget proportionally to area is the right first-order direction. This raises the
        /// ATTEMPT count, not a guaranteed placement count -- Stamp's own loop above already stops at
        /// the first failed placement attempt, so requesting more than a smaller area can host is
        /// harmless (a few wasted attempts, never extra buildings) while a larger area can now actually
        /// reach its real site ceiling instead of stopping at a small-area-tuned number.
        /// </summary>
        private static int EffectiveMaxCount(MacroLayoutParameters parameters, int configuredMax)
        {
            if (string.IsNullOrEmpty(parameters.RoadCrosser)) return configuredMax;

            const int baselineTiles = 20 * 20;
            var areaTiles = parameters.Width * parameters.Height;
            if (areaTiles <= baselineTiles) return configuredMax;

            // Contiguous-block compositions double the attempt budget on top of the area scale:
            // adjacency unlocks sites the isolated-margin rule physically could not host (each stamp
            // no longer consumes its own exclusive ring), so the per-area site ceiling roughly
            // doubles -- and Stamp's own break-on-first-failure still stops early wherever the real
            // ceiling is lower, so an over-request stays harmless exactly as documented above.
            // Deliberately NOT applied at or below the 20x20 tuning baseline (same guard as the area
            // scale itself): the per-tileset budgets AND the dressing-density gates are both tuned
            // against 20x20 evidence, and doubling there pushed building share past what the
            // baseline-size dressing pools can dress to the hand-built density band.
            var contiguityScale = parameters.BuildingBlockContiguity ? 2.0 : 1.0;
            var scale = (double)areaTiles / baselineTiles * contiguityScale;
            var scaled = (int)Math.Ceiling(configuredMax * scale);
            return Math.Max(configuredMax, scaled);
        }

        private static TileGroupRecord FindGroup(TilesetModel tileset, string name)
        {
            foreach (var candidate in tileset.Groups)
            {
                if (string.Equals(candidate.Name, name, StringComparison.OrdinalIgnoreCase))
                    return candidate;
            }

            return null;
        }

        /// <summary>
        /// Structurally re-verifies a configured group name against real tileset data rather than
        /// trusting a tileset profile's list blindly. A 1x1 group is first checked against
        /// CorridorInsert (see TryClassifyCorridorInsert) and then CorridorStub (see
        /// TryClassifyCorridorStub); anything that doesn't match falls through to the
        /// WallRoom/WallAlcove/OpenSetPiece rules, which reject holes (-1 members) and raised corners,
        /// then classify the surviving shape as WallRoom (all-solid corners with at least one
        /// perimeter Doorway edge), WallAlcove (all-solid corners, zero crosser edges, at least one
        /// door slot — e.g. vmr01 Room 1-5 2x2), or OpenSetPiece (no crosser edges at all, with every
        /// corner either solid or matching this layout's own open terrain, and at least one corner
        /// actually open). A door slot is tolerated (never spawns a door object — WriteMember only ever
        /// writes corners/edges, doors are placed solely by TileDoorPlanner/GroupExitPlanner at a real
        /// TransitionPoint) on any of the three: WallAlcove/OpenSetPiece since their original precedent
        /// (BigDoor01/02, InteriorHallDoor), and WallRoom too — a whole family of real door-entrance
        /// room groups (e.g. tin01/tni01's "*Room01_1x2"/"*Room02_1x2" pairs, tic01's "Room - Bath 1/2
        /// (2x1)", tii01's "Resting Pods") pairs a blank wall tile with an entrance tile carrying BOTH
        /// a perimeter Doorway edge AND a door slot — the identical unpopulated-door-slot convention
        /// IsCornerEdgeResolverReachable-equivalent ungrouped tiles already resolve under today (see
        /// TileResolver's crosser+door admission gate), just inside a multi-tile group instead of a
        /// single ungrouped tile. A shape whose ONLY Doorway edges face another member of the SAME
        /// group (an interior, not perimeter, opening — e.g. tic01's "Turret Interior - Lit/Dark (2x1)")
        /// still correctly fails below via the perimeterDoorways.Count == 0 check, unaffected by this.
        ///
        /// A mixed/open-member group (NOT all-solid-cornered) that also carries a door-family edge is
        /// no longer rejected outright either, as long as every one of its doorway edges is INTERIOR
        /// (faces another member of the SAME group, never the group's own perimeter) — it falls through
        /// to the OpenSetPiece corner-match rule below instead, tolerating the door-family edge as a
        /// never-written-for-resolution seam exactly like WriteMember's own doc comment already
        /// documents for a mismatched interior boundary (both flanking cells are pinned, so neither is
        /// ever read back via corner/edge key lookup — see WriteMember). This closes udp2's seven
        /// district "*_Entry 2x1" pairs (e.g. Office_Vinyl_Entry: an all-Wall member paired with an
        /// Office_Vinyl-open member whose sole "Door" edge faces its own group-mate) and tbx78's
        /// "elevator" group (a "wall"/"facility" split tile whose "doorway2" edge faces its own
        /// group-mate) — verified directly against both tilesets' raw .set data that the doorway edge in
        /// every case is interior-only, never perimeter. A mixed-member group whose doorway edge DOES
        /// face the group's own perimeter is deliberately still rejected (return false below) rather
        /// than tolerated: WriteMember writes every member edge verbatim into the shared per-cell grid,
        /// so a perimeter door-family edge on an open-cornered footprint cell would rewrite its
        /// unpinned neighbor's facing edge too (EdgeCrosserGrid stores one value per SHARED edge — see
        /// TryPlaceDoorwayCorridorInsert's own doc comment), and TryPlaceOpenSetPiece's site search
        /// (unlike TryPlaceWallRoom's IsWallRoomSiteValid) never verifies that neighbor can actually
        /// resolve a matching tile afterward. No verified tileset data needs that broader case this
        /// pass, so it stays out of scope rather than guessed at.
        /// </summary>
        private static bool TryClassify(TilesetModel tileset, TileGroupRecord group, MacroLayoutParameters parameters, out ClassifiedGroup classified)
        {
            classified = null;
            if (group.Rows <= 0 || group.Columns <= 0) return false;
            if (group.TileIds.Count != group.Rows * group.Columns) return false;

            // CorridorInsert/CorridorStub are checked first and independently of the
            // WallRoom/WallAlcove/OpenSetPiece rules below. Everything else falls through to the
            // hole/height rejection used by WallRoom/WallAlcove/OpenSetPiece.
            if (group.Rows == 1 && group.Columns == 1 && group.TileIds.Count == 1)
            {
                var soloTileId = group.TileIds[0];
                if (soloTileId >= 0 && soloTileId < tileset.Tiles.Count)
                {
                    var soloTile = tileset.Tiles[soloTileId];
                    if (TryClassifyCorridorInsert(tileset, soloTile, group, parameters, out classified))
                        return true;
                    if (TryClassifyCorridorStub(soloTile, group, parameters, out classified))
                        return true;
                    if (TryClassifyReliefPiece(soloTile, group, parameters, out classified))
                        return true;
                }
            }

            // A -1 TileId is a genuine hole in the group's rectangular footprint (e.g. tdt01/tds01
            // "Platform03_2x2", an L-shaped 2x2 with one empty corner) -- it is skipped here rather
            // than rejecting the whole group. Every classification decision below (hasAnyDoorway,
            // allCornersSolid, hasAnyDoor, matchesPrimary/matchesSecondary) is derived only from
            // `members`, so it already only ever sees real tiles; the hole is treated as ordinary plan
            // space the group doesn't own -- ordinary site validation (open/solid/crosser/pinned/
            // transition checks) still runs against it via the group's full Rows x Columns rectangle
            // (see IsHole call sites below), and no member write ever touches it (WriteMember only
            // runs for real members), so its corners resolve from whatever the surrounding plan
            // (neighboring real members plus, for an OpenSetPiece, the room's own open floor) already
            // wrote there.
            // A group whose members carry a body crosser (Corridor/Alley/Custom-body -- the SAME
            // vocabulary TryClassifyCorridorStub splices a single cell onto) instead of Doorway is
            // tolerated here too (e.g. Barrows/tbw01's CorridorDown_1x2, whose outer member carries a
            // lone "corridor" edge rather than a Doorway port) -- see the CorridorStubChain branch
            // below. Anything else (Fence/Bridge/an unrecognized name) still rejects the whole group.
            var stubCrossers = CorridorStubCrossersFor(parameters);
            bool IsAllowedMemberEdge(string edge) =>
                string.IsNullOrEmpty(edge) || IsDoorwayEdge(edge, parameters) || stubCrossers.Any(c => Eq(edge, c));

            var members = new List<GroupMember>();
            for (var row = 0; row < group.Rows; row++)
            {
                for (var col = 0; col < group.Columns; col++)
                {
                    var tileId = group.TileIds[row * group.Columns + col];
                    if (tileId < 0) continue; // hole
                    if (tileId >= tileset.Tiles.Count) return false; // out of range -- genuinely bad data

                    var tile = tileset.Tiles[tileId];
                    if (tile.CornerHeights[0] != 0 || tile.CornerHeights[1] != 0 ||
                        tile.CornerHeights[2] != 0 || tile.CornerHeights[3] != 0) return false; // raised

                    foreach (var edge in tile.Edges)
                    {
                        if (!IsAllowedMemberEdge(edge)) return false;
                    }

                    members.Add(new GroupMember { LocalRow = row, LocalCol = col, Tile = tile });
                }
            }
            if (members.Count == 0) return false; // an all-hole "group" is degenerate

            var perimeterDoorways = new List<(int, int, int)>();
            var perimeterBodyCrossers = new List<(int, int, int)>();
            var hasInteriorBodyCrosser = false;
            foreach (var member in members)
            {
                for (var slot = 0; slot < 4; slot++)
                {
                    var edge = member.Tile.GetEdgeAt(0, slot);
                    var isDoorway = IsDoorwayEdge(edge, parameters);
                    var isBodyCrosser = !isDoorway && stubCrossers.Any(c => Eq(edge, c));
                    if (!isDoorway && !isBodyCrosser) continue;

                    var (dx, dy) = SlotOffsets[slot];
                    var neighborRow = member.LocalRow + dy;
                    var neighborCol = member.LocalCol + dx;
                    var outOfBounds = neighborRow < 0 || neighborRow >= group.Rows ||
                                       neighborCol < 0 || neighborCol >= group.Columns;
                    // A Doorway/body-crosser edge facing a hole cell (in-bounds but no real member
                    // there) is also a perimeter opening -- the hole isn't another real member that
                    // could receive/match an interior edge, so this must face outward like any true
                    // perimeter edge.
                    var isPerimeter = outOfBounds || IsHole(group, neighborRow, neighborCol);

                    if (isDoorway)
                    {
                        if (isPerimeter) perimeterDoorways.Add((member.LocalRow, member.LocalCol, slot));
                    }
                    else if (isPerimeter)
                    {
                        perimeterBodyCrossers.Add((member.LocalRow, member.LocalCol, slot));
                    }
                    else
                    {
                        // An interior body-crosser seam (two real members facing each other with a
                        // body crosser between them) is a shape no verified data uses and CorridorStub-
                        // Chain's site/write logic doesn't handle -- reject the whole group rather than
                        // guess.
                        hasInteriorBodyCrosser = true;
                    }
                }
            }

            var hasAnyDoorway = members.Any(m => m.Tile.Edges.Any(e => IsDoorwayEdge(e, parameters)));
            var hasAnyBodyCrosser = members.Any(m => m.Tile.Edges.Any(e => !IsDoorwayEdge(e, parameters) && stubCrossers.Any(c => Eq(e, c))));
            var allCornersSolid = members.All(m => m.Tile.Corners.All(c => Eq(c, parameters.SolidTerrain)));
            var hasAnyDoor = members.Any(m => m.Tile.Doors.Count != 0);

            // CorridorStubChain: a multi-tile, all-solid-cornered group that splices directly onto an
            // existing Tunnel-mode chain using its own body crosser (Corridor/Alley/Custom-body) as the
            // opening, rather than pairing a Doorway port against a body-crossered neighbor the way
            // WallRoom does -- see TryPlaceCorridorStubChain. Checked ahead of the WallRoom/WallAlcove/
            // OpenSetPiece branches below (mutually exclusive in every verified shape: real data never
            // mixes a body-crosser edge with a Doorway edge on the same group).
            if (hasAnyBodyCrosser)
            {
                if (hasAnyDoorway || !allCornersSolid || hasInteriorBodyCrosser || perimeterBodyCrossers.Count == 0)
                    return false;

                var bodyCrosserEdge = members.SelectMany(m => m.Tile.Edges).First(e => !string.IsNullOrEmpty(e) && !IsDoorwayEdge(e, parameters));

                classified = new ClassifiedGroup
                {
                    Group = group,
                    Members = members,
                    Kind = GroupKind.CorridorStubChain,
                    PerimeterBodyCrossers = perimeterBodyCrossers,
                    InsertCrosser = stubCrossers.First(c => Eq(c, bodyCrosserEdge))
                };
                return true;
            }

            if (hasAnyDoorway)
            {
                // A doorway edge implies a WallRoom when every corner is solid; anything with at least
                // one opening facing outward classifies, anything without one is an unsupported shape
                // for this pass. A door slot is tolerated here too (see this method's own doc comment)
                // -- WriteMember never writes door data, so an unpopulated slot on a stamped WallRoom
                // member renders exactly like any other unpopulated Doorway-keyed door-slot tile
                // already does today.
                if (allCornersSolid)
                {
                    if (perimeterDoorways.Count == 0) return false;

                    classified = new ClassifiedGroup
                    {
                        Group = group,
                        Members = members,
                        Kind = GroupKind.WallRoom,
                        PerimeterDoorways = perimeterDoorways
                    };
                    return true;
                }

                // Mixed/open-member shape: only tolerated when every doorway edge is interior (see this
                // method's own doc comment) -- a genuine perimeter doorway edge on an open-cornered
                // footprint is still an unsupported shape, rejected here rather than risking an
                // unresolvable neighbor cell. Falls through to the OpenSetPiece corner-match rule below
                // when this holds.
                if (perimeterDoorways.Count != 0) return false;
            }

            // WallAlcove: all-solid corners, zero crosser edges anywhere, at least one door slot (e.g.
            // vmr01 "Room 1 2x2".."Room 5 2x2" — a small enclosed wall chamber with a doorframe object
            // but no Doorway crosser vocabulary of its own). Checked before OpenSetPiece so an
            // all-solid group is never misrouted there (see OpenSetPiece's own open-corner requirement
            // below, which independently guards against the same misroute).
            //
            // DoorMemberPerimeterEdges records which of the door-carrying member's OWN edges face
            // outside the footprint (see IsWallAlcoveSiteValid) -- computed the same way
            // PerimeterDoorways/PerimeterBodyCrossers are above, just scoped to members with
            // Tile.Doors.Count != 0 instead of a Doorway/body-crosser edge.
            if (allCornersSolid && hasAnyDoor)
            {
                var doorMemberPerimeterEdges = new List<(int, int, int)>();
                foreach (var member in members)
                {
                    if (member.Tile.Doors.Count == 0) continue;

                    for (var slot = 0; slot < 4; slot++)
                    {
                        var (dx, dy) = SlotOffsets[slot];
                        var neighborRow = member.LocalRow + dy;
                        var neighborCol = member.LocalCol + dx;
                        var outOfBounds = neighborRow < 0 || neighborRow >= group.Rows ||
                                           neighborCol < 0 || neighborCol >= group.Columns;
                        if (outOfBounds || IsHole(group, neighborRow, neighborCol))
                            doorMemberPerimeterEdges.Add((member.LocalRow, member.LocalCol, slot));
                    }
                }

                classified = new ClassifiedGroup
                {
                    Group = group,
                    Members = members,
                    Kind = GroupKind.WallAlcove,
                    PerimeterDoorways = new List<(int, int, int)>(),
                    DoorMemberPerimeterEdges = doorMemberPerimeterEdges
                };
                return true;
            }

            // OpenSetPiece: every corner must be solid plus EXACTLY ONE of this layout's open terrains
            // (primary OpenTerrain or, when districts are configured, SecondaryOpenTerrain), with AT
            // LEAST ONE corner actually equal to that open terrain — an all-solid group vacuously
            // satisfies "every corner is solid-or-open" without this, which would misclassify a
            // WallAlcove-shaped group (already routed above) or any other all-solid shape as floor
            // decor. A group whose corners mix both open terrains, or match neither (e.g. a
            // Floor-cornered piece in a Chasm-only layout), is structurally incompatible here and
            // skipped whole. Determining which single terrain the piece's open corners represent lets
            // TryPlaceOpenSetPiece restrict candidate rooms to that same district (see
            // LayoutRoom.OpenTerrain). A door slot is tolerated (never spawns a door object) — e.g.
            // tdt01/tds01 StairsDown_2x2/StairsUp_2x2 and vmr01 ExteriorStairsDown_2x2/
            // ExteriorStairsUp_2x2/ExteriorRuinedTower_2x2 each carry exactly one.
            var matchesPrimary = members.All(m => m.Tile.Corners.All(c => Eq(c, parameters.SolidTerrain) || Eq(c, parameters.OpenTerrain))) &&
                                  members.Any(m => m.Tile.Corners.Any(c => Eq(c, parameters.OpenTerrain)));
            var matchesSecondary = !string.IsNullOrEmpty(parameters.SecondaryOpenTerrain) &&
                                    members.All(m => m.Tile.Corners.All(c => Eq(c, parameters.SolidTerrain) || Eq(c, parameters.SecondaryOpenTerrain))) &&
                                    members.Any(m => m.Tile.Corners.Any(c => Eq(c, parameters.SecondaryOpenTerrain)));

            string openSetPieceTerrain;
            if (matchesPrimary) openSetPieceTerrain = parameters.OpenTerrain;
            else if (matchesSecondary) openSetPieceTerrain = parameters.SecondaryOpenTerrain;
            else return false;

            classified = new ClassifiedGroup
            {
                Group = group,
                Members = members,
                Kind = GroupKind.OpenSetPiece,
                PerimeterDoorways = perimeterDoorways,
                OpenSetPieceTerrain = openSetPieceTerrain
            };
            return true;
        }

        /// <summary>
        /// Classifies a 1x1 group as a CorridorInsert: edges carry exactly one opposite pair of a
        /// single crosser (Top+Bottom or Left+Right) with the other two edges blank — anything else (a
        /// Doorway edge, a third crosser, an L/T/X junction pattern) is rejected. Matches only a
        /// straight segment, never a junction or room-adapter tile. Tries Corridor and Alley (solid
        /// corners, a wall-embedded tunnel gate: BigDoor01/02, BigDoorAlley) before Fence (this
        /// layout's open terrain, a fence-run gate: FenceDoor01/02, Interior/ExteriorFenceDoor) before
        /// Bridge (this layout's accent/channel terrain, a gate spliced into a
        /// LayoutAccentChannelCarver span: tdt01 BridgeDoor, tds01/vmr01 BridgeDoor01).
        ///
        /// A fifth, structurally distinct case is tried last: an all-solid, opposite-Doorway-PAIR tile
        /// (e.g. tdt01 "Door_Trans" TILE151 -- a pass-through doorway segment, not a room/tunnel
        /// junction opening) can splice into a straight Corridor run too, but unlike the four crosser
        /// gates above it can never match an existing chain cell directly (the chain's own crosser is
        /// "Corridor", not "Doorway") -- see TryPlaceCorridorInsert's Doorway branch, which rewrites the
        /// two flanking plan edges to Doorway so the flanking cells re-key to the tileset's own
        /// solid-corner Corridor/Doorway adapter tile at ordinary resolution time. Gated on
        /// HasCorridorDoorwayAdapter so a tileset lacking that adapter tile never enables an insert that
        /// could leave the flanking cells unresolvable.
        /// </summary>
        private static bool TryClassifyCorridorInsert(TilesetModel tileset, TileRecord tile, TileGroupRecord group, MacroLayoutParameters parameters, out ClassifiedGroup classified)
        {
            classified = null;

            if (tile.CornerHeights[0] != 0 || tile.CornerHeights[1] != 0 ||
                tile.CornerHeights[2] != 0 || tile.CornerHeights[3] != 0) return false; // raised

            var accentTerrain = !string.IsNullOrEmpty(parameters.ChannelTerrain) ? parameters.ChannelTerrain : parameters.AccentTerrain;

            var allSolid = tile.Corners.All(c => Eq(c, parameters.SolidTerrain));
            var allOpen = !string.IsNullOrEmpty(parameters.OpenTerrain) && tile.Corners.All(c => Eq(c, parameters.OpenTerrain));
            var allAccent = !string.IsNullOrEmpty(accentTerrain) && tile.Corners.All(c => Eq(c, accentTerrain));
            if (!allSolid && !allOpen && !allAccent) return false;

            foreach (var crosser in CorridorInsertCrossersFor(parameters))
            {
                // Corridor/Alley/Custom-body inserts are wall-embedded tunnel gates (solid corners); a
                // Fence insert is a fence-run gate (this layout's open terrain); a Bridge insert is a
                // channel gate (this layout's accent/channel terrain). Skip whichever terrain this
                // tile's own corners don't support for that crosser.
                var terrainMatches = crosser switch
                {
                    FenceCrosser => allOpen,
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
                    if (!Eq(edge, crosser)) { edgesMatch = false; break; } // any other crosser disqualifies this candidate
                    hasCrosser[slot] = true;
                }
                if (!edgesMatch) continue;

                var isVerticalPair = hasCrosser[EdgeSlot.Top] && hasCrosser[EdgeSlot.Bottom] &&
                                      !hasCrosser[EdgeSlot.Left] && !hasCrosser[EdgeSlot.Right];
                var isHorizontalPair = hasCrosser[EdgeSlot.Left] && hasCrosser[EdgeSlot.Right] &&
                                        !hasCrosser[EdgeSlot.Top] && !hasCrosser[EdgeSlot.Bottom];
                if (!isVerticalPair && !isHorizontalPair) continue;

                classified = new ClassifiedGroup
                {
                    Group = group,
                    Members = new List<GroupMember> { new GroupMember { LocalRow = 0, LocalCol = 0, Tile = tile } },
                    Kind = GroupKind.CorridorInsert,
                    PerimeterDoorways = new List<(int, int, int)>(),
                    InsertCrosser = crosser
                };
                return true;
            }

            // Doorway-pair straight-segment insert (see this method's doc comment). Solid corners
            // only -- a pass-through doorway segment is always a wall-embedded shape, never an open-
            // terrain or accent-terrain one.
            if (allSolid)
            {
                var hasDoorwayEdge = new bool[4];
                var edgesAreDoorwayOnly = true;
                string matchedDoorwayCrosser = null;
                for (var slot = 0; slot < 4; slot++)
                {
                    var edge = tile.Edges[slot] ?? string.Empty;
                    if (edge.Length == 0) continue;
                    if (!TryMatchDoorwayEdge(edge, parameters, out var matched)) { edgesAreDoorwayOnly = false; break; }
                    // Both edges of the pair must be the SAME door-slot crosser name -- a tile mixing
                    // canonical "Doorway" on one edge with a declared alternate on the other is not a
                    // shape any verified data uses, so treat it like any other unrecognized pattern.
                    if (matchedDoorwayCrosser == null) matchedDoorwayCrosser = matched;
                    else if (!Eq(matchedDoorwayCrosser, matched)) { edgesAreDoorwayOnly = false; break; }
                    hasDoorwayEdge[slot] = true;
                }

                var isVerticalDoorwayPair = hasDoorwayEdge[EdgeSlot.Top] && hasDoorwayEdge[EdgeSlot.Bottom] &&
                                             !hasDoorwayEdge[EdgeSlot.Left] && !hasDoorwayEdge[EdgeSlot.Right];
                var isHorizontalDoorwayPair = hasDoorwayEdge[EdgeSlot.Left] && hasDoorwayEdge[EdgeSlot.Right] &&
                                               !hasDoorwayEdge[EdgeSlot.Top] && !hasDoorwayEdge[EdgeSlot.Bottom];

                if (edgesAreDoorwayOnly && (isVerticalDoorwayPair || isHorizontalDoorwayPair) &&
                    HasCorridorDoorwayAdapter(tileset, parameters, matchedDoorwayCrosser))
                {
                    classified = new ClassifiedGroup
                    {
                        Group = group,
                        Members = new List<GroupMember> { new GroupMember { LocalRow = 0, LocalCol = 0, Tile = tile } },
                        Kind = GroupKind.CorridorInsert,
                        PerimeterDoorways = new List<(int, int, int)>(),
                        InsertCrosser = matchedDoorwayCrosser
                    };
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// True when the tileset carries at least one flat, all-solid-corner tile with exactly one
        /// Corridor edge and its opposite edge carrying <paramref name="doorwayCrosser"/> (the other
        /// two blank) -- the corridor-to-doorway adapter a Doorway-pair CorridorInsert's flanking
        /// cells must re-key to once their shared edge is rewritten from Corridor to that same door-
        /// slot crosser name (see TryPlaceDoorwayCorridorInsert). Verified present in tdt01 (TILE46),
        /// tds01 (TILE47), zsf01 (TILE20), and vmr01 (TILE45) using the canonical "Doorway" name;
        /// <paramref name="doorwayCrosser"/> generalizes the search to whichever door-slot crosser the
        /// candidate group's own tile actually carries (canonical or a profile-declared alternate --
        /// see TryMatchDoorwayEdge), so a tileset that renames it is checked for the SAME adapter shape
        /// under its own name rather than always literal "Doorway". Checked here rather than assumed so
        /// a future tileset lacking it never enables an unplaceable splice.
        /// </summary>
        private static bool HasCorridorDoorwayAdapter(TilesetModel tileset, MacroLayoutParameters parameters, string doorwayCrosser)
        {
            if (string.IsNullOrEmpty(doorwayCrosser)) return false;

            foreach (var candidate in tileset.Tiles)
            {
                if (candidate.CornerHeights[0] != 0 || candidate.CornerHeights[1] != 0 ||
                    candidate.CornerHeights[2] != 0 || candidate.CornerHeights[3] != 0) continue; // raised
                if (!candidate.Corners.All(c => Eq(c, parameters.SolidTerrain))) continue;

                var corridorSlot = -1;
                var doorwaySlot = -1;
                var onlyThoseTwo = true;
                for (var slot = 0; slot < 4; slot++)
                {
                    var edge = candidate.Edges[slot] ?? string.Empty;
                    if (edge.Length == 0) continue;
                    if (Eq(edge, CorridorCrosser)) corridorSlot = slot;
                    else if (Eq(edge, doorwayCrosser)) doorwaySlot = slot;
                    else { onlyThoseTwo = false; break; }
                }

                if (!onlyThoseTwo || corridorSlot == -1 || doorwaySlot == -1) continue;
                if (Math.Abs(corridorSlot - doorwaySlot) == 2) return true; // opposite pair
            }

            return false;
        }

        /// <summary>
        /// Classifies a 1x1 group as a CorridorStub: flat, all-solid corners, exactly ONE crosser edge
        /// (a dead end, never an opposite pair — that shape is CorridorInsert's, checked first by
        /// TryClassify) of Corridor or Alley, with an optional door slot the tile's own art carries as
        /// a doorframe (this pass never spawns a door object for it, matching CorridorInsert's
        /// precedent). Matches tdt01 StairsDown01/StairsUp01, tds01 StairsDown/StairsUp, and vmr01
        /// InteriorStairsDown/InteriorStairsUp (Corridor) and ExteriorStairsDown/ExteriorStairsUp
        /// (Alley) — a themed dead-end cap TryPlaceCorridorStub splices onto an existing Tunnel-mode
        /// chain by extending it one cell.
        /// </summary>
        private static bool TryClassifyCorridorStub(TileRecord tile, TileGroupRecord group, MacroLayoutParameters parameters, out ClassifiedGroup classified)
        {
            classified = null;

            if (tile.CornerHeights[0] != 0 || tile.CornerHeights[1] != 0 ||
                tile.CornerHeights[2] != 0 || tile.CornerHeights[3] != 0) return false; // raised
            if (!tile.Corners.All(c => Eq(c, parameters.SolidTerrain))) return false;

            foreach (var crosser in CorridorStubCrossersFor(parameters))
            {
                var hasCrosser = new bool[4];
                var crosserCount = 0;
                var edgesMatch = true;
                for (var slot = 0; slot < 4; slot++)
                {
                    var edge = tile.Edges[slot] ?? string.Empty;
                    if (edge.Length == 0) continue;
                    if (!Eq(edge, crosser)) { edgesMatch = false; break; }
                    hasCrosser[slot] = true;
                    crosserCount++;
                }
                if (!edgesMatch || crosserCount != 1) continue; // exactly one edge — a dead end, not a pair

                var slotIndex = Array.IndexOf(hasCrosser, true);

                classified = new ClassifiedGroup
                {
                    Group = group,
                    Members = new List<GroupMember> { new GroupMember { LocalRow = 0, LocalCol = 0, Tile = tile } },
                    Kind = GroupKind.CorridorStub,
                    PerimeterDoorways = new List<(int, int, int)>(),
                    InsertCrosser = crosser,
                    StubEdgeSlot = slotIndex
                };
                return true;
            }

            return false;
        }

        // ---------------- ReliefPiece ----------------

        /// <summary>
        /// A RAISED (non-flat) 1x1 group piece -- a baked-mesh set piece authored to straddle a
        /// specific corner-height step, e.g. tde01's "Ramp - Straight" ([Floor 0,0,1,1], a walkable
        /// ramp mesh over a straight rim edge) and "Ramp - Corner, *" pieces, tdm01's "[Cave]/
        /// [Desert]/[Organic] Ramp", or ttf01's raised wall/gate/cave-mouth family ("Wall - Breach/
        /// Door/Tower 1/2, City/Forest,Water,Cobbles", "Ramp - City Wall"/"Ramp - Moss Wall", "Wall -
        /// Breach/Door, Moss", "Cave" -- and ttd01's "SmallCave", tdm01's "[City/Cave/Desert/Organic]
        /// Cave Entrance"). Every flat group kind above rejects non-flat members outright (their sites
        /// are flat by construction); a relief piece is the opposite -- its site is a cell whose
        /// PAINTED corner (terrain, height) field, produced by the elevation/pool/relief height
        /// passes, exactly matches the piece's own corner profile under some rotation (see
        /// TryPlaceReliefPiece). Classification is deliberately structural only (non-flat, 1x1); the
        /// exact-match site search is what guarantees a stamped piece is always consistent with the
        /// surrounding grid.
        ///
        /// Edges may be blank, OR ALL equal this composition's own declared ramp-lane crosser
        /// (LayoutElevationPainter.RampCrosserFor, canonical "Ramp" by default) -- mirrors
        /// TileCoverageCensusTests.IsTerrainReliefReachable's identical ungrouped-tile rule, since a
        /// ramp-lane-crossered raised piece (e.g. "Ramp - City Wall") sits on exactly the same 1-wide
        /// lane LayoutReliefPainter.TrySpliceReliefLane carves; any OTHER crosser name (Doorway,
        /// Corridor, Fence, Bridge, an unrelated tunnel family) still rejects the whole piece -- a
        /// relief piece is never a tunnel/room-network member.
        ///
        /// A door slot is tolerated exactly like WallAlcove/OpenSetPiece/WallRoom already tolerate one
        /// (see TryClassify's own doc comment) -- never spawns a door object (WriteMember/
        /// TryPlaceReliefPiece never write door data), so an unpopulated slot on a stamped ReliefPiece
        /// renders exactly like any other unpopulated door-slot tile already does today. This is what
        /// closes ttf01's raised gate-tower/breach/moss-wall family and the "Cave"/"SmallCave"/"Cave
        /// Entrance" doorframe-on-a-rim-step shape shared by ttf01/ttd01/tdm01.
        /// </summary>
        private static bool TryClassifyReliefPiece(TileRecord tile, TileGroupRecord group, MacroLayoutParameters parameters, out ClassifiedGroup classified)
        {
            classified = null;

            var isFlat = tile.CornerHeights[0] == 0 && tile.CornerHeights[1] == 0 &&
                         tile.CornerHeights[2] == 0 && tile.CornerHeights[3] == 0;
            if (isFlat) return false;

            var rampCrosser = LayoutElevationPainter.RampCrosserFor(parameters);
            foreach (var edge in tile.Edges)
            {
                if (string.IsNullOrEmpty(edge)) continue;
                if (!Eq(edge, rampCrosser)) return false;
            }

            // A uniform raised profile (all 4 corners at the same nonzero height) normalizes to flat
            // -- such a "plateau top" piece would match ordinary flat interior cells at a raised
            // placementHeight, which is the plain resolver's territory, not a relief step piece.
            if (tile.CornerHeights[0] == tile.CornerHeights[1] &&
                tile.CornerHeights[1] == tile.CornerHeights[2] &&
                tile.CornerHeights[2] == tile.CornerHeights[3]) return false;

            classified = new ClassifiedGroup
            {
                Group = group,
                Members = new List<GroupMember> { new() { LocalRow = 0, LocalCol = 0, Tile = tile } },
                Kind = GroupKind.ReliefPiece
            };
            return true;
        }

        /// <summary>
        /// Finds a cell whose painted corner labels AND normalized corner-height deltas exactly match
        /// the piece's own rotated profile, then pins the piece there at placementHeight = the cell's
        /// grid height min minus the piece's own corner-height min -- TileResolver's height-aware
        /// placement convention, carried through the pin so the resolved tile sits at the same final
        /// Tile_Height an ungrouped twin of the same shape would. Cell edges must match the piece's OWN
        /// rotated edge pattern exactly -- blank for a crosser-free piece (the original precedent,
        /// e.g. tde01's "Ramp - Straight"), or the composition's ramp-lane crosser for a lane-edged
        /// piece (e.g. ttf01's "Ramp - City Wall" -- the SAME lane edge LayoutReliefPainter.
        /// TrySpliceReliefLane already writes into the grid, so an exact per-orientation edge match,
        /// not a blanket "must be blank" filter, is what correctly admits it) -- see
        /// TryClassifyReliefPiece's edge-tolerance doc comment. The cell must not host a transition
        /// anchor or an earlier pin. No corner/edge rewrite -- like CorridorInsert, the site already
        /// matches by construction, so only PinnedTiles is written.
        /// </summary>
        private static bool TryPlaceReliefPiece(MacroLayout layout, ClassifiedGroup classified, System.Random random)
        {
            var tile = classified.Members[0].Tile;
            var corners = layout.Corners;
            var crossers = layout.Crossers;
            var width = corners.Width;
            var height = corners.Height;

            var tileMin = Math.Min(Math.Min(tile.CornerHeights[0], tile.CornerHeights[1]),
                Math.Min(tile.CornerHeights[2], tile.CornerHeights[3]));

            var transitionTiles = new HashSet<(int X, int Y)>(layout.Transitions.Select(t => t.Tile));
            var candidates = new List<((int X, int Y) Cell, int Orientation, int PlacementHeight)>();

            for (var y = 0; y < height; y++)
            {
                for (var x = 0; x < width; x++)
                {
                    var cell = (X: x, Y: y);
                    if (layout.PinnedTiles.ContainsKey(cell)) continue;
                    if (transitionTiles.Contains(cell)) continue;

                    var hTl = corners.Heights[x, y + 1];
                    var hTr = corners.Heights[x + 1, y + 1];
                    var hBr = corners.Heights[x + 1, y];
                    var hBl = corners.Heights[x, y];
                    var cellMin = Math.Min(Math.Min(hTl, hTr), Math.Min(hBr, hBl));

                    // NWN Tile_Height is never negative; a painted field whose min sits below the
                    // piece's own authored min cannot host it.
                    var placementHeight = cellMin - tileMin;
                    if (placementHeight < 0) continue;

                    var cellTop = crossers.GetEdge(x, y, EdgeSlot.Top);
                    var cellRight = crossers.GetEdge(x, y, EdgeSlot.Right);
                    var cellBottom = crossers.GetEdge(x, y, EdgeSlot.Bottom);
                    var cellLeft = crossers.GetEdge(x, y, EdgeSlot.Left);

                    for (var orientation = 0; orientation < 4; orientation++)
                    {
                        var matches =
                            Eq(corners.Labels[x, y + 1], tile.GetCornerAt(orientation, CornerSlot.TopLeft)) &&
                            Eq(corners.Labels[x + 1, y + 1], tile.GetCornerAt(orientation, CornerSlot.TopRight)) &&
                            Eq(corners.Labels[x + 1, y], tile.GetCornerAt(orientation, CornerSlot.BottomRight)) &&
                            Eq(corners.Labels[x, y], tile.GetCornerAt(orientation, CornerSlot.BottomLeft)) &&
                            hTl - cellMin == tile.GetCornerHeightAt(orientation, CornerSlot.TopLeft) - tileMin &&
                            hTr - cellMin == tile.GetCornerHeightAt(orientation, CornerSlot.TopRight) - tileMin &&
                            hBr - cellMin == tile.GetCornerHeightAt(orientation, CornerSlot.BottomRight) - tileMin &&
                            hBl - cellMin == tile.GetCornerHeightAt(orientation, CornerSlot.BottomLeft) - tileMin &&
                            Eq(cellTop, tile.GetEdgeAt(orientation, EdgeSlot.Top)) &&
                            Eq(cellRight, tile.GetEdgeAt(orientation, EdgeSlot.Right)) &&
                            Eq(cellBottom, tile.GetEdgeAt(orientation, EdgeSlot.Bottom)) &&
                            Eq(cellLeft, tile.GetEdgeAt(orientation, EdgeSlot.Left));

                        if (matches)
                        {
                            candidates.Add((cell, orientation, placementHeight));
                            break; // one orientation per cell is plenty -- extra rotations of the same site add nothing
                        }
                    }
                }
            }

            if (candidates.Count == 0) return false;

            var pick = candidates[random.Next(candidates.Count)];
            layout.PinnedTiles[pick.Cell] = (tile.TileId, pick.Orientation, pick.PlacementHeight);
            return true;
        }

        // ---------------- CorridorInsert ----------------

        /// <summary>
        /// Finds a straight tunnel segment cell (fully solid corners, crosser plan exactly one
        /// opposite Corridor pair and nothing else — never a junction) and pins the insert tile at
        /// whichever orientation aligns its own Corridor pair with the plan's axis. Corners and
        /// crossers at the cell already match by construction (the plan's existing tunnel data), so
        /// only PinnedTiles needs writing — no corner/edge rewrite like WallRoom/OpenSetPiece.
        /// </summary>
        private static bool TryPlaceCorridorInsert(
            MacroLayout layout, MacroLayoutParameters parameters, ClassifiedGroup classified, System.Random random)
        {
            var tile = classified.Members[0].Tile;
            var crosser = classified.InsertCrosser;

            if (IsDoorwayEdge(crosser, parameters))
                return TryPlaceDoorwayCorridorInsert(layout, parameters, tile, crosser, random);

            var corners = layout.Corners;
            var crossers = layout.Crossers;
            var width = corners.Width;
            var height = corners.Height;
            var accentTerrain = !string.IsNullOrEmpty(parameters.ChannelTerrain) ? parameters.ChannelTerrain : parameters.AccentTerrain;

            var transitionTiles = new HashSet<(int X, int Y)>(layout.Transitions.Select(t => t.Tile));
            var candidates = new List<(int X, int Y)>();

            for (var y = 0; y < height; y++)
            {
                for (var x = 0; x < width; x++)
                {
                    var cell = (X: x, Y: y);
                    if (layout.PinnedTiles.ContainsKey(cell)) continue;
                    if (transitionTiles.Contains(cell)) continue;

                    // Corridor/Alley inserts sit on a fully solid cell (wall-embedded tunnel body); a
                    // Fence insert sits on a fully open cell (a gate spliced into an open fence run); a
                    // Bridge insert sits on a fully accent-terrain cell (a gate spliced into a channel
                    // span carved by LayoutAccentChannelCarver).
                    var terrainOk = crosser switch
                    {
                        FenceCrosser => IsFullyOpenCell(corners, cell, parameters.OpenTerrain),
                        BridgeCrosser => !string.IsNullOrEmpty(accentTerrain) && IsFullyOpenCell(corners, cell, accentTerrain),
                        _ => IsFullySolidCell(corners, cell, parameters.SolidTerrain)
                    };
                    if (!terrainOk) continue;

                    if (!IsStraightCorridorCell(crossers, cell, crosser, out _)) continue;

                    candidates.Add(cell);
                }
            }

            Shuffle(candidates, random);

            foreach (var cell in candidates)
            {
                IsStraightCorridorCell(crossers, cell, crosser, out var isVertical);

                for (var orientation = 0; orientation < 4; orientation++)
                {
                    var oTop = tile.GetEdgeAt(orientation, EdgeSlot.Top);
                    var oRight = tile.GetEdgeAt(orientation, EdgeSlot.Right);
                    var oBottom = tile.GetEdgeAt(orientation, EdgeSlot.Bottom);
                    var oLeft = tile.GetEdgeAt(orientation, EdgeSlot.Left);

                    var matches = isVertical
                        ? Eq(oTop, crosser) && Eq(oBottom, crosser) && (oLeft ?? "").Length == 0 && (oRight ?? "").Length == 0
                        : Eq(oLeft, crosser) && Eq(oRight, crosser) && (oTop ?? "").Length == 0 && (oBottom ?? "").Length == 0;

                    if (!matches) continue;

                    layout.PinnedTiles[cell] = (tile.TileId, orientation, 0);
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// True when the cell's crosser plan is exactly one opposite pair of <paramref name="crosser"/>
        /// (Top+Bottom or Left+Right) with the other two edges blank — a straight segment, never a
        /// junction or a room-adapter (Doorway) cell. <paramref name="isVertical"/> is true for Top+Bottom.
        /// </summary>
        private static bool IsStraightCorridorCell(EdgeCrosserGrid crossers, (int X, int Y) cell, string crosser, out bool isVertical)
        {
            var top = crossers.GetEdge(cell.X, cell.Y, EdgeSlot.Top);
            var right = crossers.GetEdge(cell.X, cell.Y, EdgeSlot.Right);
            var bottom = crossers.GetEdge(cell.X, cell.Y, EdgeSlot.Bottom);
            var left = crossers.GetEdge(cell.X, cell.Y, EdgeSlot.Left);

            isVertical = Eq(top, crosser) && Eq(bottom, crosser) && left.Length == 0 && right.Length == 0;
            var isHorizontal = Eq(left, crosser) && Eq(right, crosser) && top.Length == 0 && bottom.Length == 0;

            return isVertical || isHorizontal;
        }

        /// <summary>
        /// Places a Doorway-pair CorridorInsert (e.g. tdt01 "Door_Trans"): finds a straight Corridor
        /// chain cell whose two immediate flanking cells (in the pair's axis) are themselves plain,
        /// unpinned, non-transition straight-segment cells (their far edge stays Corridor, their other
        /// two edges blank -- a mid-run cell, never a junction or room port), pins the insert tile at
        /// the aligned orientation, and rewrites the insert cell's own two Doorway-axis crosser edges
        /// from Corridor to Doorway. Because EdgeCrosserGrid stores one value per SHARED edge (see its
        /// doc comment), writing the insert cell's Top/Bottom (or Left/Right) edges simultaneously
        /// rewrites the two flanking cells' facing edges too, while their OUTWARD edges (toward the
        /// rest of the chain) are untouched -- so at ordinary TileResolver resolution time each
        /// flanking cell naturally re-keys to the tileset's own solid-corner Corridor/Doorway adapter
        /// tile (existence verified once at classify time by HasCorridorDoorwayAdapter). The flanking
        /// cells are never pinned or otherwise written here; TileDoorPlanner runs afterward and already
        /// skips any cell carrying a crosser edge, so it never contests this splice.
        /// </summary>
        private static bool TryPlaceDoorwayCorridorInsert(
            MacroLayout layout, MacroLayoutParameters parameters, TileRecord tile, string doorwayCrosser, System.Random random)
        {
            var corners = layout.Corners;
            var crossers = layout.Crossers;
            var width = corners.Width;
            var height = corners.Height;

            var transitionTiles = new HashSet<(int X, int Y)>(layout.Transitions.Select(t => t.Tile));
            var candidates = new List<(int X, int Y)>();

            for (var y = 0; y < height; y++)
            {
                for (var x = 0; x < width; x++)
                {
                    var cell = (X: x, Y: y);
                    if (layout.PinnedTiles.ContainsKey(cell)) continue;
                    if (transitionTiles.Contains(cell)) continue;
                    if (!IsFullySolidCell(corners, cell, parameters.SolidTerrain)) continue;
                    if (!IsStraightTunnelBodyCell(crossers, parameters, cell, out var isVertical)) continue;

                    var (dxA, dyA) = isVertical ? (0, 1) : (1, 0); // Top or Right direction
                    var neighborA = (X: cell.X + dxA, Y: cell.Y + dyA);
                    var neighborB = (X: cell.X - dxA, Y: cell.Y - dyA);

                    if (!IsValidFlankingChainCell(layout, parameters, neighborA, isVertical, transitionTiles)) continue;
                    if (!IsValidFlankingChainCell(layout, parameters, neighborB, isVertical, transitionTiles)) continue;

                    candidates.Add(cell);
                }
            }

            Shuffle(candidates, random);

            foreach (var cell in candidates)
            {
                IsStraightTunnelBodyCell(crossers, parameters, cell, out var isVertical);

                for (var orientation = 0; orientation < 4; orientation++)
                {
                    var oTop = tile.GetEdgeAt(orientation, EdgeSlot.Top);
                    var oRight = tile.GetEdgeAt(orientation, EdgeSlot.Right);
                    var oBottom = tile.GetEdgeAt(orientation, EdgeSlot.Bottom);
                    var oLeft = tile.GetEdgeAt(orientation, EdgeSlot.Left);

                    var matches = isVertical
                        ? Eq(oTop, doorwayCrosser) && Eq(oBottom, doorwayCrosser) && (oLeft ?? "").Length == 0 && (oRight ?? "").Length == 0
                        : Eq(oLeft, doorwayCrosser) && Eq(oRight, doorwayCrosser) && (oTop ?? "").Length == 0 && (oBottom ?? "").Length == 0;

                    if (!matches) continue;

                    if (isVertical)
                    {
                        crossers.SetEdge(cell.X, cell.Y, EdgeSlot.Top, doorwayCrosser);
                        crossers.SetEdge(cell.X, cell.Y, EdgeSlot.Bottom, doorwayCrosser);
                    }
                    else
                    {
                        crossers.SetEdge(cell.X, cell.Y, EdgeSlot.Left, doorwayCrosser);
                        crossers.SetEdge(cell.X, cell.Y, EdgeSlot.Right, doorwayCrosser);
                    }

                    layout.PinnedTiles[cell] = (tile.TileId, orientation, 0);
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// True when <paramref name="neighbor"/> is a legal Doorway-pair-insert flank: in bounds, not
        /// already pinned, not a transition anchor, fully solid, and itself a pure straight Corridor
        /// segment on the SAME axis as the candidate insert cell (its far edge keeps Corridor, its
        /// other two edges stay blank) -- never a dead end, junction, or perpendicular chain cell,
        /// which would mean the splice sits at a joint rather than mid-run.
        /// </summary>
        private static bool IsValidFlankingChainCell(
            MacroLayout layout, MacroLayoutParameters parameters, (int X, int Y) neighbor, bool axisIsVertical,
            HashSet<(int X, int Y)> transitionTiles)
        {
            var corners = layout.Corners;
            var crossers = layout.Crossers;

            if (neighbor.X < 0 || neighbor.Y < 0 || neighbor.X >= corners.Width || neighbor.Y >= corners.Height) return false;
            if (layout.PinnedTiles.ContainsKey(neighbor)) return false;
            if (transitionTiles.Contains(neighbor)) return false;
            if (!IsFullySolidCell(corners, neighbor, parameters.SolidTerrain)) return false;

            return IsStraightTunnelBodyCell(crossers, parameters, neighbor, out var neighborIsVertical) &&
                   neighborIsVertical == axisIsVertical;
        }

        // ---------------- CorridorStub ----------------

        /// <summary>
        /// Extends an existing Tunnel-mode chain by one dead-end cell: finds an all-solid,
        /// currently crosser-free, unpinned, non-transition cell adjacent to an existing chain cell
        /// that already carries the classified crosser on some OTHER edge (confirming it's a genuine
        /// body cell of an existing Corridor/Alley chain, not a fresh cell of its own), sets the
        /// shared edge between the two cells to that crosser — splicing a one-cell stub off the chain
        /// — and pins the stub tile at whichever orientation puts its own single crosser edge on that
        /// shared slot (with the other three edges blank, matching the classified dead-end shape). A
        /// no-op (returns false, no grid mutation) when no chain of the required crosser exists yet —
        /// e.g. any OpenLane-mode layout profile, where CorridorMode never carves Corridor/Alley edges.
        /// </summary>
        private static bool TryPlaceCorridorStub(
            MacroLayout layout, MacroLayoutParameters parameters, ClassifiedGroup classified, System.Random random)
        {
            var tile = classified.Members[0].Tile;
            var crosser = classified.InsertCrosser;
            var corners = layout.Corners;
            var crossers = layout.Crossers;
            var width = corners.Width;
            var height = corners.Height;

            var transitionTiles = new HashSet<(int X, int Y)>(layout.Transitions.Select(t => t.Tile));
            var candidates = new List<((int X, int Y) Cell, int SlotFromCell)>();

            for (var y = 0; y < height; y++)
            {
                for (var x = 0; x < width; x++)
                {
                    var cell = (X: x, Y: y);
                    if (layout.PinnedTiles.ContainsKey(cell)) continue;
                    if (transitionTiles.Contains(cell)) continue;
                    if (!IsFullySolidCell(corners, cell, parameters.SolidTerrain)) continue;
                    if (TileDoorGeometry.HasAnyCrosserEdge(crossers, cell)) continue; // must be a fresh dead end

                    for (var slot = 0; slot < 4; slot++)
                    {
                        var (dx, dy) = SlotOffsets[slot];
                        var neighbor = (X: cell.X + dx, Y: cell.Y + dy);
                        if (neighbor.X < 0 || neighbor.Y < 0 || neighbor.X >= width || neighbor.Y >= height) continue;
                        if (layout.PinnedTiles.ContainsKey(neighbor)) continue;
                        if (!IsFullySolidCell(corners, neighbor, parameters.SolidTerrain)) continue;

                        var backSlot = OppositeSlot(slot);
                        if (crossers.GetEdge(neighbor.X, neighbor.Y, backSlot).Length != 0) continue; // shared edge must still be blank

                        var neighborHasChain = false;
                        for (var s = 0; s < 4; s++)
                        {
                            if (s == backSlot) continue;
                            if (Eq(crossers.GetEdge(neighbor.X, neighbor.Y, s), crosser)) { neighborHasChain = true; break; }
                        }
                        if (!neighborHasChain) continue;

                        candidates.Add((cell, slot));
                    }
                }
            }

            Shuffle(candidates, random);

            foreach (var (cell, slotFromCell) in candidates)
            {
                for (var orientation = 0; orientation < 4; orientation++)
                {
                    var matches = true;
                    for (var s = 0; s < 4; s++)
                    {
                        var expected = s == slotFromCell ? crosser : string.Empty;
                        if (!Eq(tile.GetEdgeAt(orientation, s), expected)) { matches = false; break; }
                    }
                    if (!matches) continue;

                    crossers.SetEdge(cell.X, cell.Y, slotFromCell, crosser);
                    layout.PinnedTiles[cell] = (tile.TileId, orientation, 0);
                    return true;
                }
            }

            return false;
        }

        private static int OppositeSlot(int slot)
        {
            return slot switch
            {
                EdgeSlot.Top => EdgeSlot.Bottom,
                EdgeSlot.Bottom => EdgeSlot.Top,
                EdgeSlot.Left => EdgeSlot.Right,
                EdgeSlot.Right => EdgeSlot.Left,
                _ => slot
            };
        }

        // ---------------- WallRoom ----------------

        private static bool TryPlaceWallRoom(
            MacroLayout layout, MacroLayoutParameters parameters, ClassifiedGroup classified,
            System.Random random, ref int nextRoomId, bool openLaneBoundarySupported)
        {
            var group = classified.Group;
            var width = layout.Corners.Width;
            var height = layout.Corners.Height;

            var anchors = new List<(int X, int Y)>();
            for (var ay = 0; ay <= height - group.Rows; ay++)
            for (var ax = 0; ax <= width - group.Columns; ax++)
                anchors.Add((ax, ay));

            Shuffle(anchors, random);

            var transitionTiles = new HashSet<(int X, int Y)>(layout.Transitions.Select(t => t.Tile));

            foreach (var anchor in anchors)
            {
                if (!IsWallRoomSiteValid(layout, parameters, classified, anchor, transitionTiles, openLaneBoundarySupported))
                    continue;

                StampWallRoom(layout, parameters, classified, anchor, ref nextRoomId);
                return true;
            }

            return false;
        }

        /// <summary>
        /// Whole-tileset capability probe (see <see cref="TileResolver.HasCandidate"/>'s own "not for
        /// hot per-cell resolution" note -- this must be computed once per Stamp call, never per
        /// placement attempt) for whether a WallRoom's perimeter Doorway edge can resolve when it
        /// borders an OpenLane corridor/room cell instead of a Tunnel-mode chain cell (see
        /// IsWallRoomSiteValid's OpenLane branch below). This is the SAME boundary-tile shape
        /// TunnelVocabularyCheck.SupportsBoundaryShape already verifies for every ordinary room
        /// entrance door: near corners (shared with the WallRoom's own fully-solid footprint cell,
        /// guaranteed solid by IsWallRoomSiteValid's own IsFullySolidCell check) solid, far corners
        /// this layout's OpenTerrain (or SecondaryOpenTerrain) uniformly, port edge Doorway. Guards the
        /// OpenLane WallRoom site check from ever stamping a group next to a boundary shape TileResolver
        /// could never place a real tile for.
        /// </summary>
        private static bool SupportsWallRoomOpenLaneBoundary(TilesetModel tileset, MacroLayoutParameters parameters)
        {
            if (string.IsNullOrEmpty(parameters.SolidTerrain)) return false;

            bool Supports(string openTerrain) =>
                !string.IsNullOrEmpty(openTerrain) &&
                TileResolver.HasCandidate(
                    tileset, parameters.SolidTerrain, openTerrain, openTerrain, parameters.SolidTerrain,
                    string.Empty, string.Empty, string.Empty, DoorwayCrosser, parameters.DoorSlotCrossers);

            return Supports(parameters.OpenTerrain) || Supports(parameters.SecondaryOpenTerrain);
        }

        private static bool IsWallRoomSiteValid(
            MacroLayout layout, MacroLayoutParameters parameters, ClassifiedGroup classified,
            (int X, int Y) anchor, HashSet<(int X, int Y)> transitionTiles, bool openLaneBoundarySupported)
        {
            var group = classified.Group;
            var corners = layout.Corners;
            var crossers = layout.Crossers;
            var width = corners.Width;
            var height = corners.Height;

            for (var r = 0; r < group.Rows; r++)
            {
                for (var c = 0; c < group.Columns; c++)
                {
                    var cell = (X: anchor.X + c, Y: anchor.Y + r);

                    if (layout.PinnedTiles.ContainsKey(cell)) return false;
                    if (transitionTiles.Contains(cell)) return false;
                    // A hole slot is ordinary plan space, not a real member -- it carries no solidity
                    // or crosser-free requirement of its own (see TryClassify's hole handling).
                    if (IsHole(group, r, c)) continue;
                    if (!IsFullySolidCell(corners, cell, parameters.SolidTerrain)) return false;

                    for (var slot = 0; slot < 4; slot++)
                    {
                        if (crossers.GetEdge(cell.X, cell.Y, slot).Length != 0) return false;
                    }
                }
            }

            foreach (var (row, col, slot) in classified.PerimeterDoorways)
            {
                var cell = (X: anchor.X + col, Y: anchor.Y + row);
                var (dx, dy) = SlotOffsets[slot];
                var neighbor = (X: cell.X + dx, Y: cell.Y + dy);

                if (neighbor.X < 0 || neighbor.Y < 0 || neighbor.X >= width || neighbor.Y >= height) return false;

                var neighborHasCorridor = false;
                var neighborHasDoorway = false;
                for (var slot2 = 0; slot2 < 4; slot2++)
                {
                    var edge = crossers.GetEdge(neighbor.X, neighbor.Y, slot2);
                    if (IsCorridorTunnelBodyEdge(edge, parameters)) neighborHasCorridor = true;
                    if (IsDoorwayEdge(edge, parameters)) neighborHasDoorway = true;
                }

                if (neighborHasDoorway) return false; // keeps two WallRoom instances from claiming the same adapter cell

                // v1 site: a plain Tunnel-mode corridor chain cell (see LayoutTunnelCarver).
                if (neighborHasCorridor) continue;

                // OpenLane site: no wall-embedded tunnel chain exists in this mode (see
                // RoomsAndCorridorsLayout.CarveAllEdges' downgrade), so the network touch is instead a
                // solid-cornered WallRoom cell bordering a genuine open-lane/room boundary -- the same
                // shape SupportsWallRoomOpenLaneBoundary already verified the tileset can resolve.
                if (openLaneBoundarySupported && IsOpenLaneBoundaryNeighbor(corners, crossers, neighbor, slot, parameters))
                    continue;

                return false;
            }

            return true;
        }

        /// <summary>
        /// True when <paramref name="neighbor"/> (the cell across a WallRoom's perimeter Doorway edge,
        /// in the direction of <paramref name="slot"/>) is a genuine OpenLane boundary cell: currently
        /// crosser-free (so no other structure has already claimed it), and its two corners FAR from
        /// the WallRoom (its near corners are shared grid vertices with the WallRoom's own footprint
        /// cell, already guaranteed solid by the caller's IsFullySolidCell check) are both this
        /// layout's OpenTerrain, or both SecondaryOpenTerrain -- the exact shape
        /// SupportsWallRoomOpenLaneBoundary's capability probe verified is resolvable. A partial/mixed
        /// far side (e.g. only one far corner open, or the two far corners split across two different
        /// open terrains) is rejected: that is not the clean axis-aligned boundary shape the probe
        /// checked, and stamping it could leave an unresolvable cell for TileResolver later.
        /// </summary>
        private static bool IsOpenLaneBoundaryNeighbor(
            CornerTerrainGrid corners, EdgeCrosserGrid crossers, (int X, int Y) neighbor, int slot,
            MacroLayoutParameters parameters)
        {
            for (var s = 0; s < 4; s++)
            {
                if (crossers.GetEdge(neighbor.X, neighbor.Y, s).Length != 0) return false;
            }

            var (farA, farB) = FarCorners(corners, neighbor, slot);
            if (Eq(farA, parameters.OpenTerrain) && Eq(farB, parameters.OpenTerrain)) return true;
            if (!string.IsNullOrEmpty(parameters.SecondaryOpenTerrain) &&
                Eq(farA, parameters.SecondaryOpenTerrain) && Eq(farB, parameters.SecondaryOpenTerrain)) return true;

            return false;
        }

        /// <summary>
        /// The two corner-grid vertices of <paramref name="neighbor"/> NOT shared with the adjacent
        /// cell across <paramref name="slot"/> (e.g. slot == Right means neighbor sits to that cell's
        /// east, so neighbor's own east-side TR/BR vertices are "far" -- its west-side TL/BL vertices
        /// are the shared boundary with the caller). Mirrors WriteMember's own
        /// cell -> (TL, TR, BR, BL) = ((x,y+1), (x+1,y+1), (x+1,y), (x,y)) vertex convention.
        /// </summary>
        private static (string A, string B) FarCorners(CornerTerrainGrid corners, (int X, int Y) neighbor, int slot)
        {
            var nx = neighbor.X;
            var ny = neighbor.Y;
            return slot switch
            {
                EdgeSlot.Right => (corners.Labels[nx + 1, ny + 1], corners.Labels[nx + 1, ny]), // TR, BR
                EdgeSlot.Left => (corners.Labels[nx, ny + 1], corners.Labels[nx, ny]), // TL, BL
                EdgeSlot.Top => (corners.Labels[nx, ny + 1], corners.Labels[nx + 1, ny + 1]), // TL, TR
                EdgeSlot.Bottom => (corners.Labels[nx, ny], corners.Labels[nx + 1, ny]), // BL, BR
                _ => (string.Empty, string.Empty)
            };
        }

        // ---------------- CorridorStubChain ----------------

        /// <summary>
        /// Places a CorridorStubChain (e.g. Barrows/tbw01's CorridorDown_1x2/Corridor_Up_1x2/
        /// Corridor_Up_1x2_02, or Castle Interior 2/Fort Interior's Mythallar_3x3 4-way junction chamber):
        /// stamps the group's footprint into solid space exactly like TryPlaceWallRoom (same
        /// anchor-search/footprint-solid-and-crosser-free/StampWallRoom write path -- see StampWallRoom,
        /// which is generic and unaware of WHY a member carries an edge), but unlike WallRoom's
        /// Doorway-port-facing-a-body-crosser-neighbor pairing, this group's own perimeter opening(s)
        /// carry the composition's body crosser (Corridor/Alley/Custom-body) directly.
        ///
        /// A group can carry MORE than one perimeter body-crosser edge (Mythallar_3x3 offers all four
        /// cardinal sides as potential connections, unlike Barrows' single-anchor 1x2 groups) -- only
        /// ONE needs to actually splice onto an existing chain cell for the group to be reachable at
        /// all; the site search still requires every OTHER perimeter edge's own neighbor cell to be a
        /// real, available (solid/crosser-free/unpinned/non-transition) cell, so WriteMember's shared-
        /// edge write never lands on an already-claimed or out-of-bounds cell. Those OTHER edges are
        /// left "dangling" (no matching chain), which is only safe when SupportsAmbientCorridorDeadEnd's
        /// capability probe confirms the tileset has a genuine ordinary ungrouped, doorless, all-solid,
        /// single-crosser-edge tile for this exact body crosser -- the same shape every ordinary
        /// uncarved Tunnel-mode dead end already resolves via, so TileResolver can place a real tile for
        /// that neighbor cell later without this group ever having reserved/pinned it. A no-op (returns
        /// false, no grid mutation) when no chain of the required crosser exists yet, same as
        /// TryPlaceCorridorStub.
        /// </summary>
        private static bool TryPlaceCorridorStubChain(
            MacroLayout layout, MacroLayoutParameters parameters, TilesetModel tileset, ClassifiedGroup classified,
            System.Random random, ref int nextRoomId)
        {
            var group = classified.Group;
            var width = layout.Corners.Width;
            var height = layout.Corners.Height;

            // Computed once per call (TileResolver.HasCandidate rebuilds its lookup fresh each call, so
            // this must not run per placement attempt) -- see SupportsAmbientCorridorDeadEnd.
            var ambientDeadEndSupported = SupportsAmbientCorridorDeadEnd(tileset, parameters, classified.InsertCrosser);

            var anchors = new List<(int X, int Y)>();
            for (var ay = 0; ay <= height - group.Rows; ay++)
            for (var ax = 0; ax <= width - group.Columns; ax++)
                anchors.Add((ax, ay));

            Shuffle(anchors, random);

            var transitionTiles = new HashSet<(int X, int Y)>(layout.Transitions.Select(t => t.Tile));

            foreach (var anchor in anchors)
            {
                if (!IsCorridorStubChainSiteValid(layout, parameters, classified, anchor, transitionTiles, ambientDeadEndSupported))
                    continue;

                StampWallRoom(layout, parameters, classified, anchor, ref nextRoomId);
                return true;
            }

            return false;
        }

        /// <summary>
        /// Whole-tileset capability probe: true when the tileset has at least one ordinary ungrouped,
        /// doorless, all-solid-corner tile carrying exactly <paramref name="crosser"/> on ONE edge (the
        /// other three blank) -- the generic "not-yet-carved dead end" shape every plain uncarved
        /// Tunnel-mode chain cell already resolves via (LayoutTunnelCarver's own straight/turn/T/X
        /// vocabulary implies at least the crosser-presence half of this; this additionally confirms the
        /// specific single-edge dead-end shape TileResolver can actually place a tile for). Used by
        /// TryPlaceCorridorStubChain to allow a multi-opening group (e.g. Mythallar_3x3) to leave its
        /// UNCONNECTED perimeter openings dangling rather than requiring every one to already face an
        /// existing chain cell.
        /// </summary>
        private static bool SupportsAmbientCorridorDeadEnd(TilesetModel tileset, MacroLayoutParameters parameters, string crosser)
        {
            if (string.IsNullOrEmpty(crosser)) return false;
            return TileResolver.HasCandidate(
                tileset, parameters.SolidTerrain, parameters.SolidTerrain, parameters.SolidTerrain, parameters.SolidTerrain,
                crosser, string.Empty, string.Empty, string.Empty);
        }

        private static bool IsCorridorStubChainSiteValid(
            MacroLayout layout, MacroLayoutParameters parameters, ClassifiedGroup classified,
            (int X, int Y) anchor, HashSet<(int X, int Y)> transitionTiles, bool ambientDeadEndSupported)
        {
            var group = classified.Group;
            var corners = layout.Corners;
            var crossers = layout.Crossers;
            var width = corners.Width;
            var height = corners.Height;

            for (var r = 0; r < group.Rows; r++)
            {
                for (var c = 0; c < group.Columns; c++)
                {
                    var cell = (X: anchor.X + c, Y: anchor.Y + r);

                    if (layout.PinnedTiles.ContainsKey(cell)) return false;
                    if (transitionTiles.Contains(cell)) return false;
                    if (IsHole(group, r, c)) continue; // ordinary plan space, not a real member
                    if (!IsFullySolidCell(corners, cell, parameters.SolidTerrain)) return false;

                    for (var slot = 0; slot < 4; slot++)
                    {
                        if (crossers.GetEdge(cell.X, cell.Y, slot).Length != 0) return false;
                    }
                }
            }

            var connectedAny = false;
            foreach (var (row, col, slot) in classified.PerimeterBodyCrossers)
            {
                var cell = (X: anchor.X + col, Y: anchor.Y + row);
                var (dx, dy) = SlotOffsets[slot];
                var neighbor = (X: cell.X + dx, Y: cell.Y + dy);

                if (neighbor.X < 0 || neighbor.Y < 0 || neighbor.X >= width || neighbor.Y >= height) return false;

                var backSlot = OppositeSlot(slot);
                if (crossers.GetEdge(neighbor.X, neighbor.Y, backSlot).Length != 0) return false; // shared edge must still be blank

                var neighborHasChain = false;
                for (var s = 0; s < 4; s++)
                {
                    if (s == backSlot) continue;
                    if (Eq(crossers.GetEdge(neighbor.X, neighbor.Y, s), classified.InsertCrosser)) { neighborHasChain = true; break; }
                }

                if (neighborHasChain)
                {
                    connectedAny = true;
                    continue;
                }

                // Not an existing chain -- this opening would dangle. Only safe when the tileset can
                // resolve an ordinary ambient dead end for this crosser AND the neighbor cell itself is
                // still real, untouched plan space (never claimed by anything else) -- see
                // SupportsAmbientCorridorDeadEnd's own doc comment.
                if (!ambientDeadEndSupported) return false;
                if (layout.PinnedTiles.ContainsKey(neighbor)) return false;
                if (transitionTiles.Contains(neighbor)) return false;
                if (!IsFullySolidCell(corners, neighbor, parameters.SolidTerrain)) return false;
            }

            // At least one real connection to the existing network is required -- an all-dangling group
            // would be an isolated, unreachable pocket.
            return connectedAny;
        }

        private static void StampWallRoom(
            MacroLayout layout, MacroLayoutParameters parameters, ClassifiedGroup classified,
            (int X, int Y) anchor, ref int nextRoomId)
        {
            var footprint = new List<(int X, int Y)>();

            foreach (var member in classified.Members)
            {
                var cell = (X: anchor.X + member.LocalCol, Y: anchor.Y + member.LocalRow);
                footprint.Add(cell);
                WriteMember(layout, parameters, member.Tile, cell);
            }

            layout.Rooms.Add(new LayoutRoom
            {
                Id = nextRoomId++,
                Role = RoomRole.Standard,
                IsSetPiece = true,
                CenterTile = footprint[0],
                Tiles = footprint
            });
        }

        // ---------------- WallAlcove ----------------

        /// <summary>
        /// Places a WallAlcove (e.g. vmr01 "Room 1 2x2".."Room 5 2x2"): stamps the group's footprint
        /// into solid space exactly like TryPlaceWallRoom, but since this shape carries no Doorway
        /// crosser vocabulary of its own (see TryClassify), the site requirement is: at least one of
        /// the DOOR-CARRYING MEMBER'S OWN perimeter edges (see ClassifiedGroup.DoorMemberPerimeterEdges)
        /// touches the reachable network — either a fully-open room cell across its FULL shared edge
        /// (this layout's OpenTerrain or, when districts are active, SecondaryOpenTerrain) or an
        /// existing Corridor/Alley tunnel-chain cell — instead of v1's "any footprint cell, any side"
        /// rule. See IsWallAlcoveSiteValid's own doc comment for why this is the right (and the most
        /// this shape can ever support) notion of "aligned": the door slot itself always sits on the
        /// seam shared with ANOTHER member of the SAME footprint (verified against TileGroupRecord's
        /// row0=south pin and against the real vmr01 "Room 5 2x2" placement in the hand-built
        /// spacenarshaddung area), so no orientation or site choice can ever put the door's own local
        /// facing edge against a cell outside the group -- only the door-carrying MEMBER's position can
        /// be steered next to real reachable space. No door object is ever spawned here (matching the
        /// CorridorInsert/OpenSetPiece-with-tolerated-doors precedent, and matching hand-built usage,
        /// where the slot is instead populated by a bespoke, hand-authored area-transition Door with no
        /// relationship to grid adjacency -- content this generator has no linked-area counterpart for).
        /// </summary>
        private static bool TryPlaceWallAlcove(
            MacroLayout layout, MacroLayoutParameters parameters, ClassifiedGroup classified,
            System.Random random, ref int nextRoomId)
        {
            var group = classified.Group;
            var width = layout.Corners.Width;
            var height = layout.Corners.Height;

            var anchors = new List<(int X, int Y)>();
            for (var ay = 0; ay <= height - group.Rows; ay++)
            for (var ax = 0; ax <= width - group.Columns; ax++)
                anchors.Add((ax, ay));

            Shuffle(anchors, random);

            var transitionTiles = new HashSet<(int X, int Y)>(layout.Transitions.Select(t => t.Tile));

            foreach (var anchor in anchors)
            {
                if (!IsWallAlcoveSiteValid(layout, parameters, classified, anchor, transitionTiles))
                    continue;

                StampWallRoom(layout, parameters, classified, anchor, ref nextRoomId);
                return true;
            }

            return false;
        }

        /// <summary>
        /// Validates the footprint exactly like v1 did (legal, unclaimed, fully-solid-and-crosser-free
        /// space, and the same lenient "neighbor carries at least one corner of the open terrain"
        /// touch tolerance -- both unchanged below, see CellHasAnyCornerOfTerrain's own doc comment for
        /// why a single corner is the correct tolerance for a mixed wall-ring boundary), but tightens
        /// WHICH cell that touch is measured against: instead of accepting ANY footprint member's ANY
        /// side touching the network (v1 -- which could stamp a real, decorated alcove flush against
        /// the network by an unrelated bland corner while its OWN door-carrying member sat buried in
        /// solid mass on every side, reading as a sealed floating box), this requires the DOOR-CARRYING
        /// MEMBER's OWN perimeter edge (see ClassifiedGroup.DoorMemberPerimeterEdges) specifically to be
        /// the one touching. This is deliberately NOT "the door slot's own local facing direction
        /// touches the network" -- that is structurally impossible for every verified WallAlcove shape
        /// (vmr01's "Room 1-5 2x2": the door-carrying member is always the group's OTHER-row corner
        /// tile, and its door sits on the seam shared with the group's own opposite-row member, not any
        /// footprint-external cell, at ANY orientation -- confirmed against the hand-built
        /// "spacenarshaddung" area, which places "Room 5 2x2" unrotated and populates that exact slot
        /// with a bespoke area-transition Door object, not a grid-adjacent connector). What IS
        /// achievable, and what this enforces, is that the SPECIAL member (the one carrying the
        /// alcove's ornamental door slot) is the one sitting next to real reachable space, rather than
        /// an arbitrary other member of the same box.
        /// </summary>
        private static bool IsWallAlcoveSiteValid(
            MacroLayout layout, MacroLayoutParameters parameters, ClassifiedGroup classified,
            (int X, int Y) anchor, HashSet<(int X, int Y)> transitionTiles)
        {
            var group = classified.Group;
            var corners = layout.Corners;
            var crossers = layout.Crossers;
            var width = corners.Width;
            var height = corners.Height;

            for (var r = 0; r < group.Rows; r++)
            {
                for (var c = 0; c < group.Columns; c++)
                {
                    var cell = (X: anchor.X + c, Y: anchor.Y + r);

                    if (layout.PinnedTiles.ContainsKey(cell)) return false;
                    if (transitionTiles.Contains(cell)) return false;
                    // A hole slot is ordinary plan space, not a real member -- it carries no solidity
                    // or crosser-free requirement of its own (see TryClassify's hole handling).
                    if (IsHole(group, r, c)) continue;
                    if (!IsFullySolidCell(corners, cell, parameters.SolidTerrain)) return false;

                    for (var slot = 0; slot < 4; slot++)
                    {
                        if (crossers.GetEdge(cell.X, cell.Y, slot).Length != 0) return false;
                    }
                }
            }

            if (classified.DoorMemberPerimeterEdges == null || classified.DoorMemberPerimeterEdges.Count == 0)
                return false; // the door-carrying member has no footprint-external edge at all (e.g. a
                              // fully-interior member of a 3x3+ group) -- no site can ever satisfy this
                              // group's own shape, so it is correctly never placed rather than guessed at.

            foreach (var (row, col, slot) in classified.DoorMemberPerimeterEdges)
            {
                var cell = (X: anchor.X + col, Y: anchor.Y + row);
                var (dx, dy) = SlotOffsets[slot];
                var neighbor = (X: cell.X + dx, Y: cell.Y + dy);
                if (neighbor.X < 0 || neighbor.Y < 0 || neighbor.X >= width || neighbor.Y >= height) continue;

                // Matches v1's own touch tolerance (see the class doc comment above): a room's own
                // interior cells are always separated from untouched solid mass by at least one
                // "mixed" boundary cell (open on the room-facing side, solid on the far side), so
                // requiring only that the neighbor carries at least one corner of the open terrain --
                // not a full matching edge -- correctly matches that mixed boundary ring. The
                // improvement over v1 is WHICH cell this is checked against (the door-carrying
                // member's own perimeter neighbor only, not any of the other members'), not how
                // lenient the terrain match itself is.
                if (CellHasAnyCornerOfTerrain(corners, neighbor, parameters.OpenTerrain)) return true;
                if (!string.IsNullOrEmpty(parameters.SecondaryOpenTerrain) &&
                    CellHasAnyCornerOfTerrain(corners, neighbor, parameters.SecondaryOpenTerrain)) return true;

                for (var s = 0; s < 4; s++)
                {
                    var edge = crossers.GetEdge(neighbor.X, neighbor.Y, s);
                    if (Eq(edge, CorridorCrosser) || Eq(edge, AlleyCrosser)) return true;
                    if (parameters.CorridorCrosserType == CorridorCrosserType.Custom &&
                        !string.IsNullOrEmpty(parameters.TunnelBodyCrosser) &&
                        Eq(edge, parameters.TunnelBodyCrosser)) return true;
                }
            }

            return false;
        }

        // ---------------- OpenSetPiece ----------------

        private static bool TryPlaceOpenSetPiece(
            MacroLayout layout, MacroLayoutParameters parameters, ClassifiedGroup classified, System.Random random)
        {
            var group = classified.Group;

            var siteCandidates = new List<(LayoutRoom Room, (int X, int Y) Anchor)>();
            // District-aware: only rooms carved from this piece's own open terrain are eligible (see
            // ClassifiedGroup.OpenSetPieceTerrain) — a room's OpenTerrain is always populated by every
            // layout style's room-building path, and equals parameters.OpenTerrain everywhere districts
            // are inactive, so this is a no-op filter in the single-terrain case.
            foreach (var room in layout.Rooms.Where(r => !r.IsSetPiece && Eq(r.OpenTerrain, classified.OpenSetPieceTerrain)).OrderBy(r => r.Id))
            {
                foreach (var anchor in room.Tiles.OrderBy(t => t.Y).ThenBy(t => t.X))
                    siteCandidates.Add((room, anchor));
            }

            Shuffle(siteCandidates, random);

            // Tiered preference: on a road-declaring composition (LayoutRoadCarver.CarveRoads has
            // already run -- see MacroLayoutGenerator.Generate's ordering), prefer a valid site whose
            // footprint fronts an already-carved Road lane (roadAdjacent -- see
            // IsOpenSetPieceSiteValid), matching the hand-built fcx01 pattern of buildings fronting
            // streets. On a contiguous-block composition (see MacroLayoutParameters.
            // BuildingBlockContiguity) the preference is additionally scored on whether the footprint
            // ADJOINS an already-stamped building (buildingAdjacent): street-fronting AND
            // block-forming first (canyon walls along streets, the hand-built promenade-family
            // pattern), then street-fronting, then block-forming, then free-standing. With the knob
            // off, buildingAdjacent is always false and this reduces exactly to the original two-pass
            // behavior (commit on first road-adjacent site, else first valid site of any kind) with
            // zero extra RNG draws -- every non-city tileset is unaffected. A building that lands
            // without road frontage gets a connector spur afterward -- see LayoutRoadCarver.CarveSpurs.
            // Inert at or below the 20x20 tuning baseline, exactly like SetPieceRoomSupplyScaling
            // and EffectiveMaxCount's area scale (see DungeonTilesetProfile.BuildingBlockContiguity):
            // baseline-size compositions keep the pre-mechanism placement byte-for-byte -- their
            // budgets AND the urban dressing-density gates are tuned against 20x20 evidence, and
            // block assembly there measurably starved the street-margin dressing pools (packed20
            // realized density 0.92-1.14 per total tile vs the 1.2-1.35 hand-built band).
            HashSet<(int X, int Y)> stampedCells = null;
            if (parameters.BuildingBlockContiguity &&
                parameters.Width * parameters.Height > LayoutParameterConstraints.RoomSupplyBaselineTiles)
            {
                stampedCells = new HashSet<(int X, int Y)>();
                foreach (var stampedFootprint in layout.StampedOpenSetPieceFootprints)
                foreach (var cell in stampedFootprint)
                    stampedCells.Add(cell);
            }

            var topScore = stampedCells != null ? 3 : 2;
            var bestScore = -1;
            (LayoutRoom Room, List<(int X, int Y)> Footprint, (int X, int Y)? RelocatedCenter)? fallback = null;

            foreach (var (room, anchor) in siteCandidates)
            {
                if (!IsOpenSetPieceSiteValid(layout, parameters, room, classified, anchor, stampedCells,
                        out var footprint, out var relocatedCenter, out var roadAdjacent, out var buildingAdjacent))
                    continue;

                var score = (roadAdjacent ? 2 : 0) + (buildingAdjacent ? 1 : 0);
                if (score == topScore)
                {
                    CommitOpenSetPiece(layout, parameters, classified, room, footprint, relocatedCenter);
                    return true;
                }

                if (score > bestScore)
                {
                    bestScore = score;
                    fallback = (room, footprint, relocatedCenter);
                }
            }

            if (fallback.HasValue)
            {
                CommitOpenSetPiece(layout, parameters, classified, fallback.Value.Room, fallback.Value.Footprint, fallback.Value.RelocatedCenter);
                return true;
            }

            return false;
        }

        /// <summary>Commits a validated OpenSetPiece site: writes the tiles, records the footprint for
        /// LayoutRoadCarver.CarveSpurs, and relocates the room's CenterTile when the site required it.
        /// See IsOpenSetPieceSiteValid's own doc comment: relocating happens AFTER every other pass that
        /// reads/reserves CenterTile has already run (Stamp runs after CarveRoads and before
        /// ValidateInvariants -- see MacroLayoutGenerator.Generate's ordering), so no earlier pass ever
        /// observes a stale center.</summary>
        private static void CommitOpenSetPiece(
            MacroLayout layout, MacroLayoutParameters parameters, ClassifiedGroup classified, LayoutRoom room,
            List<(int X, int Y)> footprint, (int X, int Y)? relocatedCenter)
        {
            StampOpenSetPiece(layout, parameters, classified, room, footprint);
            layout.StampedOpenSetPieceFootprints.Add(footprint);
            if (relocatedCenter.HasValue)
                room.CenterTile = relocatedCenter.Value;
        }

        /// <summary>
        /// Footprint plus a 1-cell margin ring must sit entirely inside this same room's open tiles,
        /// and touch neither a transition anchor nor an already-pinned cell (an earlier set piece
        /// instance). The room's own CenterTile no longer blocks a site outright: on real Halls/
        /// Complex-carved rooms (3-6 corners, i.e. 2x2..5x5 tiles) a 2x2+ footprint plus its margin
        /// consumes most or all of the room's interior, so the reserved center is almost always
        /// somewhere inside that rectangle -- treating it as an unconditional exclusion left
        /// TryPlaceOpenSetPiece with essentially 0 viable sites on every shipped tileset (verified:
        /// 0/100 across vmr01/tdm01/ttf01-Forest/ttf01-ForestPlatform x Halls/Complex, see
        /// OpenSetPiecePlacementRateTests). Instead, when a candidate site's extended (footprint +
        /// margin) rectangle would consume the CURRENT CenterTile, this looks for another fully-open
        /// room tile (still equal to the room's own OpenTerrain on all 4 corners -- excluding any tile
        /// already repainted by LayoutAccentPainter/LayoutFenceCarver/LayoutElevationPainter/
        /// LayoutElevationPoolPainter/LayoutReliefPainter, all of which already ran and already treated
        /// the OLD center as their own forbidden/protected cell — see those passes' own CenterTile
        /// doc comments) outside the extended rectangle to become the room's new representative center.
        /// CenterTile is read afterward only by AreaSynthesizer's connectivity walk and
        /// DungeonContentPlacer's spawn/objective anchor (see AreaLayout.cs's own doc comment) -- both
        /// consume the FINAL layout returned from MacroLayoutGenerator.Generate, after Stamp has
        /// already run, so a relocation here is never observed as stale by an earlier pass. If no
        /// such alternate tile exists (the footprint + margin would consume the room's entire open
        /// interior), the site is rejected exactly as before -- there would be nothing left to anchor
        /// the room's own connectivity/spawn point to.
        ///
        /// Two additions for road-declaring compositions (parameters.RoadCrosser set -- see
        /// LayoutRoadCarver, which now runs BEFORE Stamp): (1) a footprint cell that already carries the
        /// Road crosser on any edge rejects the site outright -- WriteMember rewrites every edge of its
        /// own footprint cells, so stamping over a carved lane would silently erase it. (2)
        /// <paramref name="roadAdjacent"/> reports whether the footprint's OWN 1-cell margin ring (not
        /// the footprint itself) touches a Road-crossed cell, letting TryPlaceOpenSetPiece prefer a
        /// street-fronting site -- see that method's own doc comment. Both are no-ops (roadAdjacent
        /// always false, no extra rejection) when RoadCrosser is empty, so every non-city tileset is
        /// unaffected.
        ///
        /// Contiguous-block mode (<paramref name="stampedCells"/> non-null, i.e.
        /// MacroLayoutParameters.BuildingBlockContiguity): a margin-ring cell occupied by an earlier
        /// stamped OpenSetPiece footprint no longer rejects the site -- buildings may adjoin into
        /// blocks -- subject to seam label agreement (StampSeamsAgree), the hand-built block-size cap
        /// (MaxContiguousBlockTiles), and room-split protection (CountRoomComponents); orthogonal
        /// contact is reported via <paramref name="buildingAdjacent"/> for TryPlaceOpenSetPiece's
        /// tiered street-fronting/block-forming preference. With the knob off, stampedCells is null,
        /// buildingAdjacent is always false, and every check reduces to the pre-mechanism behavior.
        /// </summary>
        private static bool IsOpenSetPieceSiteValid(
            MacroLayout layout, MacroLayoutParameters parameters, LayoutRoom room, ClassifiedGroup classified, (int X, int Y) anchor,
            HashSet<(int X, int Y)> stampedCells,
            out List<(int X, int Y)> footprint, out (int X, int Y)? relocatedCenter, out bool roadAdjacent, out bool buildingAdjacent)
        {
            footprint = null;
            relocatedCenter = null;
            roadAdjacent = false;
            buildingAdjacent = false;

            var group = classified.Group;
            var roomTiles = new HashSet<(int X, int Y)>(room.Tiles);
            var transitionTiles = new HashSet<(int X, int Y)>(layout.Transitions.Select(t => t.Tile));
            var roadCrosser = parameters.RoadCrosser;

            var fp = new List<(int X, int Y)>();
            for (var r = 0; r < group.Rows; r++)
            for (var c = 0; c < group.Columns; c++)
                fp.Add((anchor.X + c, anchor.Y + r));
            var fpSet = new HashSet<(int X, int Y)>(fp);

            // Road exclusion/adjacency reads layout.Crossers, which is only safely indexable for real
            // grid cells -- both checks below live INSIDE the extended loop, after roomTiles.Contains
            // has already confirmed the cell is a real (in-bounds) room tile, exactly like every other
            // per-cell check here (transitionTiles/PinnedTiles/CenterTile).
            var extended = new List<(int X, int Y)>();
            var touchesCenter = false;
            var touchesStamped = false;
            for (var y = anchor.Y - 1; y <= anchor.Y + group.Rows; y++)
            {
                for (var x = anchor.X - 1; x <= anchor.X + group.Columns; x++)
                {
                    var cell = (X: x, Y: y);

                    // Contiguous-block mode only (stampedCells non-null): a margin-ring cell already
                    // occupied by an earlier stamped OpenSetPiece footprint is an allowed seam, not a
                    // rejection -- the new footprint may adjoin the existing building. The footprint
                    // itself must never overlap one (that would overwrite stamped tiles). Seam
                    // compatibility (corner labels + edge crossers) is verified after the loop. Only
                    // ORTHOGONAL contact (the ring cell shares an edge with the footprint rectangle,
                    // i.e. it is not one of the 4 diagonal ring corners) counts as block adjacency,
                    // matching the hand-built contiguity measurement's orthogonal blocks.
                    if (stampedCells != null && stampedCells.Contains(cell))
                    {
                        if (fpSet.Contains(cell)) return false;
                        touchesStamped = true;
                        var xInside = x >= anchor.X && x < anchor.X + group.Columns;
                        var yInside = y >= anchor.Y && y < anchor.Y + group.Rows;
                        if (xInside || yInside) buildingAdjacent = true;
                        continue;
                    }

                    if (!roomTiles.Contains(cell)) return false;
                    if (transitionTiles.Contains(cell)) return false;
                    if (layout.PinnedTiles.ContainsKey(cell)) return false;
                    if (cell == room.CenterTile) touchesCenter = true;
                    extended.Add(cell);

                    if (!string.IsNullOrEmpty(roadCrosser))
                    {
                        var cellHasRoadEdge = false;
                        for (var slot = 0; slot < 4; slot++)
                        {
                            if (Eq(layout.Crossers.GetEdge(cell.X, cell.Y, slot), roadCrosser)) { cellHasRoadEdge = true; break; }
                        }

                        if (cellHasRoadEdge)
                        {
                            // A footprint cell that already carries a Road edge would have that edge
                            // silently overwritten by WriteMember -- reject the whole site. A margin
                            // (ring) cell carrying one instead means this footprint FRONTS a carved lane.
                            if (fpSet.Contains(cell)) return false;
                            roadAdjacent = true;
                        }
                    }
                }
            }

            // Contiguous-block seam verification: every corner label and edge crosser the new stamp
            // would write onto a boundary shared with an already-stamped building must EQUAL what the
            // earlier stamp already wrote there -- WriteMember overwrites shared grid slots
            // last-write-wins, so a disagreeing seam would silently repaint the existing building's
            // face (and a corner disagreement changes what an unpinned neighbor cell later resolves
            // against). Hand-built promenade-family blocks satisfy this at every seam (the fcx01
            // tower groups carry uniform open-cornered, crosser-free perimeter faces), so requiring
            // agreement never blocks the intended pattern -- it only blocks visually incompatible
            // pairings (e.g. a solid-cornered platform face against an open-cornered tower face).
            if (touchesStamped && !StampSeamsAgree(layout, parameters, classified, anchor, stampedCells, fpSet))
                return false;

            // Contiguous-block size cap: hand-built promenade-family blocks top out at 48 contiguous
            // building tiles (narshadaar_promi's largest; the Cobble-district areas' largest is 30) --
            // an uncapped adjacency chain measured merged blocks of 99-126 tiles, walling off far more
            // of the area than any hand-built reference does. A site whose adjacency would grow the
            // merged orthogonal block past the hand-built ceiling is rejected, redistributing that
            // group instance to a different room/block instead.
            if (buildingAdjacent &&
                MergedBlockSize(fpSet, stampedCells) > MaxContiguousBlockTiles)
                return false;

            // Contiguous-block room-split protection: adjoined masses can form L/U shapes that pocket
            // off part of the room's remaining open interior (the isolated-margin rule guaranteed a
            // walkable ring around every stamp, so this could not previously happen inside a room).
            // Reject any site whose consumption would INCREASE the number of orthogonally-connected
            // components of the room's remaining tiles -- transitions, the (possibly relocated)
            // center, and street continuity through the room all stay reachable exactly as before.
            if (stampedCells != null &&
                CountRoomComponents(room.Tiles, fpSet) > CountRoomComponents(room.Tiles, null))
                return false;

            if (touchesCenter)
            {
                var extendedSet = new HashSet<(int X, int Y)>(extended);
                var relocationCandidates = room.Tiles
                    .Where(t => !extendedSet.Contains(t))
                    .Where(t => !transitionTiles.Contains(t) && !layout.PinnedTiles.ContainsKey(t))
                    .Where(t => IsFullyOpenCell(layout.Corners, t, room.OpenTerrain))
                    .ToList();
                if (relocationCandidates.Count == 0) return false;

                // Deterministic given the seed-derived room/tile ordering above -- nearest to the
                // original center keeps the relocated anchor representative of the room, and ties
                // break on the stable room.Tiles order (no extra RNG draw, so this never perturbs the
                // seed's RNG sequence for anything placed afterward).
                relocatedCenter = relocationCandidates
                    .OrderBy(t => Math.Abs(t.X - room.CenterTile.X) + Math.Abs(t.Y - room.CenterTile.Y))
                    .First();
            }

            footprint = fp;
            return true;
        }

        private static void StampOpenSetPiece(
            MacroLayout layout, MacroLayoutParameters parameters, ClassifiedGroup classified,
            LayoutRoom room, List<(int X, int Y)> footprint)
        {
            foreach (var member in classified.Members)
            {
                var cell = footprint[member.LocalRow * classified.Group.Columns + member.LocalCol];
                WriteMember(layout, parameters, member.Tile, cell);
            }

            var footprintSet = new HashSet<(int X, int Y)>(footprint);
            room.Tiles = room.Tiles.Where(t => !footprintSet.Contains(t)).ToList();
        }

        /// <summary>
        /// True when every corner label and edge crosser the candidate OpenSetPiece stamp would write
        /// onto a boundary it shares with an already-stamped OpenSetPiece cell agrees with the value
        /// already present in the shared grids -- see the call site in IsOpenSetPieceSiteValid for why
        /// agreement is required. Corner writes are simulated in the same member order
        /// StampOpenSetPiece/WriteMember would apply them (last-write-wins for a group's own interior
        /// corners), then every simulated corner that also belongs to a stamped cell outside the
        /// footprint (orthogonal OR diagonal contact -- a diagonal neighbor shares exactly one corner
        /// point) is compared against the existing label. Edges are compared per shared orthogonal
        /// slot. Comparison is case-insensitive (Eq), matching every other label comparison here.
        /// </summary>
        private static bool StampSeamsAgree(
            MacroLayout layout, MacroLayoutParameters parameters, ClassifiedGroup classified,
            (int X, int Y) anchor, HashSet<(int X, int Y)> stampedCells, HashSet<(int X, int Y)> fpSet)
        {
            // Edge agreement on shared orthogonal slots.
            foreach (var member in classified.Members)
            {
                var cell = (X: anchor.X + member.LocalCol, Y: anchor.Y + member.LocalRow);
                for (var slot = 0; slot < 4; slot++)
                {
                    var (dx, dy) = SlotOffsets[slot];
                    var neighbor = (X: cell.X + dx, Y: cell.Y + dy);
                    if (fpSet.Contains(neighbor) || !stampedCells.Contains(neighbor)) continue;

                    if (!Eq(member.Tile.GetEdgeAt(0, slot), layout.Crossers.GetEdge(cell.X, cell.Y, slot)))
                        return false;
                }
            }

            // Corner agreement: simulate the stamp's corner writes, then compare every written corner
            // that any stamped (non-footprint) cell also owns.
            var writes = new Dictionary<(int X, int Y), string>();
            foreach (var member in classified.Members)
            {
                var cell = (X: anchor.X + member.LocalCol, Y: anchor.Y + member.LocalRow);
                writes[(cell.X, cell.Y + 1)] = Canonicalize(member.Tile.GetCornerAt(0, CornerSlot.TopLeft), parameters);
                writes[(cell.X + 1, cell.Y + 1)] = Canonicalize(member.Tile.GetCornerAt(0, CornerSlot.TopRight), parameters);
                writes[(cell.X + 1, cell.Y)] = Canonicalize(member.Tile.GetCornerAt(0, CornerSlot.BottomRight), parameters);
                writes[(cell.X, cell.Y)] = Canonicalize(member.Tile.GetCornerAt(0, CornerSlot.BottomLeft), parameters);
            }

            foreach (var ((cx, cy), label) in writes)
            {
                var sharedWithStamped = false;
                for (var dy = -1; dy <= 0 && !sharedWithStamped; dy++)
                for (var dx = -1; dx <= 0 && !sharedWithStamped; dx++)
                {
                    var owner = (X: cx + dx, Y: cy + dy);
                    if (!fpSet.Contains(owner) && stampedCells.Contains(owner))
                        sharedWithStamped = true;
                }

                if (sharedWithStamped && !Eq(layout.Corners.Labels[cx, cy], label))
                    return false;
            }

            return true;
        }

        /// <summary>
        /// Hand-built promenade-family ceiling on one contiguous building block's tile count -- see
        /// the cap check in IsOpenSetPieceSiteValid (largest measured hand-built block is 48 tiles;
        /// the largest single fcx01 group footprint is 36).
        /// </summary>
        private const int MaxContiguousBlockTiles = 48;

        /// <summary>
        /// Size of the contiguous orthogonal building block the candidate footprint would merge into:
        /// BFS from the footprint cells over footprint-plus-stamped-building cells. Used by the
        /// contiguous-block size cap in IsOpenSetPieceSiteValid.
        /// </summary>
        private static int MergedBlockSize(HashSet<(int X, int Y)> fpSet, HashSet<(int X, int Y)> stampedCells)
        {
            var seen = new HashSet<(int X, int Y)>(fpSet);
            var queue = new Queue<(int X, int Y)>(fpSet);
            while (queue.Count > 0)
            {
                var (x, y) = queue.Dequeue();
                foreach (var (dx, dy) in SlotOffsets)
                {
                    var next = (X: x + dx, Y: y + dy);
                    if (!seen.Contains(next) && stampedCells.Contains(next))
                    {
                        seen.Add(next);
                        queue.Enqueue(next);
                    }
                }
            }

            return seen.Count;
        }

        /// <summary>
        /// Number of orthogonally-connected components of <paramref name="roomTiles"/> after removing
        /// <paramref name="exclude"/> (null = remove nothing). Used by the contiguous-block room-split
        /// check in IsOpenSetPieceSiteValid; room tile lists are small (a corner-size-11 room is 100
        /// tiles), so a plain BFS per candidate site is cheap.
        /// </summary>
        private static int CountRoomComponents(List<(int X, int Y)> roomTiles, HashSet<(int X, int Y)> exclude)
        {
            var remaining = new HashSet<(int X, int Y)>(roomTiles);
            if (exclude != null) remaining.ExceptWith(exclude);

            var seen = new HashSet<(int X, int Y)>();
            var queue = new Queue<(int X, int Y)>();
            var components = 0;
            foreach (var start in remaining)
            {
                if (seen.Contains(start)) continue;
                components++;
                seen.Add(start);
                queue.Enqueue(start);
                while (queue.Count > 0)
                {
                    var (x, y) = queue.Dequeue();
                    foreach (var (dx, dy) in SlotOffsets)
                    {
                        var next = (X: x + dx, Y: y + dy);
                        if (remaining.Contains(next) && seen.Add(next))
                            queue.Enqueue(next);
                    }
                }
            }

            return components;
        }

        // ---------------- shared write helpers ----------------

        /// <summary>
        /// Writes one member tile's corners and all 4 edges into the shared grids at orientation 0,
        /// then pins the cell verbatim. Corner/edge writes for a multi-cell group's INTERIOR shared
        /// boundary may disagree between the two flanking members (the real zsf01 "Bedroom" group
        /// does — both members carry the same raw Doorway-on-Top edge data) but that is harmless: both
        /// flanking cells are pinned, so neither is ever read back via corner/edge key lookup. Only
        /// PERIMETER writes (read by an unpinned neighbor's own key) need to be correct, and those are
        /// exclusive per shared grid slot, so last-write-wins for interior slots never touches them.
        /// </summary>
        private static void WriteMember(MacroLayout layout, MacroLayoutParameters parameters, TileRecord tile, (int X, int Y) cell)
        {
            var tl = Canonicalize(tile.GetCornerAt(0, CornerSlot.TopLeft), parameters);
            var tr = Canonicalize(tile.GetCornerAt(0, CornerSlot.TopRight), parameters);
            var br = Canonicalize(tile.GetCornerAt(0, CornerSlot.BottomRight), parameters);
            var bl = Canonicalize(tile.GetCornerAt(0, CornerSlot.BottomLeft), parameters);

            layout.Corners.Labels[cell.X, cell.Y + 1] = tl;
            layout.Corners.Labels[cell.X + 1, cell.Y + 1] = tr;
            layout.Corners.Labels[cell.X + 1, cell.Y] = br;
            layout.Corners.Labels[cell.X, cell.Y] = bl;

            for (var slot = 0; slot < 4; slot++)
                layout.Crossers.SetEdge(cell.X, cell.Y, slot, tile.GetEdgeAt(0, slot));

            layout.PinnedTiles[cell] = (tile.TileId, 0, 0);
        }

        /// <summary>Normalizes a .set corner label's casing to match the layout's own solid/open terrain strings exactly, so downstream exact-string comparisons never trip on a source file's casing quirks.</summary>
        private static string Canonicalize(string label, MacroLayoutParameters parameters)
        {
            if (Eq(label, parameters.SolidTerrain)) return parameters.SolidTerrain;
            if (Eq(label, parameters.OpenTerrain)) return parameters.OpenTerrain;
            if (!string.IsNullOrEmpty(parameters.SecondaryOpenTerrain) && Eq(label, parameters.SecondaryOpenTerrain))
                return parameters.SecondaryOpenTerrain;
            return label;
        }

        private static bool IsFullySolidCell(CornerTerrainGrid corners, (int X, int Y) cell, string solidTerrain)
        {
            return Eq(corners.Labels[cell.X, cell.Y], solidTerrain) &&
                   Eq(corners.Labels[cell.X + 1, cell.Y], solidTerrain) &&
                   Eq(corners.Labels[cell.X, cell.Y + 1], solidTerrain) &&
                   Eq(corners.Labels[cell.X + 1, cell.Y + 1], solidTerrain) &&
                   TileDoorGeometry.IsFlatCell(corners, cell.X, cell.Y);
        }

        private static bool IsFullyOpenCell(CornerTerrainGrid corners, (int X, int Y) cell, string openTerrain)
        {
            return Eq(corners.Labels[cell.X, cell.Y], openTerrain) &&
                   Eq(corners.Labels[cell.X + 1, cell.Y], openTerrain) &&
                   Eq(corners.Labels[cell.X, cell.Y + 1], openTerrain) &&
                   Eq(corners.Labels[cell.X + 1, cell.Y + 1], openTerrain) &&
                   TileDoorGeometry.IsFlatCell(corners, cell.X, cell.Y);
        }

        /// <summary>True when any one of the cell's 4 corners equals the given terrain (as opposed to
        /// IsFullyOpenCell's stricter "all 4 corners" requirement) — used by WallAlcove's network-touch
        /// check, since a room-adjacent solid cell borders a mixed boundary cell, never a fully open
        /// one directly.</summary>
        private static bool CellHasAnyCornerOfTerrain(CornerTerrainGrid corners, (int X, int Y) cell, string terrain)
        {
            return Eq(corners.Labels[cell.X, cell.Y], terrain) ||
                   Eq(corners.Labels[cell.X + 1, cell.Y], terrain) ||
                   Eq(corners.Labels[cell.X, cell.Y + 1], terrain) ||
                   Eq(corners.Labels[cell.X + 1, cell.Y + 1], terrain);
        }

        /// <summary>True when the group's (row, col) local slot is a -1 hole (no real tile there) rather than a real member.</summary>
        private static bool IsHole(TileGroupRecord group, int row, int col)
        {
            return group.TileIds[row * group.Columns + col] < 0;
        }

        private static bool Eq(string a, string b) => string.Equals(a ?? string.Empty, b ?? string.Empty, StringComparison.OrdinalIgnoreCase);

        private static void Shuffle<T>(List<T> list, System.Random random)
        {
            for (var i = list.Count - 1; i > 0; i--)
            {
                var j = random.Next(i + 1);
                (list[i], list[j]) = (list[j], list[i]);
            }
        }
    }
}
