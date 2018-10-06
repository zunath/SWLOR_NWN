using System;
using SWLOR.Game.Server.GameObject;
using SWLOR.Game.Server.QuestRule.Contracts;
using SWLOR.Game.Server.Service.Contracts;

namespace SWLOR.Game.Server.QuestRule
{
    public class RemoveKeyItemsRule: IQuestRule
    {
        private readonly IKeyItemService _keyItem;

        public RemoveKeyItemsRule(IKeyItemService keyItem)
        {
            _keyItem = keyItem;
        }

        public void Run(NWPlayer player, NWObject questSource, int questID, string[] args)
        {
            foreach (var keyItem in args)
            {
                int keyItemID = Convert.ToInt32(keyItem);
                _keyItem.RemovePlayerKeyItem(player, keyItemID);
            }
        }
    }
}
