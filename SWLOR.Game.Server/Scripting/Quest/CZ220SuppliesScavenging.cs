using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.Quest;
using SWLOR.Game.Server.Service;

namespace SWLOR.Game.Server.Scripting.Quest
{
    public class CZ220SuppliesScavenging: AbstractQuest
    {
        public CZ220SuppliesScavenging()
        {
            CreateQuest(6, "CZ-220 Supplies - Scavenging", "cz220_scavenging")
                
                .AddObjectiveCollectItem(1, "scrap_metal", 10, false)
                .AddObjectiveTalkToNPC(2)

                .AddRewardGold(50)
                .AddRewardKeyItem(KeyItem.CraftingTerminalDroidOperatorWorkReceipt)
                .AddRewardFame(FameRegion.CZ220, 5)

                .OnAccepted((player, questGiver) =>
                {
                    KeyItemService.GivePlayerKeyItem(player, KeyItem.CraftingTerminalDroidOperatorWorkOrder);
                })
                .OnCompleted((player, questGiver) =>
                {
                    KeyItemService.RemovePlayerKeyItem(player, KeyItem.CraftingTerminalDroidOperatorWorkOrder);
                });
        }
    }
}
