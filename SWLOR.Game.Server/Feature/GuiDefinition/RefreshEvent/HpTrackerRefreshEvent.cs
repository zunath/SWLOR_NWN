using SWLOR.Game.Server.Service.GuiService;

namespace SWLOR.Game.Server.Feature.GuiDefinition.RefreshEvent
{
    /// <summary>
    /// Signals the HP Tracker window to rebuild its list (HP changed, or creatures moved in/out of range).
    /// </summary>
    public class HpTrackerRefreshEvent : IGuiRefreshEvent
    {
    }
}
