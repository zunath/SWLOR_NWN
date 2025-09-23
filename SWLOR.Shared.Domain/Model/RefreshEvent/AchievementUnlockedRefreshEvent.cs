using SWLOR.Shared.Abstractions.Contracts;
using SWLOR.Shared.Domain.Enums;

namespace SWLOR.Shared.Domain.Model.RefreshEvent
{
    public class AchievementUnlockedRefreshEvent : IGuiRefreshEvent
    {
        public AchievementType Type { get; set; }
    }
}
