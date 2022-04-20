using SWLOR.Game.Server.Service.GuiService;
using SWLOR.Game.Server.Service.SkillService;

namespace SWLOR.Game.Server.Feature.GuiDefinition.Payload
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
