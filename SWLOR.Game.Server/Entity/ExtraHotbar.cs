using System.Collections.Generic;
using SWLOR.Game.Server.Service.ExtraHotbarService;

namespace SWLOR.Game.Server.Entity;

/// <summary>
/// One of a player's extra hotbars, shown in addition to the native quickbar.
/// </summary>
public sealed class ExtraHotbar
{
    public bool IsVisible { get; set; }
    public int SlotCount { get; set; } = 12;
    public ExtraHotbarLayout Layout { get; set; }
    public List<ExtraHotbarSlot> Slots { get; set; } = new();
}
