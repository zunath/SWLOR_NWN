using SWLOR.Game.Server.Data.Entity;
using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.GameObject;
using SWLOR.Game.Server.Quest.Contracts;
using SWLOR.Game.Server.Service;

namespace SWLOR.Game.Server.Quest.Objective
{
    public class CollectKeyItemObjective: IQuestObjective
    {
        public KeyItem KeyItemID { get; }

        public CollectKeyItemObjective(KeyItem keyItemID)
        {
            KeyItemID = keyItemID;
        }

        public void Initialize(NWPlayer player, int questID)
        {

        }

        public bool IsComplete(NWPlayer player, int questID)
        {
            return KeyItemService.PlayerHasKeyItem(player, KeyItemID);
        }
    }
}
