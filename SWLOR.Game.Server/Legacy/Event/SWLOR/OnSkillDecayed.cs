
using SWLOR.Game.Server.Legacy.GameObject;

namespace SWLOR.Game.Server.Legacy.Event.SWLOR
{
    public class OnSkillDecayed
    {
        public NWPlayer Player { get; set; }
        public int SkillID { get; set; }
        public int OldLevel { get; set; }
        public int NewLevel { get; set; }

        public OnSkillDecayed(NWPlayer player, int skillID, int oldLevel, int newLevel)
        {
            Player = player;
            SkillID = skillID;
            OldLevel = oldLevel;
            NewLevel = newLevel;
        }
    }
}
