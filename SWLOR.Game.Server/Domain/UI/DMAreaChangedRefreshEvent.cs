using SWLOR.Game.Server.Domain.Foundation;

namespace SWLOR.Game.Server.Domain.UI
{
    /// <summary>
    /// Raised when a DM enters an area, so windows that list the current area's objects can refresh.
    /// </summary>
    public class DMAreaChangedRefreshEvent : IGuiRefreshEvent
    {
    }
}
