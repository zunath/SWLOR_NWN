using SWLOR.Game.Server.Service.ExtraHotbarService;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Entity;

/// <summary>
/// What a single extra hotbar slot runs when clicked.
/// </summary>
public sealed class ExtraHotbarSlot
{
    public ExtraHotbarSlotType Type { get; set; }
    public FeatType Feat { get; set; }

    /// <summary>
    /// Items are matched by resref so a slot keeps working after a stack runs out and is restocked.
    /// </summary>
    public string ItemResref { get; set; } = string.Empty;

    public string ItemName { get; set; } = string.Empty;

    /// <summary>
    /// The chat command text, including its leading slash and arguments.
    /// </summary>
    public string Command { get; set; } = string.Empty;

    public ActionMode ActionMode { get; set; }
}
