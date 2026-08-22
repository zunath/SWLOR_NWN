#nullable disable
using SWLOR.Toolset.Domain.AreaGeneration;
using SWLOR.Toolset.Domain.AreaGeneration.Decoration;
using SWLOR.Toolset.Domain.AreaGeneration.Tileset;

namespace SWLOR.Toolset.Domain.AreaGeneration.Authoring
{
    /// <summary>
    /// Runs the deterministic composition and retry pipeline used by the area-generation authoring
    /// surface. Geometry and dressing are both planned here so preview and the area written to the
    /// open module always consume the same result.
    /// </summary>
    public static class GenerationEngine
    {
        public static GenerationResult Generate(
            DungeonComposition composition,
            TilesetModel tileset,
            int width,
            int height,
            int seed,
            LayoutKnobOverrides overrides)
        {
            var baseParameters = composition.BuildLayoutParameters();
            overrides?.ApplyTo(baseParameters, composition.Tileset);

            var openTerrainOverride = composition.Tileset?.PrimaryOpenTerrain ?? string.Empty;
            var solved = LayoutSolver.Solve(baseParameters, tileset, width, height, seed, openTerrainOverride);

            // Decorations are pure/engine-free (DungeonDecorationPlanner.Plan) exactly like the layout
            // solver itself, so preview and module writing compute the same plan
            // would produce for this composition+seed -- without ever touching the schematic/map
            // render, which stays tile-only (see GenerationResult.PlannedDecorationCount doc comment).
            IReadOnlyList<PlannedDecoration> plannedDecorations = Array.Empty<PlannedDecoration>();
            if (solved.Success && composition.Content != null && (overrides?.EnableDecorations ?? true))
            {
                var densityPercent = overrides?.DecorationDensityPercent ?? 100;
                var decorationProfile = overrides?.DecorationProfile ?? string.Empty;
                plannedDecorations = DungeonDecorationPlanner.Plan(
                    solved.Resolved, composition.Tileset, composition.Content, densityPercent, decorationProfile);
            }

            return new GenerationResult
            {
                Success = solved.Success,
                Layout = solved.Layout,
                Parameters = solved.Parameters,
                Tileset = tileset,
                Resolved = solved.Resolved,
                AttemptSeed = solved.AttemptSeed,
                FailureReason = solved.FailureReason,
                PlannedDecorations = plannedDecorations
            };
        }
    }
}
