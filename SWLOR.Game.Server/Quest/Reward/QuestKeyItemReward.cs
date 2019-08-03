using SWLOR.Game.Server.GameObject;
using SWLOR.Game.Server.Quest.Contracts;
using SWLOR.Game.Server.Service;

namespace SWLOR.Game.Server.Quest.Reward
{
    public class QuestKeyItemReward: IQuestReward
    {
        private readonly int _keyItemID;

        public QuestKeyItemReward(int keyItemID)
        {
            _keyItemID = keyItemID;
        }

        public void GiveReward(NWPlayer player)
        {
            KeyItemService.GivePlayerKeyItem(player, _keyItemID);
        }
    }
}
