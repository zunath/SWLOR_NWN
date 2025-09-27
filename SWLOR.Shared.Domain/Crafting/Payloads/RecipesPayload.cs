using SWLOR.Shared.Abstractions.Contracts;
using SWLOR.Shared.Domain.Character.Enums;
using SWLOR.Shared.Domain.Crafting.Enums;

namespace SWLOR.Shared.Domain.Crafting.Payloads
{
    public class RecipesPayload: IGuiPayload
    {
        public RecipesUIModeType Mode { get; set; }
        public SkillType Skill { get; set; }

        public RecipesPayload(RecipesUIModeType mode, SkillType skill)
        {
            Mode = mode;
            Skill = skill;
        }
    }
}
