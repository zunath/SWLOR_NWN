using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.Quest;

namespace SWLOR.Game.Server.Scripting.Quest.GuildTasks.EngineeringGuild
{
    public class BlueCrystalCluster: AbstractQuest
    {
        public BlueCrystalCluster()
        {
            CreateQuest(354, "Engineering Guild Task: 1x Blue Crystal Cluster", "eng_tsk_354")
                .IsRepeatable()
				.IsGuildTask(GuildType.EngineeringGuild, 0)


                .AddObjectiveCollectItem(1, "c_cluster_blue", 1, true)

                .AddRewardGold(120)
                .AddRewardGuildPoints(GuildType.EngineeringGuild, 30);
        }
    }
}
