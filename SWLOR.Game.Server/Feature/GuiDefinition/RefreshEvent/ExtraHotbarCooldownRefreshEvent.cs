using SWLOR.Game.Server.Service.GuiService;

namespace SWLOR.Game.Server.Feature.GuiDefinition.RefreshEvent
{
    /// <summary>
    /// Raised when an ability recharge advances a frame or ends, so extra hotbar slots redraw their recharge sweep.
    /// </summary>
    public class ExtraHotbarCooldownRefreshEvent : IGuiRefreshEvent
    {
    }
}
