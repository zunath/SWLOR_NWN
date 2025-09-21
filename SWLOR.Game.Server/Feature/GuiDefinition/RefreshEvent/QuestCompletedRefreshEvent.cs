using SWLOR.Shared.UI.Contracts;

namespace SWLOR.Game.Server.Feature.GuiDefinition.RefreshEvent
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
