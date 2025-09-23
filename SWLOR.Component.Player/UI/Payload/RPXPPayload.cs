using SWLOR.Shared.Domain.Enums;
using SWLOR.Shared.UI.Model;

namespace SWLOR.Component.Player.UI.Payload
{
    public class RPXPPayload: GuiPayloadBase
    {
        public string SkillName { get; set; }
        public int MaxRPXP { get; set; }
        public SkillType Skill { get; set; }
    }
}
