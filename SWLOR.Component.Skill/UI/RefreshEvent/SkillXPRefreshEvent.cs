using SWLOR.Component.Skill.Enums;
using SWLOR.Shared.Domain.Enums;
using SWLOR.Shared.UI.Contracts;

namespace SWLOR.Component.Skill.UI.RefreshEvent
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
