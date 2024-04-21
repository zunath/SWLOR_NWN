using SWLOR.Core.Service.GuiService;
using SWLOR.Core.Service.SkillService;

namespace SWLOR.Core.Feature.GuiDefinition.Payload
{
    public class RecipesPayload: GuiPayloadBase
    {
        public SkillType Skill { get; set; }

        public RecipesPayload(SkillType skill)
        {
            Skill = skill;
        }
    }
}
