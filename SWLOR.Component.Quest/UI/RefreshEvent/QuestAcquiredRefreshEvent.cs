using SWLOR.Shared.UI.Contracts;

namespace SWLOR.Component.Quest.UI.RefreshEvent
{
    public class QuestAcquiredRefreshEvent: IGuiRefreshEvent
    {
        public string QuestId { get; set; }

        public QuestAcquiredRefreshEvent(string questId)
        {
            QuestId = questId;
        }
    }
}
