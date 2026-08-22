#nullable enable
using SWLOR.Toolset.Domain.AreaGeneration.Tileset;

namespace SWLOR.Toolset.Domain.AreaGeneration.Authoring
{
    /// <summary>
    /// A solved authoring result plus the exact definitions used to produce it. Keeping these
    /// together lets preview and module writing share one immutable draft.
    /// </summary>
    public sealed record AreaGenerationDraft(
        AreaGenerationSettings Settings,
        DungeonComposition Composition,
        TilesetModel Tileset,
        GenerationResult Result);
}
