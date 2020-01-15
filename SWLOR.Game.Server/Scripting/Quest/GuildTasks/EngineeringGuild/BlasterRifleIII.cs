using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.Quest;

namespace SWLOR.Game.Server.Scripting.Quest.GuildTasks.EngineeringGuild
{
    public class BlasterRifleIII: AbstractQuest
    {
        public BlasterRifleIII()
        {
            CreateQuest(456, "Engineering Guild Task: 1x Blaster Rifle III", "eng_tsk_456")
                .IsRepeatable()
				.IsGuildTask(GuildType.EngineeringGuild, 3)


                .AddObjectiveCollectItem(1, "rifle_3", 1, true)

                .AddRewardGold(285)
                .AddRewardGuildPoints(GuildType.EngineeringGuild, 59);
        }
    }
}
