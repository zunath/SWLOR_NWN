using System.Collections.Generic;
using SWLOR.Game.Server.Domain.UI;
using SWLOR.Game.Server.Domain.Character;

namespace SWLOR.Game.Server.Feature.GuiDefinition.RefreshEvent
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
