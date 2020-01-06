using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.GameObject;
using SWLOR.Game.Server.Quest.Contracts;
using SWLOR.Game.Server.Service;

namespace SWLOR.Game.Server.Quest.Reward
{
    public class QuestKeyItemReward: IQuestReward
    {
        private readonly KeyItem _keyItemID;

        public QuestKeyItemReward(KeyItem keyItemID, bool isSelectable)
        {
            _keyItemID = keyItemID;
            IsSelectable = isSelectable;

            var keyItem = KeyItemService.GetKeyItem(_keyItemID);
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
