﻿using SWLOR.Game.Server.GameObject;
using SWLOR.Game.Server.Quest.Contracts;
using SWLOR.Game.Server.Service;

namespace SWLOR.Game.Server.Quest.Reward
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
