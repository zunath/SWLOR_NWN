using SWLOR.Game.Server.Quest;

namespace SWLOR.Game.Server.Scripts.Quest
{
    public class KathHoundPartCollection: AbstractQuest
    {
        public KathHoundPartCollection()
        {
            CreateQuest(15, "Kath Hound Part Collection", "k_hound_parts")
                .AddObjectiveCollectItem(1, "k_hound_tooth", 5, false)
                .AddObjectiveCollectItem(1, "k_hound_fur", 5, false)
                .AddObjectiveTalkToNPC(2)

                .AddRewardGold(600)
                .AddRewardFame(3, 20);
        }
    }
}
