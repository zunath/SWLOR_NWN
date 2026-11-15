using SWLOR.Game.Server.Domain.Foundation;

namespace SWLOR.Game.Server.Domain.UI
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
