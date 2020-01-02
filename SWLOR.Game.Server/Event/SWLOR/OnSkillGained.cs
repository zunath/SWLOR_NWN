using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.GameObject;

namespace SWLOR.Game.Server.Event.SWLOR
{
    public class OnSkillGained
    {
        public NWPlayer Player { get; set; }
        public SkillType SkillType { get; set; }

        public OnSkillGained(NWPlayer player, SkillType skillType)
        {
            Player = player;
            SkillType = skillType;
        }
    }
}
