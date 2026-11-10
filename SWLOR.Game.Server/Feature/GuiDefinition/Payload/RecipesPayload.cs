using SWLOR.Game.Server.Domain.Crafting;
using SWLOR.Game.Server.Domain.UI;
using SWLOR.Game.Server.Domain.Character;

namespace SWLOR.Game.Server.Feature.GuiDefinition.Payload
{
    public class RecipesPayload: GuiPayloadBase
    {
        public RecipesUIMode Mode { get; set; }
        public SkillType Skill { get; set; }

        public RecipesPayload(RecipesUIMode mode, SkillType skill)
        {
            Mode = mode;
            Skill = skill;
        }
    }
}
