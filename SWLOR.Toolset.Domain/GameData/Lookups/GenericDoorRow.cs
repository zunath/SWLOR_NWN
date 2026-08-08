namespace SWLOR.Toolset.Domain.GameData.Lookups
{
    /// <summary>One genericdoors.2da row used when a door has no tileset-specific appearance.</summary>
    public sealed record GenericDoorRow(
        int Id,
        string Label,
        string DisplayName,
        string? Model)
    {
        /// <summary>
        /// Whether the engine renders this door model. The generic transition door is deliberately
        /// invisible in game and needs an editor-only translucent representation instead.
        /// </summary>
        public bool VisibleModel { get; init; } = true;
    }
}
