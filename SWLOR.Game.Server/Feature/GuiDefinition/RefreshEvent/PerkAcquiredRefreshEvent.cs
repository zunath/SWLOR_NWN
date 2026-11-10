using SWLOR.Game.Server.Domain.UI;
using SWLOR.Game.Server.Domain.Character;

namespace SWLOR.Game.Server.Feature.GuiDefinition.RefreshEvent
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
