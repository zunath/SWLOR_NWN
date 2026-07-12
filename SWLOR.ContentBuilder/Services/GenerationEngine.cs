using System;
using System.Collections.Generic;
using SWLOR.Game.Server.Service.AreaGenerationService;

namespace SWLOR.ContentBuilder.Services
{
    /// <summary>Outcome of one GenerationEngine.Generate call.</summary>
    internal sealed class GenerationResult
    {
        public bool Success { get; init; }
        public MacroLayout Layout { get; init; }
        public MacroLayoutParameters Parameters { get; init; }
        public TilesetModel Tileset { get; init; }
        public ResolvedLayout Resolved { get; init; }
        public int AttemptSeed { get; init; }
        public string FailureReason { get; init; }
    }

    /// <summary>
    /// Drives the same seed-derived retry loop as the runtime facade and SWLOR.ProcgenReview:
    /// MacroLayoutGenerator.Generate can throw InvalidOperationException for an unlucky roll, and
    /// TileResolver.TryResolve can fail to cover a corner combination — either is worth a retry with
    /// the next seed before giving up.
    /// </summary>
    internal static class GenerationEngine
    {
        private const int RetryCount = 6;

        public static GenerationResult Generate(
            MacroLayoutParameters baseParameters,
            TilesetModel tileset,
            int width,
            int height,
            string accentTerrain,
            double accentDensity,
            int seed,
            string openTerrainOverride = "",
            Dictionary<string, int> featureTiles = null,
            Dictionary<string, int> setPieces = null,
            double featureDensity = 0.05)
        {
            var lastFailure = "no attempts made";

            for (var attempt = 0; attempt < RetryCount; attempt++)
            {
                var trySeed = seed + attempt;
                var random = new Random(trySeed);

                var parameters = baseParameters.Clone();
                parameters.Width = width;
                parameters.Height = height;
                parameters.SolidTerrain = tileset.DefaultTerrain;
                parameters.OpenTerrain = string.IsNullOrEmpty(openTerrainOverride)
                    ? tileset.FloorTerrain
                    : openTerrainOverride;
                parameters.AccentTerrain = accentTerrain ?? string.Empty;
                parameters.AccentDensity = string.IsNullOrEmpty(parameters.AccentTerrain) ? 0.0 : accentDensity;
                // Feature tile SET always comes from the tileset profile (no UI for that); density is
                // the only user-tunable knob (Advanced > Feature Density slider).
                parameters.FeatureTiles = featureTiles ?? new Dictionary<string, int>();
                parameters.FeatureDensity = featureDensity;
                // Set pieces likewise ride the tileset profile with no dedicated UI.
                parameters.SetPieces = setPieces ?? new Dictionary<string, int>();

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
                    return new GenerationResult
                    {
                        Success = true,
                        Layout = macro,
                        Parameters = parameters,
                        Tileset = tileset,
                        Resolved = resolved,
                        AttemptSeed = trySeed
                    };
                }

                lastFailure = failureReason;
            }

            return new GenerationResult
            {
                Success = false,
                FailureReason = lastFailure,
                AttemptSeed = seed
            };
        }
    }
}
