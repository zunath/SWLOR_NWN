#nullable disable
using System;
using SWLOR.Toolset.Domain.AreaGeneration.Tileset;

namespace SWLOR.Toolset.Domain.AreaGeneration
{
    /// <summary>
    /// Offline-solves a macro layout and resolves it to concrete tiles: no NWN engine calls, no
    /// area/door/waypoint realization (see <see cref="AreaGeneration"/> for the live-server facade
    /// that adds those on top). This is the single shared implementation of the seed-derived retry
    /// loop (MacroLayoutGenerator.Generate can throw for an unlucky roll; TileResolver.TryResolve can
    /// fail to cover a corner combination -- either is worth a retry with the next seed before giving
    /// up) that both the native preview and module writer use,
    /// so the two can never independently drift out of parity with each other again.
    /// </summary>
    public static class LayoutSolver
    {
        public const int DefaultRetryCount = 6;

        /// <summary>
        /// <paramref name="baseParameters"/> should already be the full EFFECTIVE parameters for this
        /// generation (typically <see cref="DungeonComposition.BuildLayoutParameters"/> plus whatever
        /// caller-specific overrides apply) -- everything except Width/Height/SolidTerrain/OpenTerrain,
        /// which this method stamps itself every attempt from <paramref name="width"/>/
        /// <paramref name="height"/>/<paramref name="tileset"/>/<paramref name="openTerrainOverride"/>.
        /// SolidTerrain is only stamped when the base parameters carry none: a tileset profile may
        /// declare an explicit solid (the exterior inversion, stamped by BuildLayoutParameters -- see
        /// DungeonTilesetProfile.SolidTerrainOverride), which wins over the tileset's GENERAL Default.
        /// </summary>
        public static LayoutSolverResult Solve(
            MacroLayoutParameters baseParameters,
            TilesetModel tileset,
            int width,
            int height,
            int seed,
            string openTerrainOverride = "",
            int retryCount = DefaultRetryCount)
        {
            if (baseParameters == null) throw new ArgumentNullException(nameof(baseParameters));
            if (tileset == null) throw new ArgumentNullException(nameof(tileset));

            var lastFailure = "no attempts made";

            for (var attempt = 0; attempt < retryCount; attempt++)
            {
                var trySeed = seed + attempt;
                // Fully-qualified: SWLOR.Game.Server.Service.Random (an unrelated static RNG-helper
                // service) shadows System.Random by simple name inside this project.
                var random = new System.Random(trySeed);

                var parameters = baseParameters.Clone();
                parameters.Width = width;
                parameters.Height = height;
                if (string.IsNullOrEmpty(parameters.SolidTerrain))
                    parameters.SolidTerrain = tileset.DefaultTerrain;
                parameters.OpenTerrain = string.IsNullOrEmpty(openTerrainOverride)
                    ? tileset.FloorTerrain
                    : openTerrainOverride;

                MacroLayout macro;
                try
                {
                    macro = MacroLayoutGenerator.Generate(parameters, random, tileset);
                    macro.Seed = trySeed;
                }
                catch (InvalidOperationException ex)
                {
                    lastFailure = ex.Message;
                    continue;
                }

                if (TileResolver.TryResolve(tileset, macro, random, out var resolved, out var failureReason))
                {
                    return new LayoutSolverResult
                    {
                        Success = true,
                        Layout = macro,
                        Parameters = parameters,
                        Resolved = resolved,
                        AttemptSeed = trySeed
                    };
                }

                lastFailure = failureReason;
            }

            return new LayoutSolverResult
            {
                Success = false,
                FailureReason = lastFailure,
                AttemptSeed = seed
            };
        }
    }
}
