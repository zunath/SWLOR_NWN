using System.Collections.Generic;
using SWLOR.Shared.Core.Enums;
using SWLOR.Shared.UI.Contracts;

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
