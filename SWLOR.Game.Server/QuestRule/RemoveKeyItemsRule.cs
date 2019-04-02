using System;
using SWLOR.Game.Server.GameObject;
using SWLOR.Game.Server.QuestRule.Contracts;
using SWLOR.Game.Server.Service;


namespace SWLOR.Game.Server.QuestRule
{
    public class RemoveKeyItemsRule: IQuestRule
    {
        public void Run(NWPlayer player, NWObject questSource, int questID, string[] args)
        {
            foreach (var keyItem in args)
            {
                int keyItemID = Convert.ToInt32(keyItem);
                KeyItemService.RemovePlayerKeyItem(player, keyItemID);
            }
        }
    }
}
