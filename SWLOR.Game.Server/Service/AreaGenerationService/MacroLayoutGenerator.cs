using System;
using SWLOR.Game.Server.Service.AreaGenerationService.Layouts;

namespace SWLOR.Game.Server.Service.AreaGenerationService
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
            var wasCloned = false;
            if (tileset != null &&
                parameters.CorridorCrosserType == CorridorCrosserType.Alley &&
                !TunnelVocabularyCheck.SupportsTunnels(
                    tileset, parameters.OpenTerrain, parameters.SecondaryOpenTerrain, parameters.SolidTerrain,
                    CorridorCrosserType.Alley))
            {
                parameters = parameters.Clone();
                parameters.CorridorCrosserType = CorridorCrosserType.Corridor;
                wasCloned = true;
            }

            // Tunnel mode (Corridor crosser type, post Alley-downgrade above) needs the full Corridor/
            // Doorway SHAPE inventory the tunnel carver can emit, not merely both crosser names present
            // in the tileset's own declared vocabulary. Some onboarded tilesets (e.g. Barrows/tbw01)
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
            // sequence: Alley -> Corridor -> OpenLane.
            if (tileset != null &&
                parameters.CorridorMode == CorridorMode.Tunnel &&
                parameters.CorridorCrosserType == CorridorCrosserType.Corridor &&
                !TunnelVocabularyCheck.SupportsTunnels(
                    tileset, parameters.OpenTerrain, parameters.SecondaryOpenTerrain, parameters.SolidTerrain,
                    CorridorCrosserType.Corridor))
            {
                if (!wasCloned)
                {
                    parameters = parameters.Clone();
                    wasCloned = true;
                }

                parameters.CorridorMode = CorridorMode.OpenLane;
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

            LayoutRoleAssignment.AssignRoles(layout, parameters, random);
            LayoutAccentPainter.PaintAccents(layout, parameters, random);
            LayoutAccentChannelCarver.CarveChannels(layout, parameters, tileset, random);
            LayoutTransitionAssignment.AssignTransitions(layout, parameters, random);

            // Runs after transitions are anchored (so a fence line can avoid them) and before
            // LayoutGroupStamper, whose CorridorInsert classifier can splice a FenceDoor/BigDoorAlley
            // group gate into a straight run this pass carves when a tileset profile configures one.
            LayoutFenceCarver.CarveFences(layout, parameters, tileset, random);

            // Runs after transitions are anchored (so set pieces can avoid them) and before invariant
            // validation (so a bad stamp still fails loudly instead of silently corrupting a layout).
            if (tileset != null)
                LayoutGroupStamper.Stamp(layout, parameters, tileset, random);

            ValidateInvariants(layout, parameters);

            return layout;
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
