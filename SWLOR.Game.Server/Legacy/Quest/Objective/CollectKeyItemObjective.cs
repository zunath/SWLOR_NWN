using SWLOR.Game.Server.Legacy.GameObject;
using SWLOR.Game.Server.Legacy.Quest.Contracts;
using SWLOR.Game.Server.Legacy.Service;

namespace SWLOR.Game.Server.Legacy.Quest.Objective
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
