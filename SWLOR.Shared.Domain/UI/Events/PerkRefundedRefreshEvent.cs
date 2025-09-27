using SWLOR.Shared.Abstractions.Contracts;
using SWLOR.Shared.Domain.Perk.Enums;

namespace SWLOR.Shared.Domain.UI.Events
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
