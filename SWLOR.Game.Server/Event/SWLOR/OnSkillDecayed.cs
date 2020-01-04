
using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.GameObject;

namespace SWLOR.Game.Server.Event.SWLOR
{
    public class OnSkillDecayed
    {
        public NWPlayer Player { get; set; }
        public Skill Skill { get; set; }
        public int OldLevel { get; set; }
        public int NewLevel { get; set; }

        public OnSkillDecayed(NWPlayer player, Skill skill, int oldLevel, int newLevel)
        {
            Player = player;
            Skill = skill;
            OldLevel = oldLevel;
            NewLevel = newLevel;
        }
    }
}
