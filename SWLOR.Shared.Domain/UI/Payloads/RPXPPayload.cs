using SWLOR.Shared.Abstractions.Contracts;
using SWLOR.Shared.Domain.Skill.Enums;

namespace SWLOR.Shared.Domain.UI.Payloads
{
    public class RPXPPayload: IGuiPayload
    {
        public string SkillName { get; set; }
        public int MaxRPXP { get; set; }
        public SkillType Skill { get; set; }
    }
}
