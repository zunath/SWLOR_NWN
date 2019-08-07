using SWLOR.Game.Server.Quest;
using SWLOR.Game.Server.Service;

namespace SWLOR.Game.Server.Scripts.Quest
{
    public class CZ220SuppliesArmorsmith: AbstractQuest
    {
        public CZ220SuppliesArmorsmith()
        {
            CreateQuest(3, "CZ-220 Supplies - Armorsmith", "cz220_armorsmith")
                
                .AddObjectiveCollectItem(1, "padding_fiber", 1, true)
                .AddObjectiveTalkToNPC(2)

                .AddRewardGold(50)
                .AddRewardKeyItem(3)
                .AddRewardFame(2, 5)

                .OnAccepted((player, questGiver) =>
                {
                    KeyItemService.GivePlayerKeyItem(player, 4);
                })
                .OnCompleted((player, questGiver) =>
                {
                    KeyItemService.RemovePlayerKeyItem(player, 4);
                });
        }
    }
}
