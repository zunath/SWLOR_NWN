using SWLOR.Shared.UI.Contracts;

namespace SWLOR.Game.Server.Feature.GuiDefinition.RefreshEvent
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
