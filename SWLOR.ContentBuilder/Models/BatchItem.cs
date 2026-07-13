using SWLOR.Game.Server.Service.AreaGenerationService;

namespace SWLOR.ContentBuilder.Models
{
    /// <summary>
    /// One queued composition for "Build Review Module". An empty TilesetProfileKey/LayoutProfileKey
    /// means "use the theme's own default profile".
    ///
    /// ProcgenReview only accepts one square size per area, while the preview UI allows independent
    /// Width/Height sliders; Size here is stamped from Width when queuing (see MainWindow.AddToBatch),
    /// matching the review tool's square-area contract.
    ///
    /// Parameters carries the full EFFECTIVE MacroLayoutParameters used for the preview that was on
    /// screen when this item was queued -- the exact object GenerationEngine.Generate produced,
    /// cloned by AddToBatch right after it forces a fresh GeneratePreview(). This is what the batch
    /// ships to SWLOR.ProcgenReview (see MainWindow.BuildReviewModuleAsync, "--areas-file"): shipping
    /// the composed+overridden parameters verbatim, rather than re-deriving them from
    /// theme/tileset/layout/seed/size alone, is what guarantees the built review module reproduces
    /// this exact preview -- previously every Advanced-settings override (style, room counts/sizes,
    /// corridor width, loop factor, organic fill, accent, feature density) was silently dropped
    /// because only the composition keys were serialized.
    /// </summary>
    internal sealed class BatchItem
    {
        public string ThemeKey { get; init; }
        public string ThemeDisplayName { get; init; }
        public string TilesetProfileKey { get; init; }
        public string TilesetDisplayName { get; init; }
        public string LayoutProfileKey { get; init; }
        public string LayoutDisplayName { get; init; }
        public int Seed { get; init; }
        public int Size { get; init; }
        public int Entrances { get; init; } = 1;
        public int Exits { get; init; } = 1;
        public bool DoorTransitions { get; init; } = true;
        public MacroLayoutParameters Parameters { get; init; }
    }
}
