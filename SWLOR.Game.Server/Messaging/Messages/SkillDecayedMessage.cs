
using SWLOR.Game.Server.GameObject;

namespace SWLOR.Game.Server.Messaging.Messages
{
    public class SkillDecayedMessage
    {
        public NWPlayer Player { get; set; }
        public int SkillID { get; set; }
        public int OldLevel { get; set; }
        public int NewLevel { get; set; }

        public SkillDecayedMessage(NWPlayer player, int skillID, int oldLevel, int newLevel)
        {
            Player = player;
            SkillID = skillID;
            OldLevel = oldLevel;
            NewLevel = newLevel;
        }
    }
}
