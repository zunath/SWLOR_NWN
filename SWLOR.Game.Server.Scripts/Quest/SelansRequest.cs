using SWLOR.Game.Server.Quest;
using SWLOR.Game.Server.Scripting.Contracts;
using SWLOR.Game.Server.Service;

namespace SWLOR.Game.Server.Scripts.Quest
{
    public class SelansRequest: AbstractQuest, IScript
    {
        public SelansRequest()
        {
            CreateQuest(2, "Selan's Request", "selan_request")

                .AddObjectiveCollectKeyItem(1, 1)
                .AddObjectiveCollectKeyItem(1, 2)
                .AddObjectiveCollectKeyItem(1, 3)

                .AddRewardGold(500)
                .AddRewardFame(2, 15)

                .OnCompleted(player =>
                {
                    KeyItemService.RemovePlayerKeyItem(player, 1);
                    KeyItemService.RemovePlayerKeyItem(player, 2);
                    KeyItemService.RemovePlayerKeyItem(player, 3);
                });
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
