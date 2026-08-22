#nullable disable
using System;
using System.Collections.Generic;
using System.Linq;
using SWLOR.Toolset.Domain.AreaGeneration.Layouts;
using SWLOR.Toolset.Domain.AreaGeneration.Tileset;

namespace SWLOR.Toolset.Domain.AreaGeneration
{
    /// <summary>
    /// Produces a corner-granularity macro layout for procedural area generation. Dispatches to the
    /// style-specific generator selected by <see cref="MacroLayoutParameters.Style"/>, then runs the
    /// shared post-passes every style must honor: room role assignment, accent terrain painting, and
    /// final invariant validation (border ring solid, open corners fully connected).
    ///
    /// Connectivity is guaranteed by construction for every style; if a style genuinely cannot achieve
    /// it for a given roll, it throws <see cref="InvalidOperationException"/> so the calling facade can
    /// retry with a new seed.
    /// </summary>
    public static class MacroLayoutGenerator
    {
        /// <summary>
        /// <paramref name="tileset"/> is optional and defaults to null for back-compat: existing
        /// callers that never configure MacroLayoutParameters.SetPieces get identical behavior with
        /// zero extra work. Pass the same TilesetModel that will later resolve this layout so
        /// LayoutGroupStamper can structurally re-verify configured set-piece groups against real
        /// tile data before pinning them.
        /// </summary>
        public static MacroLayout Generate(MacroLayoutParameters parameters, System.Random random, TilesetModel tileset = null)
        {
            if (parameters == null) throw new ArgumentNullException(nameof(parameters));
            if (random == null) throw new ArgumentNullException(nameof(random));

            var wasCloned = false;

            // Terrain-label case unification. A .set file can spell the SAME terrain differently
            // across its own sections (ttz01: [GENERAL] Default=Grass but [TERRAIN0] Name=grass with
            // lowercase tile-corner labels), and profile-declared labels are hand-typed strings, so
            // two effective parameter labels can case-insensitively denote one terrain while differing
            // ordinally (ttz01/Tropical: SolidTerrain="Grass" stamped from Default by LayoutSolver,
            // OpenTerrain="grass" from the profile). That split is incoherent: the layout styles,
            // ValidateInvariants' open-connectivity check, and LayoutCornerUtils' HashSet label sets
            // all compare corner labels ORDINALLY, while classification/site-search/resolution
            // (LayoutGroupStamper.Eq, TileResolver) compare case-insensitively -- so an intended
            // Solid==Open open-field composition actually generates as a two-label cave, and
            // LayoutGroupStamper.WriteMember's Canonicalize (which checks SolidTerrain FIRST) rewrites
            // a stamped group's open-spelled corners to the solid spelling, physically converting open
            // corners to solid. Measured on ttz01/Tropical/Organic: each door-bearing WallAlcove group
            // in isolation produced 36-40% single-attempt "disconnected open space" failures (a stamp
            // adjacent to the open blob's edge pinches off a pocket); unifying the spelling measured 0
            // failures on the same seeds. Unifies each split group of labels to the tileset's own
            // declared [TERRAIN] spelling (falling back to the first spelling seen when the label is
            // not a declared terrain), on a clone so the caller's object is never mutated. Gated on an
            // actual split existing: every composition whose labels already agree (all registered
            // profiles except ttz01's grass-open pair) takes zero clones and zero behavior change.
            if (tileset != null)
            {
                var caseFixes = TerrainLabelCaseFixes(parameters, tileset);
                if (caseFixes.Count != 0)
                {
                    parameters = parameters.Clone();
                    wasCloned = true;
                    ApplyTerrainLabelCaseFixes(parameters, caseFixes);
                }
            }

            // The Alley crosser vocabulary exists only in vmr01, and even there only against Plaza (not
            // every secondary district terrain -- districts never activate under Alley mode anyway, see
            // TunnelVocabularyCheck). Composing an Alley-corridor layout (Streets) with a tileset that
            // lacks the full Alley SHAPE inventory -- not just the crosser name, but every tile shape
            // the tunnel carver can emit with it (straight/turn/junction bodies, port-adapter cells, and
            // the side-open boundary tile a port is actually carved onto) -- would fail resolution
            // outright ("No matching tile ... Right=Alley", Ruins/tdr01's exact gap: it has Alley body/
            // junction tiles but no boundary tile carrying a lone Alley edge), so downgrade to the
            // universally-verified Corridor/Doorway vocabulary instead — Streets then reads as regular
            // tunnels on tilesets without full Alley coverage. Adjusted on a clone so the caller's
            // parameters object is never mutated.
            if (tileset != null &&
                parameters.CorridorCrosserType == CorridorCrosserType.Alley &&
                !TunnelVocabularyCheck.SupportsTunnels(
                    tileset, parameters.OpenTerrain, parameters.SecondaryOpenTerrain, parameters.SolidTerrain,
                    CorridorCrosserType.Alley, extraDoorSlotCrossers: parameters.DoorSlotCrossers))
            {
                if (!wasCloned)
                {
                    parameters = parameters.Clone();
                    wasCloned = true;
                }

                parameters.CorridorCrosserType = CorridorCrosserType.Corridor;
            }

            // Tunnel mode (Corridor or Custom crosser type, post Alley-downgrade above) needs the full
            // body/port SHAPE inventory the tunnel carver can emit, not merely both crosser names present
            // in the tileset's own declared vocabulary. Some registered tilesets (e.g. Barrows/tbw01)
            // carve a real "corridor" crosser but never declare a "Doorway" crosser at all -- Tunnel
            // mode's port carving always needs one. Others (Illithid Interior/tii01) declare both names
            // but are missing a specific junction shape (a corridor bend merging directly into a room's
            // doorway port) that only shows up intermittently, not on every crosser-presence check --
            // RoomsAndCorridorsLayout's own per-edge fallback can't catch either gap because
            // LayoutTunnelCarver labels edges purely from corner geometry, so it reports success even
            // though the label names/shapes the resolver can never place a tile for. Downgrading here —
            // the same "clone on write, check before dispatch" shape as the Alley downgrade above — turns
            // the pairing into a rooms-with-open-lanes layout instead of a resolution failure (Barrows) or
            // an unreliable one (Illithid). A tileset that downgraded out of Alley above and also lacks
            // full Corridor/Doorway shape coverage (Barrows/Streets) composes both downgrades in
            // sequence: Alley -> Corridor -> OpenLane. Custom (a tileset-declared alternate body/port
            // family, see MacroLayoutParameters.TunnelBodyCrosser) is probed and downgraded the same way
            // -- DungeonComposition.BuildLayoutParameters only ever switches a composition into Custom
            // when the tileset profile actually declared a vocabulary, but the shape probe still runs
            // here rather than trusting that declaration blindly, mirroring every other tileset-declared
            // capability (AccentTerrain, ChannelTerrain, ElevationRegions, ...) in this codebase.
            if (tileset != null &&
                parameters.CorridorMode == CorridorMode.Tunnel &&
                (parameters.CorridorCrosserType == CorridorCrosserType.Corridor ||
                 parameters.CorridorCrosserType == CorridorCrosserType.Custom) &&
                !TunnelVocabularyCheck.SupportsTunnels(
                    tileset, parameters.OpenTerrain, parameters.SecondaryOpenTerrain, parameters.SolidTerrain,
                    parameters.CorridorCrosserType, parameters.TunnelBodyCrosser, parameters.TunnelPortCrosser,
                    parameters.DoorSlotCrossers))
            {
                if (!wasCloned)
                {
                    parameters = parameters.Clone();
                    wasCloned = true;
                }

                parameters.CorridorMode = CorridorMode.OpenLane;
            }

            // Set-piece-heavy room-supply scaling (see MacroLayoutParameters.SetPieceRoomSupplyScaling):
            // runs BEFORE ClampToValid so the derived counts still pass through the same normalization
            // every caller-supplied count does. Gated on the declared flag, so every composition that
            // never declares it takes zero new branches, zero clones, and zero RNG difference here
            // (RoomSupplyScalingIsolationTests pins that byte-identity across the registered tilesets).
            if (LayoutParameterConstraints.NeedsSetPieceRoomSupplyScaling(parameters))
            {
                if (!wasCloned)
                {
                    parameters = parameters.Clone();
                    wasCloned = true;
                }

                LayoutParameterConstraints.ApplySetPieceRoomSupplyScaling(parameters);
            }

            // Normalize every Advanced Settings knob (room counts/sizes, organic fill, corridor width,
            // entrance/exit counts, size floor) to a combination LayoutParameterConstraints has
            // verified is generation-safe. Content Builder's sliders can otherwise reach combinations
            // that throw outright (e.g. Min Rooms > Max Rooms) or silently degrade into a
            // near-certain failure (e.g. Min Room Size > Max Room Size, or OrganicCave's Organic Fill
            // slider floor at a small size) -- see LayoutParameterConstraints.ClampToValid for the
            // probe evidence behind each bound. NeedsClamping is a pure value check (no allocation) so
            // the common already-valid case clones nothing beyond what the Alley downgrade above may
            // already have done; a caller's own object is never mutated either way.
            if (LayoutParameterConstraints.NeedsClamping(parameters))
            {
                if (!wasCloned)
                    parameters = parameters.Clone();
                LayoutParameterConstraints.ClampToValid(parameters);
            }

            MacroLayout layout = parameters.Style switch
            {
                DungeonLayoutStyle.RoomsAndCorridors => RoomsAndCorridorsLayout.Generate(parameters, random),
                DungeonLayoutStyle.OrganicCave => OrganicCaveLayout.Generate(parameters, random),
                DungeonLayoutStyle.Warren => WarrenLayout.Generate(parameters, random),
                DungeonLayoutStyle.PackedRooms => PackedRoomsLayout.Generate(parameters, random),
                DungeonLayoutStyle.Labyrinth => LabyrinthLayout.Generate(parameters, random),
                _ => throw new ArgumentOutOfRangeException(nameof(parameters), parameters.Style, "Unknown DungeonLayoutStyle.")
            };

            if (layout.Rooms.Count < 2)
            {
                throw new InvalidOperationException(
                    $"{parameters.Style} layout produced only {layout.Rooms.Count} room(s); at least 2 are required.");
            }

            layout.DoorTransitions = parameters.DoorTransitions;
            layout.OpenTerrain = parameters.OpenTerrain;
            layout.SecondaryOpenTerrain = parameters.SecondaryOpenTerrain;
            layout.FeatureDensity = parameters.FeatureDensity;
            layout.FeatureTiles = parameters.FeatureTiles;
            layout.SetPieces = parameters.SetPieces;
            layout.ExitGroups = parameters.ExitGroups;
            layout.DoorSlotCrossers = parameters.DoorSlotCrossers;
            layout.ExcludedTiles = parameters.ExcludedTiles;

            LayoutRoleAssignment.AssignRoles(layout, parameters, random);
            LayoutAccentPainter.PaintAccents(layout, parameters, random);
            LayoutAccentChannelCarver.CarveChannels(layout, parameters, tileset, random);
            LayoutTransitionAssignment.AssignTransitions(layout, parameters, random);

            // Runs after transitions are anchored (so a fence line can avoid them) and before
            // LayoutGroupStamper, whose CorridorInsert classifier can splice a FenceDoor/BigDoorAlley
            // group gate into a straight run this pass carves when a tileset profile configures one.
            LayoutFenceCarver.CarveFences(layout, parameters, tileset, random);

            // Runs after fences (so it can see and avoid fence crossers too) and before
            // LayoutGroupStamper: the stamper's own flat-cell guards read CornerTerrainGrid.Heights
            // directly (see TileDoorGeometry.IsFlatCell), so painting final heights here first means
            // those guards correctly refuse to stamp a set piece onto a now-raised cell with zero
            // extra code on the stamper's side.
            LayoutElevationPainter.Paint(layout, parameters, tileset, random);

            // Runs immediately after LayoutElevationPainter for the same reason (final heights/terrain
            // must be settled before LayoutGroupStamper's flat-cell guards run): paints depth pools of a
            // second terrain inside room interiors, reusing LayoutElevationPainter's own verified
            // rectangle/rim machinery for the raised Floor bank around each pool.
            LayoutElevationPoolPainter.Paint(layout, parameters, tileset, random);

            // Runs after both height passes above have settled their uniform regions: per-corner
            // relief perturbation reads (and refines) the height/terrain fields they painted, and the
            // stamper below both respects the final heights via its flat-cell guards and actively
            // searches them for ReliefPiece sites (non-flat 1x1 group pieces stamped onto matching
            // painted corners -- see LayoutGroupStamper).
            LayoutReliefPainter.Paint(layout, parameters, tileset, random);

            // Reordered (was: Stamp then CarveRoads): CarveRoads now runs BEFORE Stamp, from transition
            // anchors and room centers through open space that no building has claimed yet -- matching
            // hand-built fcx01 evidence that real cities are streets-first (buildings front an already-
            // laid-out road network, not the other way around). Nothing is pinned yet at this point
            // (LayoutGroupStamper is the
            // only pass that writes PinnedTiles), so a lane threads directly between anchors instead of
            // detouring around not-yet-existing buildings -- see LayoutRoadCarver's own doc comment for
            // the full rationale and RoadCarverTests for the "never overlaps a stamped tile" invariant,
            // which this order now enforces from the STAMPING side (IsOpenSetPieceSiteValid rejects any
            // footprint cell that already carries a Road edge) rather than the carving side.
            LayoutRoadCarver.CarveRoads(layout, parameters, tileset, random);

            // Runs after transitions are anchored (so set pieces can avoid them) and after roads are
            // carved (so OpenSetPiece site search can prefer a road-adjacent site and never stamp over a
            // carved lane -- see IsOpenSetPieceSiteValid) and before invariant validation (so a bad stamp
            // still fails loudly instead of silently corrupting a layout).
            if (tileset != null)
                LayoutGroupStamper.Stamp(layout, parameters, tileset, random);

            // Runs immediately after Stamp: connects any stamped building whose site didn't land road-
            // adjacent (most do, via TryPlaceOpenSetPiece's preference -- this is the fallback for the
            // rest) to the street network with a short spur lane. See LayoutRoadCarver.CarveSpurs.
            LayoutRoadCarver.CarveSpurs(layout, parameters, tileset, random);

            // Runs LAST among the terrain passes (no RNG): needs the pinned tiles and transitions
            // settled so it never repaints a corner a stamped group or door slot owns. See
            // LayoutPlatformApronPainter -- inert unless the composition declared PlatformApron
            // (chasm-margin city tilesets with structural frontage).
            LayoutPlatformApronPainter.Paint(layout, parameters);

            ValidateInvariants(layout, parameters);

            return layout;
        }

