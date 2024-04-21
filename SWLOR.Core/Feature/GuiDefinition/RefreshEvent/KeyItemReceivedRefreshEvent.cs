using SWLOR.Core.Service.GuiService;
using SWLOR.Core.Service.KeyItemService;

namespace SWLOR.Core.Feature.GuiDefinition.RefreshEvent
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
