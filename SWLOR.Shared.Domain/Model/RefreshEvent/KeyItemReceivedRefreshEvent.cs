using SWLOR.Shared.Abstractions.Contracts;
using SWLOR.Shared.Domain.Enums;

namespace SWLOR.Shared.Domain.Model.RefreshEvent
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
