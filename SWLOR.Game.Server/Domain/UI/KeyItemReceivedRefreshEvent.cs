using SWLOR.Game.Server.Domain.Items;
using SWLOR.Game.Server.Domain.Foundation;

namespace SWLOR.Game.Server.Domain.UI
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
