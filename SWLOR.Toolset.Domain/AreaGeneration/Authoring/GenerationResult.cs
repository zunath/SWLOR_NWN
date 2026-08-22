#nullable disable
using SWLOR.Toolset.Domain.AreaGeneration;
using SWLOR.Toolset.Domain.AreaGeneration.Decoration;
using SWLOR.Toolset.Domain.AreaGeneration.Tileset;

namespace SWLOR.Toolset.Domain.AreaGeneration.Authoring
{
    /// <summary>Outcome of one GenerationEngine.Generate call.</summary>
    public sealed class GenerationResult
    {
        public bool Success { get; init; }
        public MacroLayout Layout { get; init; }
        public MacroLayoutParameters Parameters { get; init; }
        public TilesetModel Tileset { get; init; }
        public ResolvedLayout Resolved { get; init; }
        public int AttemptSeed { get; init; }
        public string FailureReason { get; init; }

        /// <summary>
        /// Dressing planned for this exact solved layout. The writer consumes this list directly,
        /// avoiding a second plan pass that could drift from the preview.
        /// </summary>
        public IReadOnlyList<PlannedDecoration> PlannedDecorations { get; init; } = Array.Empty<PlannedDecoration>();

        public int PlannedDecorationCount => PlannedDecorations.Count;
    }
}
