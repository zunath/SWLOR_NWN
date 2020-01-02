
using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.GameObject;

namespace SWLOR.Game.Server.Event.SWLOR
{
    public class OnSkillDecayed
    {
        public NWPlayer Player { get; set; }
        public SkillType SkillType { get; set; }
        public int OldLevel { get; set; }
        public int NewLevel { get; set; }

        public OnSkillDecayed(NWPlayer player, SkillType skillType, int oldLevel, int newLevel)
        {
            Player = player;
            SkillType = skillType;
            OldLevel = oldLevel;
            NewLevel = newLevel;
        }
    }
}
