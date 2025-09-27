using SWLOR.Shared.Abstractions.Contracts;
using SWLOR.Shared.Domain.Inventory.Enums;

namespace SWLOR.Shared.Domain.UI.Events
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
