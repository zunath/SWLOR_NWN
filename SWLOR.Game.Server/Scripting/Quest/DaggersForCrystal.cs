using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.Quest;

namespace SWLOR.Game.Server.Scripting.Quest
{
    public class DaggersForCrystal: AbstractQuest
    {
        public DaggersForCrystal()
        {
            CreateQuest(9, "Daggers for Crystal", "daggers_crystal")
                .AddObjectiveCollectItem(1, "dagger_b", 5, false)
                .AddObjectiveTalkToNPC(2)

                .AddRewardFame(FameRegion.VelesColony, 15)
                .AddRewardItem("p_crystal_red_qs", 1);
        }
    }
}
