using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.Quest;
using SWLOR.Game.Server.Service;

namespace SWLOR.Game.Server.Scripting.Quest
{
    public class SelansRequest: AbstractQuest
    {
        public SelansRequest()
        {
            CreateQuest(2, "Selan's Request", "selan_request")

                .AddObjectiveCollectKeyItem(1, KeyItem.AvixTathamWorkReceipt)
                .AddObjectiveCollectKeyItem(1, KeyItem.HalronLinthWorkReceipt)
                .AddObjectiveCollectKeyItem(1, KeyItem.CraftingTerminalDroidOperatorWorkReceipt)

                .AddRewardGold(500)
                .AddRewardFame(FameRegion.CZ220, 15)
                .AddRewardKeyItem(KeyItem.CZ220ShuttlePass)

                .OnCompleted((player, questGiver) =>
                {
                    KeyItemService.RemovePlayerKeyItem(player, KeyItem.AvixTathamWorkReceipt);
                    KeyItemService.RemovePlayerKeyItem(player, KeyItem.HalronLinthWorkReceipt);
                    KeyItemService.RemovePlayerKeyItem(player, KeyItem.CraftingTerminalDroidOperatorWorkReceipt);
                });
        }
    }
}
