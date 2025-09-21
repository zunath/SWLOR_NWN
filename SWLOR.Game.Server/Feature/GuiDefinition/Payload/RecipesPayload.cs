using SWLOR.Game.Server.Service.CraftService;
using SWLOR.Game.Server.Service.GuiService;
using SWLOR.Shared.Core.Enums;
using SWLOR.Shared.UI.Model;

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
