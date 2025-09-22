using SWLOR.Shared.UI.Contracts;

namespace SWLOR.Component.Quest.UI.RefreshEvent
{
    public class QuestProgressedRefreshEvent: IGuiRefreshEvent
    {
        public string QuestId { get; set; }

        public QuestProgressedRefreshEvent(string questId)
        {
            QuestId = questId;
        }
    }
}
