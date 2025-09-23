using SWLOR.Shared.Abstractions.Contracts;
using SWLOR.Shared.Domain.Enums;

namespace SWLOR.Component.Crafting.UI.Payload
{
    public class RecipesPayload: IGuiPayload
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
