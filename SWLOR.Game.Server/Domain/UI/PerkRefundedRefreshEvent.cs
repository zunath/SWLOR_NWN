using SWLOR.Game.Server.Domain.Character;
using SWLOR.Game.Server.Domain.Foundation;

namespace SWLOR.Game.Server.Domain.UI
{
    public class PerkRefundedRefreshEvent: IGuiRefreshEvent
    {
        public PerkType Type { get; set; }

        public PerkRefundedRefreshEvent(PerkType type)
        {
            Type = type;
        }
    }
}
