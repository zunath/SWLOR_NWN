using SWLOR.Game.Server.Legacy.GameObject;
using SWLOR.Game.Server.Legacy.Quest.Contracts;
using SWLOR.Game.Server.Legacy.Service;

namespace SWLOR.Game.Server.Legacy.Quest.Reward
{
    public class QuestKeyItemReward: IQuestReward
    {
        private readonly int _keyItemID;

        public QuestKeyItemReward(int keyItemID, bool isSelectable)
        {
            _keyItemID = keyItemID;
            IsSelectable = isSelectable;

            var keyItem = KeyItemService.GetKeyItemByID(_keyItemID);
            MenuName = "Key Item: " +  keyItem.Name;
        }

        public bool IsSelectable { get; }
        public string MenuName { get; }

        public void GiveReward(NWPlayer player)
        {
            KeyItemService.GivePlayerKeyItem(player, _keyItemID);
        }
    }
}
