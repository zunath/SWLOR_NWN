using SWLOR.Core.Service.AchievementService;
using SWLOR.Core.Service.GuiService;

namespace SWLOR.Core.Feature.GuiDefinition.RefreshEvent
{
    public class AchievementUnlockedRefreshEvent : IGuiRefreshEvent
    {
        public AchievementType Type { get; set; }
    }
}
