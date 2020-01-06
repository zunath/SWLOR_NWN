using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.Quest;
using SWLOR.Game.Server.Service;

namespace SWLOR.Game.Server.Scripting.Quest
{
    public class CZ220SuppliesWeaponsmith: AbstractQuest
    {
        public CZ220SuppliesWeaponsmith()
        {
            CreateQuest(7, "CZ-220 Supplies - Weaponsmith", "cz220_weaponsmith")
                
                .AddObjectiveCollectItem(1, "club_b", 1, true)
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
