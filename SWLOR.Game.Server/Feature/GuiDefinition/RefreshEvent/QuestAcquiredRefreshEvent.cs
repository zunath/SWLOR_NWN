using SWLOR.Game.Server.Service.GuiService;
using SWLOR.Shared.UI.Contracts;

namespace SWLOR.Game.Server.Feature.GuiDefinition.RefreshEvent
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
