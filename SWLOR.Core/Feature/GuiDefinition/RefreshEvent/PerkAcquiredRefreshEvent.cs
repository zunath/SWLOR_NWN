using SWLOR.Core.Service.GuiService;
using SWLOR.Core.Service.PerkService;

namespace SWLOR.Core.Feature.GuiDefinition.RefreshEvent
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
