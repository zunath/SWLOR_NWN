using SWLOR.Game.Server.Legacy.Enumeration;
using SWLOR.Game.Server.Legacy.Quest;

namespace SWLOR.Game.Server.Legacy.Scripts.Quest.GuildTasks.EngineeringGuild
{
    public class ResourceHarvesterII: AbstractQuest
    {
        public ResourceHarvesterII()
        {
            CreateQuest(439, "Engineering Guild Task: 1x Resource Harvester II", "eng_tsk_439")
                .IsRepeatable()

                .AddObjectiveCollectItem(1, "harvest_r_2", 1, true)

                .AddRewardGold(210)
                .AddRewardGuildPoints(GuildType.EngineeringGuild, 45);
        }
    }
}
