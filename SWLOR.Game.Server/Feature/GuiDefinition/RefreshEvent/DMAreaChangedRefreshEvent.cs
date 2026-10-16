using SWLOR.Game.Server.Service.GuiService;

namespace SWLOR.Game.Server.Feature.GuiDefinition.RefreshEvent
{
    /// <summary>
    /// Raised when a DM enters an area, so windows that list the current area's objects can refresh.
    /// </summary>
    public class DMAreaChangedRefreshEvent : IGuiRefreshEvent
    {
    }
}
