using System;
using SWLOR.Game.Server.Service.AreaGenerationService;

namespace SWLOR.AreaBuilder
{
    /// <summary>Outcome of one GenerationEngine.Generate call.</summary>
    internal sealed class GenerationResult
    {
        public bool Success { get; init; }
        public MacroLayout Layout { get; init; }
        public MacroLayoutParameters Parameters { get; init; }
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
            int seed)
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
                parameters.OpenTerrain = tileset.FloorTerrain;
                parameters.AccentTerrain = accentTerrain ?? string.Empty;
                parameters.AccentDensity = string.IsNullOrEmpty(parameters.AccentTerrain) ? 0.0 : accentDensity;

                MacroLayout macro;
                try
                {
                    macro = MacroLayoutGenerator.Generate(parameters, random);
                    macro.Seed = trySeed;
                }
                catch (InvalidOperationException ex)
                {
                    lastFailure = ex.Message;
                    continue;
                }

                if (TileResolver.TryResolve(tileset, macro, random, out _, out var failureReason))
                {
                    return new GenerationResult
                    {
                        Success = true,
                        Layout = macro,
                        Parameters = parameters,
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
