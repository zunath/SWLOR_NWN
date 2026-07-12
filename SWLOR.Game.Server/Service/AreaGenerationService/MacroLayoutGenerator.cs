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
            layout.FeatureDensity = parameters.FeatureDensity;
            layout.FeatureTiles = parameters.FeatureTiles;
            layout.SetPieces = parameters.SetPieces;
            layout.ExitGroups = parameters.ExitGroups;

            LayoutRoleAssignment.AssignRoles(layout, parameters, random);
            LayoutAccentPainter.PaintAccents(layout, parameters, random);
            LayoutTransitionAssignment.AssignTransitions(layout, parameters, random);

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

            if (!LayoutCornerUtils.IsConnectedWithLinks(corners, parameters.OpenTerrain, layout.TunnelLinks))
            {
                throw new InvalidOperationException(
                    $"{parameters.Style} layout produced disconnected open space.");
            }
        }
    }
}
