using SWLOR.Game.Server.Domain.UI;
using SWLOR.Game.Server.Domain.Character;

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
