using SWLOR.Component.Crafting.Enums;
using SWLOR.Shared.Domain.Enums;
using SWLOR.Shared.UI.Model;

namespace SWLOR.Component.Crafting.UI.Payload
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
