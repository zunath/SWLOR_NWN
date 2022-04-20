using SWLOR.Game.Server.Service.GuiService;
using SWLOR.Game.Server.Service.PerkService;

namespace SWLOR.Game.Server.Feature.GuiDefinition.RefreshEvent
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
