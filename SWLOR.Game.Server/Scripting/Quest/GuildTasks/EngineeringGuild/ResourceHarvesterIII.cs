using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.Quest;

namespace SWLOR.Game.Server.Scripting.Quest.GuildTasks.EngineeringGuild
{
    public class ResourceHarvesterIII: AbstractQuest
    {
        public ResourceHarvesterIII()
        {
            CreateQuest(487, "Engineering Guild Task: 1x Resource Harvester III", "eng_tsk_487")
                .IsRepeatable()

                .AddObjectiveCollectItem(1, "harvest_r_3", 1, true)

                .AddRewardGold(310)
                .AddRewardGuildPoints(GuildType.EngineeringGuild, 65);
        }
    }
}
