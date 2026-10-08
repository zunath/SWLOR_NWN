namespace SWLOR.Toolset.Domain.GameData.Lookups
{
    /// <summary>One selectable row from doortypes.2da, with host-resolved display text.</summary>
    public sealed record DoorTypeRow(
        int Id,
        string Label,
        string DisplayName,
        string? Model)
    {
        /// <summary>Whether the native engine displays the door model.</summary>
        public bool VisibleModel { get; init; } = true;
    }
}
