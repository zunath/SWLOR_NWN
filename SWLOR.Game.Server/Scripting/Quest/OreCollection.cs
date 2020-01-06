using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.Quest;
using static NWN._;

namespace SWLOR.Game.Server.Scripting.Quest
{
    public class OreCollection: AbstractQuest
    {
        public OreCollection()
        {
            CreateQuest(1, "Ore Collection", "ore_collection")

                .AddObjectiveCollectItem(1, "raw_veldite", 10, false)

                .AddRewardGold(50)
                .AddRewardKeyItem(KeyItem.AvixTathamWorkReceipt)
                .AddRewardFame(FameRegion.CZ220, 5)

                .OnAccepted((player, questGiver) => { CreateItemOnObject("harvest_r_old", player); });
        }
    }
}
