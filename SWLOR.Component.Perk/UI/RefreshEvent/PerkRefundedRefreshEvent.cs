using SWLOR.Component.Perk.Enums;
using SWLOR.Shared.Domain.Enums;
using SWLOR.Shared.UI.Contracts;

namespace SWLOR.Component.Perk.UI.RefreshEvent
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