        /// <summary>
        /// Detects case-split terrain labels among the composition's effective terrain-label
        /// parameters (see the unification block in <see cref="Generate"/> for the full rationale and
        /// the measured ttz01 evidence): groups the non-empty labels case-insensitively, and for any
        /// group carrying more than one ordinal spelling, emits (From, To) fixes mapping every
        /// non-canonical spelling to the canonical one -- the tileset's own declared [TERRAIN]
        /// spelling when the label is a declared terrain, else the group's first spelling in field
        /// order. Returns an empty list (the overwhelmingly common case: no clone, no change) when
        /// every label group already agrees on one spelling.
        /// </summary>
        private static List<(string From, string To)> TerrainLabelCaseFixes(MacroLayoutParameters parameters, TilesetModel tileset)
        {
            var labels = TerrainLabelFields(parameters);
            var fixes = new List<(string From, string To)>();

            foreach (var group in labels
                         .Where(l => !string.IsNullOrEmpty(l))
                         .GroupBy(l => l, StringComparer.OrdinalIgnoreCase))
            {
                var spellings = group.Distinct(StringComparer.Ordinal).ToList();
                if (spellings.Count < 2) continue;

                var canonical = tileset.Terrains
                                    .FirstOrDefault(t => string.Equals(t, group.Key, StringComparison.OrdinalIgnoreCase))
                                ?? spellings[0];

                foreach (var spelling in spellings)
                {
                    if (!string.Equals(spelling, canonical, StringComparison.Ordinal))
                        fixes.Add((spelling, canonical));
                }
            }

            return fixes;
        }

