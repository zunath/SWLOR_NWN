namespace SWLOR.Game.Server.Entity;

/// <summary>
/// One item of an equipment slot, kept by its own ID so two identical weapons can be told apart.
/// </summary>
public sealed class ExtraHotbarEquipItem
{
    public string ItemId { get; set; } = string.Empty;

    /// <summary>
    /// Finds a matching item if the original's ID is gone.
    /// </summary>
    public string ItemResref { get; set; } = string.Empty;

    public string ItemName { get; set; } = string.Empty;
}
