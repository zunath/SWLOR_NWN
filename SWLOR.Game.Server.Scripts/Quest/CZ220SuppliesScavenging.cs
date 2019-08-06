using SWLOR.Game.Server.Quest;
using SWLOR.Game.Server.Service;

namespace SWLOR.Game.Server.Scripts.Quest
{
    public class CZ220SuppliesScavenging: AbstractQuest
    {
        public CZ220SuppliesScavenging()
        {
            CreateQuest(6, "CZ-220 Supplies - Scavenging", "cz220_scavenging")
                
                .AddObjectiveCollectItem(1, "scrap_metal", 10, true)
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
