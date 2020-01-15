using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.Quest;

namespace SWLOR.Game.Server.Scripting.Quest.GuildTasks.EngineeringGuild
{
    public class BlasterRifleII: AbstractQuest
    {
        public BlasterRifleII()
        {
            CreateQuest(416, "Engineering Guild Task: 1x Blaster Rifle II", "eng_tsk_416")
                .IsRepeatable()
				.IsGuildTask(GuildType.EngineeringGuild, 2)


                .AddObjectiveCollectItem(1, "rifle_2", 1, true)

                .AddRewardGold(185)
                .AddRewardGuildPoints(GuildType.EngineeringGuild, 39);
        }
    }
}
