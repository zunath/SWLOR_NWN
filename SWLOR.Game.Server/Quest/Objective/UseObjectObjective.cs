using SWLOR.Game.Server.GameObject;
using SWLOR.Game.Server.Quest.Contracts;

namespace SWLOR.Game.Server.Quest.Objective
{
    public class UseObjectObjective: IQuestObjective
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
