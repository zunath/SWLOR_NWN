using SWLOR.Game.Server.Service.GuiService;

namespace SWLOR.Game.Server.Feature.GuiDefinition.RefreshEvent
{
    /// <summary>
    /// Sent when an event is created, changed, started or ended, when a player joins or leaves one,
    /// and once a minute so start countdowns stay current.
    /// </summary>
    public class HostedEventsChangedRefreshEvent : IGuiRefreshEvent
    {
    }
}
