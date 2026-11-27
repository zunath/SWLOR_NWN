using SWLOR.Game.Server.Domain.Foundation;

namespace SWLOR.Game.Server.Domain.UI
{
    /// <summary>
    /// Raised when an ability recharge advances a frame or ends, so extra hotbar slots redraw their recharge sweep.
    /// </summary>
    public class ExtraHotbarCooldownRefreshEvent : IGuiRefreshEvent
    {
    }
}
