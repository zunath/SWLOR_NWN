using SWLOR.Shared.Abstractions.Contracts;

namespace SWLOR.Shared.Domain.Model.RefreshEvent
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
