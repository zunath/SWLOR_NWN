using SWLOR.Game.Server.Quest;
using SWLOR.Game.Server.Scripting.Contracts;
using static NWN._;

namespace SWLOR.Game.Server.Scripts.Quest
{
    public class OreCollection: AbstractQuest, IScript
    {
        public OreCollection()
        {
            CreateQuest(1, "Ore Collection", "ore_collection")

                .AddObjectiveCollectItem(1, "raw_veldite", 10, false)

                .AddRewardGold(50)
                .AddRewardKeyItem(1)
                .AddRewardFame(2, 5)

                .OnAccepted(player => { CreateItemOnObject("harvest_r_old", player); });
        }

        public void SubscribeEvents()
        {
        }

        public void UnsubscribeEvents()
        {
        }

        public void Main()
        {
        }
    }
}
