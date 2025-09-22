using SWLOR.Component.Player.Enums;
using SWLOR.Shared.Domain.Enums;
using SWLOR.Shared.UI.Contracts;

namespace SWLOR.Component.Player.UI.RefreshEvent
{
    public class AchievementUnlockedRefreshEvent : IGuiRefreshEvent
    {
        public AchievementType Type { get; set; }
    }
}
