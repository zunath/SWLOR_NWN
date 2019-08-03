using SWLOR.Game.Server.GameObject;
using SWLOR.Game.Server.Quest.Contracts;

namespace SWLOR.Game.Server.Quest.Objective
{
    public class CollectKeyItemObjective: IQuestObjective
    {
        private int _keyItemID;

        public CollectKeyItemObjective(int keyItemID)
        {
            _keyItemID = keyItemID;
        }

        public bool IsComplete(NWPlayer player)
        {
            // todo: persistence
            return false;
        }
    }
}
