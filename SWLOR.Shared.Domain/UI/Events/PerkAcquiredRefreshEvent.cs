using SWLOR.Shared.Abstractions.Contracts;
using SWLOR.Shared.Domain.Character.Enums;

namespace SWLOR.Shared.Domain.UI.Events
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
