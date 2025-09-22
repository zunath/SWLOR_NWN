using SWLOR.Component.Inventory.Enums;
using SWLOR.Shared.Domain.Enums;
using SWLOR.Shared.UI.Contracts;

namespace SWLOR.Component.Inventory.UI.RefreshEvent
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
