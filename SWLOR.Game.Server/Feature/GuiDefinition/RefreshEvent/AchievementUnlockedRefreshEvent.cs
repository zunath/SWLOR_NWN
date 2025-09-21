using SWLOR.Shared.Core.Enums;
using SWLOR.Shared.UI.Contracts;

namespace SWLOR.Game.Server.Feature.GuiDefinition.RefreshEvent
{
    public class AchievementUnlockedRefreshEvent : IGuiRefreshEvent
    {
        public AchievementType Type { get; set; }
    }
}
