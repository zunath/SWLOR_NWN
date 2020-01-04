using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.GameObject;

namespace SWLOR.Game.Server.Event.SWLOR
{
    public class OnSkillGained
    {
        public NWPlayer Player { get; set; }
        public Skill Skill { get; set; }

        public OnSkillGained(NWPlayer player, Skill skill)
        {
            Player = player;
            Skill = skill;
        }
    }
}
