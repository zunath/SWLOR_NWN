using SWLOR.Game.Server.Domain.UI;

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
