using SWLOR.Game.Server.Service.GuiService;

namespace SWLOR.Game.Server.Feature.GuiDefinition.RefreshEvent
{
    public class PazaakRefreshEvent : IGuiRefreshEvent
    {
        /// <summary>
        /// True when a match just started, so the window switches to the table.
        /// </summary>
        public bool MatchStarted { get; set; }
    }
}
