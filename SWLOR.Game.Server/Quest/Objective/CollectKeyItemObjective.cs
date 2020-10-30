using SWLOR.Game.Server.GameObject;
using SWLOR.Game.Server.Quest.Contracts;
using SWLOR.Game.Server.Service.Legacy;

namespace SWLOR.Game.Server.Quest.Objective
{
    public class CollectKeyItemObjective: IQuestObjective
    {
        public int KeyItemID { get; }

        public CollectKeyItemObjective(int keyItemID)
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
