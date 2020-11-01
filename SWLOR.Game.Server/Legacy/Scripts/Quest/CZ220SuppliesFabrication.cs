using SWLOR.Game.Server.Legacy.Quest;
using SWLOR.Game.Server.Legacy.Service;

namespace SWLOR.Game.Server.Legacy.Scripts.Quest
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
