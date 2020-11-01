using SWLOR.Game.Server.Legacy.Enumeration;
using SWLOR.Game.Server.Legacy.Quest;

namespace SWLOR.Game.Server.Legacy.Scripts.Quest.GuildTasks.EngineeringGuild
{
    public class PowerCrystalCluster: AbstractQuest
    {
        public PowerCrystalCluster()
        {
            CreateQuest(358, "Engineering Guild Task: 1x Power Crystal Cluster", "eng_tsk_358")
                .IsRepeatable()

                .AddObjectiveCollectItem(1, "c_cluster_power", 1, true)

                .AddRewardGold(40)
                .AddRewardGuildPoints(GuildType.EngineeringGuild, 10);
        }
    }
}
