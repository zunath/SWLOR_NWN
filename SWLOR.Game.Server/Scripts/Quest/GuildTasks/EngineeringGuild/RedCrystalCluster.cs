using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.Quest;

namespace SWLOR.Game.Server.Scripts.Quest.GuildTasks.EngineeringGuild
{
    public class RedCrystalCluster: AbstractQuest
    {
        public RedCrystalCluster()
        {
            CreateQuest(360, "Engineering Guild Task: 1x Red Crystal Cluster", "eng_tsk_360")
                .IsRepeatable()

                .AddObjectiveCollectItem(1, "c_cluster_red", 1, true)

                .AddRewardGold(120)
                .AddRewardGuildPoints(GuildType.EngineeringGuild, 30);
        }
    }
}
