using SWLOR.Game.Server.Quest;
using SWLOR.Game.Server.Service;

namespace SWLOR.Game.Server.Scripts.Quest
{
    public class DatapadRetrieval: AbstractQuest
    {
        public DatapadRetrieval()
        {
            CreateQuest(13, "Datapad Retrieval", "datapad_retrieval")
                .AddObjectiveTalkToNPC(1)

                .OnAccepted((player, questGiver) =>
                {
                    ObjectVisibilityService.AdjustVisibility(player, questGiver, false);
                });
        }
    }
}
