using SWLOR.Shared.Abstractions.Contracts;
using SWLOR.Shared.Domain.Enums;

namespace SWLOR.Shared.Domain.Model.RefreshEvent
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
