using SWLOR.Game.Server.Legacy.Quest;
using SWLOR.Game.Server.Legacy.Service;

namespace SWLOR.Game.Server.Legacy.Scripts.Quest
{
    public class SelansRequest: AbstractQuest
    {
        public SelansRequest()
        {
            CreateQuest(2, "Selan's Request", "selan_request")

                .AddObjectiveCollectKeyItem(1, 1)
                .AddObjectiveCollectKeyItem(1, 2)
                .AddObjectiveCollectKeyItem(1, 3)

                .AddRewardGold(500)
                .AddRewardFame(2, 15)
                .AddRewardKeyItem(5)

                .OnCompleted((player, questGiver) =>
                {
                    KeyItemService.RemovePlayerKeyItem(player, 1);
                    KeyItemService.RemovePlayerKeyItem(player, 2);
                    KeyItemService.RemovePlayerKeyItem(player, 3);
                });
        }
    }
}
