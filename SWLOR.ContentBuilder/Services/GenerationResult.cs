using SWLOR.Game.Server.Service.AreaGenerationService;
using SWLOR.Game.Server.Service.AreaGenerationService.Tileset;

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

        /// <summary>
        /// Count of DungeonDecorationPlanner.Plan's output for this generation (0 when decorations are
        /// disabled, the theme has no curated palette, or density rolled zero this seed). Decorations
        /// are placeables, not tiles, so they never render in the schematic/map preview -- this is the
        /// only place their effect is surfaced, via MainWindow's status/log line.
        /// </summary>
        public int PlannedDecorationCount { get; init; }
    }
}
