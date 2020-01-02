using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.Quest;

namespace SWLOR.Game.Server.Scripting.Quest.GuildTasks.EngineeringGuild
{
    public class ResourceHarvesterI: AbstractQuest
    {
        public ResourceHarvesterI()
        {
            CreateQuest(403, "Engineering Guild Task: 1x Resource Harvester I", "eng_tsk_403")
                .IsRepeatable()

                .AddObjectiveCollectItem(1, "harvest_r_1", 1, true)

                .AddRewardGold(110)
                .AddRewardGuildPoints(GuildType.EngineeringGuild, 25);
        }
    }
}
