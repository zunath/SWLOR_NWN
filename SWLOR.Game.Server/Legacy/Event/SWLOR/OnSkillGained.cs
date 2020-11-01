using SWLOR.Game.Server.Legacy.GameObject;

namespace SWLOR.Game.Server.Legacy.Event.SWLOR
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
