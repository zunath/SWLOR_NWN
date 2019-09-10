using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.Quest;

namespace SWLOR.Game.Server.Scripts.Quest.GuildTasks.EngineeringGuild
{
    public class YellowCrystalCluster: AbstractQuest
    {
        public YellowCrystalCluster()
        {
            CreateQuest(362, "Engineering Guild Task: 1x Yellow Crystal Cluster", "eng_tsk_362")
                .IsRepeatable()

                .AddObjectiveCollectItem(1, "c_cluster_yellow", 1, true)

                .AddRewardGold(120)
                .AddRewardGuildPoints(GuildType.EngineeringGuild, 30);
        }
    }
}
