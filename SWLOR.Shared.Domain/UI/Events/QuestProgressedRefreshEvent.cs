using SWLOR.Shared.Abstractions.Contracts;

namespace SWLOR.Shared.Domain.UI.Events
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
