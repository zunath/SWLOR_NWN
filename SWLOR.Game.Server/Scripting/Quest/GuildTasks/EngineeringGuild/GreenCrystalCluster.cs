using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.Quest;

namespace SWLOR.Game.Server.Scripting.Quest.GuildTasks.EngineeringGuild
{
    public class GreenCrystalCluster: AbstractQuest
    {
        public GreenCrystalCluster()
        {
            CreateQuest(356, "Engineering Guild Task: 1x Green Crystal Cluster", "eng_tsk_356")
                .IsRepeatable()
				.IsGuildTask(GuildType.EngineeringGuild, 0)


                .AddObjectiveCollectItem(1, "c_cluster_green", 1, true)

                .AddRewardGold(120)
                .AddRewardGuildPoints(GuildType.EngineeringGuild, 30);
        }
    }
}
