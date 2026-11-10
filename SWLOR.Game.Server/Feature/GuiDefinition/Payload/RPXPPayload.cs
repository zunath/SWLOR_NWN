using SWLOR.Game.Server.Domain.UI;
using SWLOR.Game.Server.Domain.Character;

namespace SWLOR.Game.Server.Feature.GuiDefinition.Payload
{
    public class RPXPPayload: GuiPayloadBase
    {
        public string SkillName { get; set; }
        public int MaxRPXP { get; set; }
        public SkillType Skill { get; set; }

        /// <summary>
        /// When set, the RP XP goes to this beast instead of a skill.
        /// </summary>
        public string BeastId { get; set; }
    }
}
