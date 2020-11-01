using SWLOR.Game.Server.Legacy.GameObject;
using SWLOR.Game.Server.Legacy.Quest.Contracts;

namespace SWLOR.Game.Server.Legacy.Quest.Objective
{
    public class TalkToNPCObjective: IQuestObjective
    {
        public void Initialize(NWPlayer player, int questID)
        {
        }

        public bool IsComplete(NWPlayer player, int questID)
        {
            return true;
        }
    }
}
