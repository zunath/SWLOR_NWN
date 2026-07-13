using System;
using System.Collections.Generic;
using System.Linq;

namespace SWLOR.Game.Server.Service.AreaGenerationService.Layouts
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

        private sealed class GroupMember
        {
            public int LocalRow;
            public int LocalCol;
            public TileRecord Tile;
        }

        private enum GroupKind { WallRoom, OpenSetPiece, CorridorInsert, WallAlcove, CorridorStub }

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
        }

        internal static void Stamp(MacroLayout layout, MacroLayoutParameters parameters, TilesetModel tileset, System.Random random)
        {
            if (tileset == null || parameters.SetPieces == null || parameters.SetPieces.Count == 0)
                return;

            var nextRoomId = layout.Rooms.Count == 0 ? 0 : layout.Rooms.Max(r => r.Id) + 1;

            foreach (var groupName in parameters.SetPieces.Keys.OrderBy(k => k, StringComparer.Ordinal))
            {
                var maxCount = parameters.SetPieces[groupName];
                if (maxCount <= 0) continue;

                var group = FindGroup(tileset, groupName);
                if (group == null) continue;

                if (!TryClassify(tileset, group, parameters, out var classified))
                    continue;

                for (var i = 0; i < maxCount; i++)
                {
                    var placed = classified.Kind switch
                    {
                        GroupKind.WallRoom => TryPlaceWallRoom(layout, parameters, classified, random, ref nextRoomId),
                        GroupKind.OpenSetPiece => TryPlaceOpenSetPiece(layout, parameters, classified, random),
                        GroupKind.CorridorInsert => TryPlaceCorridorInsert(layout, parameters, classified, random),
                        GroupKind.WallAlcove => TryPlaceWallAlcove(layout, parameters, classified, random, ref nextRoomId),
                        GroupKind.CorridorStub => TryPlaceCorridorStub(layout, parameters, classified, random),
                        _ => false
                    };

                    // A failed search means the grid state can't improve for this group without
                    // human/seed changes; further attempts on the same state would only repeat it.
                    if (!placed) break;
                }
            }
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
        /// actually open). A door slot is tolerated (never spawns a door object) on a WallAlcove or
        /// OpenSetPiece candidate — matching the existing CorridorInsert precedent (BigDoor01/02,
        /// InteriorHallDoor) — but still rejected on a WallRoom candidate, since none of the verified
        /// WallRoom shapes (Cell/Room/Bedroom/2x1Room/Transiton) carry one.
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
                        if (!string.IsNullOrEmpty(edge) && !Eq(edge, DoorwayCrosser)) return false;
                    }

                    members.Add(new GroupMember { LocalRow = row, LocalCol = col, Tile = tile });
                }
            }
            if (members.Count == 0) return false; // an all-hole "group" is degenerate

            var perimeterDoorways = new List<(int, int, int)>();
            foreach (var member in members)
            {
                for (var slot = 0; slot < 4; slot++)
                {
                    if (!Eq(member.Tile.GetEdgeAt(0, slot), DoorwayCrosser)) continue;

                    var (dx, dy) = SlotOffsets[slot];
                    var neighborRow = member.LocalRow + dy;
                    var neighborCol = member.LocalCol + dx;
                    var outOfBounds = neighborRow < 0 || neighborRow >= group.Rows ||
                                       neighborCol < 0 || neighborCol >= group.Columns;
                    // A Doorway facing a hole cell (in-bounds but no real member there) is also a
                    // perimeter opening -- the hole isn't another real member that could receive/match
                    // an interior Doorway edge, so this must face outward like any true perimeter edge.
                    var isPerimeter = outOfBounds || IsHole(group, neighborRow, neighborCol);
                    if (isPerimeter)
                        perimeterDoorways.Add((member.LocalRow, member.LocalCol, slot));
                }
            }

            var hasAnyDoorway = members.Any(m => m.Tile.Edges.Any(e => Eq(e, DoorwayCrosser)));
            var allCornersSolid = members.All(m => m.Tile.Corners.All(c => Eq(c, parameters.SolidTerrain)));
            var hasAnyDoor = members.Any(m => m.Tile.Doors.Count != 0);

            if (hasAnyDoorway)
            {
                // A doorway edge implies a WallRoom; anything that isn't all-solid-cornered with at
                // least one opening facing outward is an unsupported shape for this pass. None of the
                // verified WallRoom shapes carry a door slot, so this stays strict (unlike WallAlcove/
                // OpenSetPiece below).
                if (!allCornersSolid || perimeterDoorways.Count == 0 || hasAnyDoor) return false;

                classified = new ClassifiedGroup
                {
                    Group = group,
                    Members = members,
                    Kind = GroupKind.WallRoom,
                    PerimeterDoorways = perimeterDoorways
                };
                return true;
            }

            // WallAlcove: all-solid corners, zero crosser edges anywhere, at least one door slot (e.g.
            // vmr01 "Room 1 2x2".."Room 5 2x2" — a small enclosed wall chamber with a doorframe object
            // but no Doorway crosser vocabulary of its own). Checked before OpenSetPiece so an
            // all-solid group is never misrouted there (see OpenSetPiece's own open-corner requirement
            // below, which independently guards against the same misroute).
            if (allCornersSolid && hasAnyDoor)
            {
                classified = new ClassifiedGroup
                {
                    Group = group,
                    Members = members,
                    Kind = GroupKind.WallAlcove,
                    PerimeterDoorways = new List<(int, int, int)>()
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
                for (var slot = 0; slot < 4; slot++)
                {
                    var edge = tile.Edges[slot] ?? string.Empty;
                    if (edge.Length == 0) continue;
                    if (!Eq(edge, DoorwayCrosser)) { edgesAreDoorwayOnly = false; break; }
                    hasDoorwayEdge[slot] = true;
                }

                var isVerticalDoorwayPair = hasDoorwayEdge[EdgeSlot.Top] && hasDoorwayEdge[EdgeSlot.Bottom] &&
                                             !hasDoorwayEdge[EdgeSlot.Left] && !hasDoorwayEdge[EdgeSlot.Right];
                var isHorizontalDoorwayPair = hasDoorwayEdge[EdgeSlot.Left] && hasDoorwayEdge[EdgeSlot.Right] &&
                                               !hasDoorwayEdge[EdgeSlot.Top] && !hasDoorwayEdge[EdgeSlot.Bottom];

                if (edgesAreDoorwayOnly && (isVerticalDoorwayPair || isHorizontalDoorwayPair) &&
                    HasCorridorDoorwayAdapter(tileset, parameters))
                {
                    classified = new ClassifiedGroup
                    {
                        Group = group,
                        Members = new List<GroupMember> { new GroupMember { LocalRow = 0, LocalCol = 0, Tile = tile } },
                        Kind = GroupKind.CorridorInsert,
                        PerimeterDoorways = new List<(int, int, int)>(),
                        InsertCrosser = DoorwayCrosser
                    };
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// True when the tileset carries at least one flat, all-solid-corner tile with exactly one
        /// Corridor edge and its opposite edge Doorway (the other two blank) -- the corridor-to-doorway
        /// adapter a Doorway-pair CorridorInsert's flanking cells must re-key to once their shared edge
        /// is rewritten from Corridor to Doorway (see TryPlaceCorridorInsert's Doorway branch). Verified
        /// present in tdt01 (TILE46), tds01 (TILE47), zsf01 (TILE20), and vmr01 (TILE45); checked here
        /// rather than assumed so a future tileset lacking it never enables an unplaceable splice.
        /// </summary>
        private static bool HasCorridorDoorwayAdapter(TilesetModel tileset, MacroLayoutParameters parameters)
        {
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
                    else if (Eq(edge, DoorwayCrosser)) doorwaySlot = slot;
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

            if (Eq(crosser, DoorwayCrosser))
                return TryPlaceDoorwayCorridorInsert(layout, parameters, tile, random);

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

                    layout.PinnedTiles[cell] = (tile.TileId, orientation);
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
            MacroLayout layout, MacroLayoutParameters parameters, TileRecord tile, System.Random random)
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
                    if (!IsStraightCorridorCell(crossers, cell, CorridorCrosser, out var isVertical)) continue;

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
                IsStraightCorridorCell(crossers, cell, CorridorCrosser, out var isVertical);

                for (var orientation = 0; orientation < 4; orientation++)
                {
                    var oTop = tile.GetEdgeAt(orientation, EdgeSlot.Top);
                    var oRight = tile.GetEdgeAt(orientation, EdgeSlot.Right);
                    var oBottom = tile.GetEdgeAt(orientation, EdgeSlot.Bottom);
                    var oLeft = tile.GetEdgeAt(orientation, EdgeSlot.Left);

                    var matches = isVertical
                        ? Eq(oTop, DoorwayCrosser) && Eq(oBottom, DoorwayCrosser) && (oLeft ?? "").Length == 0 && (oRight ?? "").Length == 0
                        : Eq(oLeft, DoorwayCrosser) && Eq(oRight, DoorwayCrosser) && (oTop ?? "").Length == 0 && (oBottom ?? "").Length == 0;

                    if (!matches) continue;

                    if (isVertical)
                    {
                        crossers.SetEdge(cell.X, cell.Y, EdgeSlot.Top, DoorwayCrosser);
                        crossers.SetEdge(cell.X, cell.Y, EdgeSlot.Bottom, DoorwayCrosser);
                    }
                    else
                    {
                        crossers.SetEdge(cell.X, cell.Y, EdgeSlot.Left, DoorwayCrosser);
                        crossers.SetEdge(cell.X, cell.Y, EdgeSlot.Right, DoorwayCrosser);
                    }

                    layout.PinnedTiles[cell] = (tile.TileId, orientation);
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

            return IsStraightCorridorCell(crossers, neighbor, CorridorCrosser, out var neighborIsVertical) &&
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
                    layout.PinnedTiles[cell] = (tile.TileId, orientation);
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
                if (!IsWallRoomSiteValid(layout, parameters, classified, anchor, transitionTiles))
                    continue;

                StampWallRoom(layout, parameters, classified, anchor, ref nextRoomId);
                return true;
            }

            return false;
        }

        private static bool IsWallRoomSiteValid(
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
                    if (Eq(edge, CorridorCrosser)) neighborHasCorridor = true;
                    if (Eq(edge, DoorwayCrosser)) neighborHasDoorway = true;
                }

                // Strict v1: every Doorway perimeter edge must face a plain tunnel-corridor cell.
                // Requiring it not already carry a Doorway keeps two different WallRoom instances from
                // ever claiming the same corridor adapter cell from different sides.
                if (!neighborHasCorridor || neighborHasDoorway) return false;
            }

            return true;
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
        /// crosser vocabulary of its own (see TryClassify), the site requirement is relaxed to: at
        /// least one footprint-perimeter cell already touches the reachable network — either a
        /// fully-open room cell (this layout's OpenTerrain or, when districts are active,
        /// SecondaryOpenTerrain) or an existing Corridor/Alley tunnel-chain cell. Conservative v1: the
        /// door slot's own facing is not aligned to that touch point, and no door object is ever
        /// spawned for it (matching the CorridorInsert/OpenSetPiece-with-tolerated-doors precedent) —
        /// the alcove is placed as an inert decorative wall chamber wherever the footprint legally
        /// fits and happens to border the network somewhere.
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

        private static bool IsWallAlcoveSiteValid(
            MacroLayout layout, MacroLayoutParameters parameters, ClassifiedGroup classified,
            (int X, int Y) anchor, HashSet<(int X, int Y)> transitionTiles)
        {
            var group = classified.Group;
            var corners = layout.Corners;
            var crossers = layout.Crossers;
            var width = corners.Width;
            var height = corners.Height;

            var footprint = new HashSet<(int X, int Y)>();
            for (var r = 0; r < group.Rows; r++)
            {
                for (var c = 0; c < group.Columns; c++)
                {
                    var cell = (X: anchor.X + c, Y: anchor.Y + r);
                    footprint.Add(cell);

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

            foreach (var cell in footprint)
            {
                foreach (var (dx, dy) in SlotOffsets)
                {
                    var neighbor = (X: cell.X + dx, Y: cell.Y + dy);
                    if (neighbor.X < 0 || neighbor.Y < 0 || neighbor.X >= width || neighbor.Y >= height) continue;
                    if (footprint.Contains(neighbor)) continue; // interior to this same footprint

                    // A room's own interior cells are always separated from the untouched solid mass
                    // by at least one "mixed" boundary cell (open on the room-facing side, solid on the
                    // far side, mirroring the wall-cell ring TileDoorPlanner/GroupExitPlanner walk) —
                    // no solid footprint candidate is ever directly adjacent to a FULLY open cell.
                    // Requiring only that the neighbor carries at least one corner of the open terrain
                    // correctly matches that mixed boundary ring.
                    if (CellHasAnyCornerOfTerrain(corners, neighbor, parameters.OpenTerrain)) return true;
                    if (!string.IsNullOrEmpty(parameters.SecondaryOpenTerrain) &&
                        CellHasAnyCornerOfTerrain(corners, neighbor, parameters.SecondaryOpenTerrain)) return true;

                    for (var slot = 0; slot < 4; slot++)
                    {
                        var edge = crossers.GetEdge(neighbor.X, neighbor.Y, slot);
                        if (Eq(edge, CorridorCrosser) || Eq(edge, AlleyCrosser)) return true;
                        if (parameters.CorridorCrosserType == CorridorCrosserType.Custom &&
                            !string.IsNullOrEmpty(parameters.TunnelBodyCrosser) &&
                            Eq(edge, parameters.TunnelBodyCrosser)) return true;
                    }
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

            foreach (var (room, anchor) in siteCandidates)
            {
                if (!IsOpenSetPieceSiteValid(layout, room, group, anchor, out var footprint))
                    continue;

                StampOpenSetPiece(layout, parameters, classified, room, footprint);
                return true;
            }

            return false;
        }

        private static bool IsOpenSetPieceSiteValid(
            MacroLayout layout, LayoutRoom room, TileGroupRecord group, (int X, int Y) anchor,
            out List<(int X, int Y)> footprint)
        {
            footprint = null;

            var roomTiles = new HashSet<(int X, int Y)>(room.Tiles);
            var transitionTiles = new HashSet<(int X, int Y)>(layout.Transitions.Select(t => t.Tile));

            var fp = new List<(int X, int Y)>();
            for (var r = 0; r < group.Rows; r++)
            for (var c = 0; c < group.Columns; c++)
                fp.Add((anchor.X + c, anchor.Y + r));

            // Footprint plus a 1-cell margin ring must sit entirely inside this same room's open
            // tiles, and touch neither the room's own path anchor nor any transition. This single pass
            // over the extended rectangle covers footprint and margin identically.
            for (var y = anchor.Y - 1; y <= anchor.Y + group.Rows; y++)
            {
                for (var x = anchor.X - 1; x <= anchor.X + group.Columns; x++)
                {
                    var cell = (X: x, Y: y);
                    if (!roomTiles.Contains(cell)) return false;
                    if (cell == room.CenterTile) return false;
                    if (transitionTiles.Contains(cell)) return false;
                    if (layout.PinnedTiles.ContainsKey(cell)) return false;
                }
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

            layout.PinnedTiles[cell] = (tile.TileId, 0);
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
