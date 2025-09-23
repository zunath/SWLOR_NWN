using SWLOR.Shared.Abstractions.Contracts;
using SWLOR.Shared.Domain.Enums;

namespace SWLOR.Shared.Domain.Model.RefreshEvent
{
    public class PerkAcquiredRefreshEvent: IGuiRefreshEvent
    {
        public PerkType Type { get; set; }

        public PerkAcquiredRefreshEvent(PerkType type)
        {
            Type = type;
        }
    }
}