        /// <summary>Applies <see cref="TerrainLabelCaseFixes"/>' (From, To) respellings to every
        /// terrain-label field on <paramref name="parameters"/> (always a clone -- see the unification
        /// block in <see cref="Generate"/>). Matching is ordinal: only the exact split spelling is
        /// rewritten, never an unrelated label.</summary>
        private static void ApplyTerrainLabelCaseFixes(MacroLayoutParameters parameters, List<(string From, string To)> fixes)
        {
            string Fix(string label)
            {
                foreach (var (from, to) in fixes)
                {
                    if (string.Equals(label, from, StringComparison.Ordinal))
                        return to;
                }

                return label;
            }

            parameters.SolidTerrain = Fix(parameters.SolidTerrain);
            parameters.OpenTerrain = Fix(parameters.OpenTerrain);
            parameters.SecondaryOpenTerrain = Fix(parameters.SecondaryOpenTerrain);
            parameters.AccentTerrain = Fix(parameters.AccentTerrain);
            parameters.ChannelTerrain = Fix(parameters.ChannelTerrain);
            parameters.PoolTerrain = Fix(parameters.PoolTerrain);
            parameters.ReliefBlendTerrain = Fix(parameters.ReliefBlendTerrain);
        }

        /// <summary>Every terrain-label field a macro layout composition writes into (or compares
        /// against) the corner-label grid, in a stable field order. Crosser names are a separate
        /// vocabulary (edge grid, never corner labels) and deliberately excluded.</summary>
        private static string[] TerrainLabelFields(MacroLayoutParameters parameters)
        {
            return new[]
            {
                parameters.SolidTerrain,
                parameters.OpenTerrain,
                parameters.SecondaryOpenTerrain,
                parameters.AccentTerrain,
                parameters.ChannelTerrain,
                parameters.PoolTerrain,
                parameters.ReliefBlendTerrain
            };
        }

