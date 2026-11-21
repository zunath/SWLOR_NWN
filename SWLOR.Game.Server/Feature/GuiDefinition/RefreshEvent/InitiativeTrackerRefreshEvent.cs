using SWLOR.Game.Server.Service.GuiService;

namespace SWLOR.Game.Server.Feature.GuiDefinition.RefreshEvent
{
    /// <summary>
    /// Tells open initiative tracker windows to reload. Each window reads the tracker its viewer can see.
    /// </summary>
    public class InitiativeTrackerRefreshEvent : IGuiRefreshEvent
    {
    }
}
