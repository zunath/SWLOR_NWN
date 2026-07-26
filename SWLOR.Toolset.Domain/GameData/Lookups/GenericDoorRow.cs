namespace SWLOR.Toolset.Domain.GameData.Lookups
{
    /// <summary>One genericdoors.2da row used when a door has no tileset-specific appearance.</summary>
    public sealed record GenericDoorRow(
        int Id,
        string Label,
        string DisplayName,
        string? Model);
}
