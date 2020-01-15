using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.Quest;

namespace SWLOR.Game.Server.Scripting.Quest.GuildTasks.EngineeringGuild
{
    public class ResourceHarvesterII: AbstractQuest
    {
        public ResourceHarvesterII()
        {
            CreateQuest(439, "Engineering Guild Task: 1x Resource Harvester II", "eng_tsk_439")
                .IsRepeatable()
				.IsGuildTask(GuildType.EngineeringGuild, 2)


                .AddObjectiveCollectItem(1, "harvest_r_2", 1, true)

                .AddRewardGold(210)
                .AddRewardGuildPoints(GuildType.EngineeringGuild, 45);
        }
    }
}
