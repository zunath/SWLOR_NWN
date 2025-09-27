using SWLOR.Shared.Abstractions.Contracts;
using SWLOR.Shared.Domain.Skill.Enums;

namespace SWLOR.Shared.Domain.UI.Events
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
