using SWLOR.Game.Server.Legacy.Enumeration;
using SWLOR.Game.Server.Legacy.Quest;

namespace SWLOR.Game.Server.Legacy.Scripts.Quest.GuildTasks.EngineeringGuild
{
    public class GreenCrystalCluster: AbstractQuest
    {
        public GreenCrystalCluster()
        {
            CreateQuest(356, "Engineering Guild Task: 1x Green Crystal Cluster", "eng_tsk_356")
                .IsRepeatable()

                .AddObjectiveCollectItem(1, "c_cluster_green", 1, true)

                .AddRewardGold(120)
                .AddRewardGuildPoints(GuildType.EngineeringGuild, 30);
        }
    }
}
