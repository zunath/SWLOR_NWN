namespace SWLOR.ContentBuilder.Models
{
    /// <summary>
    /// One queued composition for "Build Review Module". Serializes to SWLOR.ProcgenReview's
    /// "--areas" wire format: theme:tileset:layout:seed:size:entrances:exits:doors (see Program.cs
    /// there). An empty TilesetProfileKey/LayoutProfileKey means "use the theme's own default
    /// profile". doors is "door" (default) or "plac" (force every transition to Placeable style).
    ///
    /// ProcgenReview only accepts one square size per area, while the preview UI allows
    /// independent Width/Height sliders; Size here is stamped from Width when queuing (see
    /// MainWindow.AddToBatch), matching the review tool's square-area contract.
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

        public string ToSpec() => $"{ThemeKey}:{TilesetProfileKey}:{LayoutProfileKey}:{Seed}:{Size}:{Entrances}:{Exits}:{(DoorTransitions ? "door" : "plac")}";
    }
}
