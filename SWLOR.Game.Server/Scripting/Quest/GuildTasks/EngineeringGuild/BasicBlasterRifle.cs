using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.Quest;

namespace SWLOR.Game.Server.Scripting.Quest.GuildTasks.EngineeringGuild
{
    public class BasicBlasterRifle: AbstractQuest
    {
        public BasicBlasterRifle()
        {
            CreateQuest(350, "Engineering Guild Task: 1x Basic Blaster Rifle", "eng_tsk_350")
                .IsRepeatable()

                .AddObjectiveCollectItem(1, "rifle_b", 1, true)

                .AddRewardGold(35)
                .AddRewardGuildPoints(GuildType.EngineeringGuild, 9);
        }
    }
}
