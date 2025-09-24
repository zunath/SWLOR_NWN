using SWLOR.Shared.Abstractions.Contracts;

namespace SWLOR.Shared.Domain.UI.Events
{
    public class QuestCompletedRefreshEvent: IGuiRefreshEvent
    {
        public string QuestId { get; set; }

        public QuestCompletedRefreshEvent(string questId)
        {
            QuestId = questId;
        }
    }
}
