using SWLOR.Game.Server.Service.AreaGenerationService;
using SWLOR.Game.Server.Service.AreaGenerationService.Decoration;
using SWLOR.Game.Server.Service.AreaGenerationService.Tileset;

namespace SWLOR.ContentBuilder.Services
{
    /// <summary>
    /// Drives Content Builder's preview generation through the exact same composition + retry
    /// pipeline SWLOR.ProcgenReview and the runtime facade use (DungeonComposition.
    /// BuildLayoutParameters, then LayoutSolver.Solve's shared seed-derived retry loop). Previously
    /// this cloned the raw layout-profile Template directly, bypassing BuildLayoutParameters'
    /// SecondaryOpenTerrain/CorridorWidth-floor/ChannelTerrain/accent-terrain stamping entirely --
    /// that gap is exactly what caused Preview and the built review module to diverge.
    /// </summary>
    internal static class GenerationEngine
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
            // solver itself, so the preview can compute the SAME plan the runtime facade/ProcgenReview
            // would produce for this composition+seed -- without ever touching the schematic/map
            // render, which stays tile-only (see GenerationResult.PlannedDecorationCount doc comment).
            var plannedDecorationCount = 0;
            if (solved.Success && composition.Content != null && (overrides?.EnableDecorations ?? true))
            {
                var densityPercent = overrides?.DecorationDensityPercent ?? 100;
                var decorationProfile = overrides?.DecorationProfile ?? string.Empty;
                plannedDecorationCount = DungeonDecorationPlanner.Plan(
                    solved.Resolved, composition.Tileset, composition.Content, densityPercent, decorationProfile).Count;
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
                PlannedDecorationCount = plannedDecorationCount
            };
        }
    }
}
