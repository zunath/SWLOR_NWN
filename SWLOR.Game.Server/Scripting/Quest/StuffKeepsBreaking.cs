using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.Quest;

namespace SWLOR.Game.Server.Scripting.Quest
{
    public class StuffKeepsBreaking: AbstractQuest
    {
        public StuffKeepsBreaking()
        {
            CreateQuest(27, "Stuff Keeps Breaking!", "caxx_repair_2")
                .AddPrerequisiteQuest(26)
                
                .AddObjectiveCollectItem(1, "fv_rep_1", 1, false)
                .AddObjectiveCollectItem(1, "hv_rep_1", 1, false)
                .AddObjectiveCollectItem(1, "po_rep_1", 1, false)

                .AddRewardGold(800)
                .AddRewardFame(FameRegion.CoxxionOrganization, 25)
                .AddRewardItem("xp_tome_1", 1);
        }
    }
}
