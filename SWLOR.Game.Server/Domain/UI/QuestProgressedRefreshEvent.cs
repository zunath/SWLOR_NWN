namespace SWLOR.Game.Server.Domain.UI
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
