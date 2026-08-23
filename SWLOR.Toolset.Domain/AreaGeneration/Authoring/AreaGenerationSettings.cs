#nullable enable

namespace SWLOR.Toolset.Domain.AreaGeneration.Authoring
{
    /// <summary>One deterministic generation request from the toolset authoring surface.</summary>
    public sealed record AreaGenerationSettings
    {
        public required string ThemeKey { get; init; }
        public string TilesetProfileKey { get; init; } = string.Empty;
        public string LayoutProfileKey { get; init; } = string.Empty;
        public int Tier { get; init; } = 1;
        public int Width { get; init; } = 16;
        public int Height { get; init; } = 16;
        public int Seed { get; init; } = 4242;
        public LayoutKnobOverrides? Overrides { get; init; }
    }
}
