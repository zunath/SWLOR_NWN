using SWLOR.Game.Server.Domain.UI;
using SWLOR.Game.Server.Domain.Items;

namespace SWLOR.Game.Server.Feature.GuiDefinition.RefreshEvent
{
    public class KeyItemReceivedRefreshEvent: IGuiRefreshEvent
    {
        public KeyItemType Type { get; set; }

        public KeyItemReceivedRefreshEvent(KeyItemType type)
        {
            Type = type;
        }
    }
}
