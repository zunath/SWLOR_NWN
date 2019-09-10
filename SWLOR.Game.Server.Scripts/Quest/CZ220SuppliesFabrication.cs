using SWLOR.Game.Server.Quest;
using SWLOR.Game.Server.Service;

namespace SWLOR.Game.Server.Scripts.Quest
{
    public class CZ220SuppliesFabrication: AbstractQuest
    {
        public CZ220SuppliesFabrication()
        {
            CreateQuest(5, "CZ-220 Supplies - Fabrication", "cz220_fabrication")
                
                .AddObjectiveCollectItem(1, "power_core", 1, true)
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
