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
            string prefix = "QUEST_REMOVE_KEY_ITEM_";
            int keyItemID = questSource.GetLocalInt(prefix + "1");
            int count = 1;

            while (keyItemID > 0)
            {
                _keyItem.RemovePlayerKeyItem(player, keyItemID);

                count++;
                keyItemID = questSource.GetLocalInt(prefix + count);
            }

        }
    }
}
