using SWLOR.Game.Server.Legacy.Quest;

namespace SWLOR.Game.Server.Legacy.Scripts.Quest
{
    public class DaggersForCrystal: AbstractQuest
    {
        public DaggersForCrystal()
        {
            CreateQuest(9, "Daggers for Crystal", "daggers_crystal")
                .AddObjectiveCollectItem(1, "dagger_b", 5, false)
                .AddObjectiveTalkToNPC(2)

                .AddRewardFame(3, 15)
                .AddRewardItem("p_crystal_red_qs", 1);
        }
    }
}
