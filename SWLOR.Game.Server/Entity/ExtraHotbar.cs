using System.Collections.Generic;

namespace SWLOR.Game.Server.Entity;

/// <summary>
/// One of a player's extra hotbars, shown in addition to the native quickbar.
/// </summary>
public sealed class ExtraHotbar
{
    public bool IsVisible { get; set; }
    public int SlotCount { get; set; } = 12;
    public bool IsVertical { get; set; }
    public List<ExtraHotbarSlot> Slots { get; set; } = new();
}