        private static void ValidateInvariants(MacroLayout layout, MacroLayoutParameters parameters)
        {
            var corners = layout.Corners;

            for (var x = 0; x <= corners.Width; x++)
            {
                if (corners.Labels[x, 0] != parameters.SolidTerrain || corners.Labels[x, corners.Height] != parameters.SolidTerrain)
                {
                    throw new InvalidOperationException(
                        $"{parameters.Style} layout breached the border ring at column x={x}.");
                }
            }

            for (var y = 0; y <= corners.Height; y++)
            {
                if (corners.Labels[0, y] != parameters.SolidTerrain || corners.Labels[corners.Width, y] != parameters.SolidTerrain)
                {
                    throw new InvalidOperationException(
                        $"{parameters.Style} layout breached the border ring at row y={y}.");
                }
            }

            // District-aware: when SecondaryOpenTerrain is active, a secondary room's corners are a
            // disjoint component from the primary open graph until a Tunnel-mode TunnelLink joins them
            // (see LayoutTunnelCarver), so connectivity must be checked over both labels together.
            var openLabels = LayoutCornerUtils.OpenLabelSet(parameters);
            if (!LayoutCornerUtils.IsConnectedWithLinks(corners, openLabels, layout.TunnelLinks))
            {
                throw new InvalidOperationException(
                    $"{parameters.Style} layout produced disconnected open space.");
            }
        }
    }
}
