using SWLOR.Game.Server.Legacy.Enumeration;
using SWLOR.Game.Server.Legacy.Quest;

namespace SWLOR.Game.Server.Legacy.Scripts.Quest.GuildTasks.EngineeringGuild
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
