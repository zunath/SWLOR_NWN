using SWLOR.Game.Server.Service.GuiService;
using SWLOR.Game.Server.Service.SkillService;

namespace SWLOR.Game.Server.Feature.GuiDefinition.RefreshEvent
{
    public class SkillXPRefreshEvent : IGuiRefreshEvent
    {
        public SkillType Type { get; set; }

        public SkillXPRefreshEvent(SkillType type)
        {
            Type = type;
        }
    }
}
