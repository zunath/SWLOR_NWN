using SWLOR.Shared.Abstractions.Contracts;
using SWLOR.Shared.Domain.Character.Enums;

namespace SWLOR.Shared.Domain.UI.Events
{
    public class AchievementUnlockedRefreshEvent : IGuiRefreshEvent
    {
        public AchievementType Type { get; set; }
    }
}
