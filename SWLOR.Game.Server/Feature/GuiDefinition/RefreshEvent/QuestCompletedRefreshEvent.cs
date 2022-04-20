using SWLOR.Game.Server.Service.GuiService;

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
