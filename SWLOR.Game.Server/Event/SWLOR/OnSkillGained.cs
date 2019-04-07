using SWLOR.Game.Server.GameObject;

namespace SWLOR.Game.Server.Event.SWLOR
{
    public class OnSkillGained
    {
        public NWPlayer Player { get; set; }
        public int SkillID { get; set; }

        public OnSkillGained(NWPlayer player, int skillID)
        {
            Player = player;
            SkillID = skillID;
        }
    }
}
