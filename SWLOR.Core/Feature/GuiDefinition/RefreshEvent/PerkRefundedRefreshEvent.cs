using SWLOR.Core.Service.GuiService;
using SWLOR.Core.Service.PerkService;

namespace SWLOR.Core.Feature.GuiDefinition.RefreshEvent
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
