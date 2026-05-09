using SWLOR.Game.Server.Service.GuiService;

namespace SWLOR.Game.Server.Feature.GuiDefinition.RefreshEvent
{
    public class QuestAbandonedRefreshEvent: IGuiRefreshEvent
    {
        public string QuestId { get; set; }

        public QuestAbandonedRefreshEvent(string questId)
        {
            QuestId = questId;
        }
    }
}
