using SWLOR.Game.Server.Domain.Character;
using SWLOR.Game.Server.Domain.UI;

namespace SWLOR.Game.Server.Feature.GuiDefinition.RefreshEvent
{
    public class AchievementUnlockedRefreshEvent : IGuiRefreshEvent
    {
        public AchievementType Type { get; set; }
    }
}
