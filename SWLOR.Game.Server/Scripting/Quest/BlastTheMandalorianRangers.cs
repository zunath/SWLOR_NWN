using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.Quest;

namespace SWLOR.Game.Server.Scripting.Quest
{
    public class BlastTheMandalorianRangers: AbstractQuest
    {
        public BlastTheMandalorianRangers()
        {
            CreateQuest(21, "Blast the Mandalorian Rangers", "blast_mand_rangers")
                .AddPrerequisiteQuest(20)

                .AddObjectiveKillTarget(1, NPCGroup.MandalorianRangers, 9)
                .AddObjectiveTalkToNPC(2)

                .AddRewardGold(200)
                .AddRewardFame(FameRegion.VelesColony, 20)
                .AddRewardItem("xp_tome_1", 1);
        }
    }
}
