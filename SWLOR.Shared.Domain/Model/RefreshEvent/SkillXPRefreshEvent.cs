using SWLOR.Shared.Abstractions.Contracts;
using SWLOR.Shared.Domain.Enums;

namespace SWLOR.Shared.Domain.Model.RefreshEvent
{
    public class SkillXPRefreshEvent : IGuiRefreshEvent
    {
        public List<SkillType> ModifiedSkills { get; set; }

        public SkillXPRefreshEvent(List<SkillType> skillTypes)
        {
            ModifiedSkills = skillTypes;
        }
    }
}
