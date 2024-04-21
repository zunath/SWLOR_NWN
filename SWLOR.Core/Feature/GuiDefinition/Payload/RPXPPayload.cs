using SWLOR.Core.Service.GuiService;
using SWLOR.Core.Service.SkillService;

namespace SWLOR.Core.Feature.GuiDefinition.Payload
{
    public class RPXPPayload: GuiPayloadBase
    {
        public string SkillName { get; set; }
        public int MaxRPXP { get; set; }
        public SkillType Skill { get; set; }
    }
}
