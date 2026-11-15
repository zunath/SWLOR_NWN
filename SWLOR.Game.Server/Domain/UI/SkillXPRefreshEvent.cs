using System.Collections.Generic;
using SWLOR.Game.Server.Domain.Character;
using SWLOR.Game.Server.Domain.Foundation;

namespace SWLOR.Game.Server.Domain.UI
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
