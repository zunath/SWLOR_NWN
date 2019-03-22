using SWLOR.Game.Server.GameObject;

namespace SWLOR.Game.Server.Messaging.Messages
{
    public class SkillGainedMessage
    {
        public NWPlayer Player { get; set; }
        public int SkillID { get; set; }

        public SkillGainedMessage(NWPlayer player, int skillID)
        {
            Player = player;
            SkillID = skillID;
        }
    }
}
