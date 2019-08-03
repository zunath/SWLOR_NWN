using SWLOR.Game.Server.Data.Entity;
using SWLOR.Game.Server.GameObject;
using SWLOR.Game.Server.Quest.Contracts;

namespace SWLOR.Game.Server.Quest.Objective
{
    public class CollectKeyItemObjective: IQuestObjective
    {
        public int KeyItemID { get; }

        public CollectKeyItemObjective(int keyItemID)
        {
            KeyItemID = keyItemID;
        }

        public void Initialize(NWPlayer player, PCQuestStatus status)
        {
        }

        public bool IsComplete(NWPlayer player)
        {
            // todo: persistence
            return false;
        }
    }
}
