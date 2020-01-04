using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.Quest;
using SWLOR.Game.Server.Service;

namespace SWLOR.Game.Server.Scripting.Quest
{
    public class DatapadRetrieval: AbstractQuest
    {
        public DatapadRetrieval()
        {
            CreateQuest(13, "Datapad Retrieval", "datapad_retrieval")
                .AddObjectiveTalkToNPC(1)

                .EnableRewardSelection()
                .AddRewardFame(FameRegion.CZ220, 100, true)
                .AddRewardGold(50, true)

                .OnAccepted((player, questGiver) =>
                {
                    ObjectVisibilityService.AdjustVisibility(player, questGiver, false);
                });
        }
    }
}
