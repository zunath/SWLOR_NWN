using SWLOR.Shared.Abstractions.Contracts;
using SWLOR.Shared.Domain.Enums;

namespace SWLOR.Shared.Domain.Model.Payload
{
    public class RPXPPayload: IGuiPayload
    {
        public string SkillName { get; set; }
        public int MaxRPXP { get; set; }
        public SkillType Skill { get; set; }
    }
}
