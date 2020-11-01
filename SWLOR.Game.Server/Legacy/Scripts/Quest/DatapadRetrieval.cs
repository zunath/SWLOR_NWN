using SWLOR.Game.Server.Legacy.Quest;
using SWLOR.Game.Server.Legacy.Service;

namespace SWLOR.Game.Server.Legacy.Scripts.Quest
{
    public class DatapadRetrieval: AbstractQuest
    {
        public DatapadRetrieval()
        {
            CreateQuest(13, "Datapad Retrieval", "datapad_retrieval")
                .AddObjectiveTalkToNPC(1)

                .EnableRewardSelection()
                .AddRewardFame(2, 100, true)
                .AddRewardGold(50, true)

                .OnAccepted((player, questGiver) =>
                {
                    ObjectVisibilityService.AdjustVisibility(player, questGiver, false);
                });
        }
    }
}
