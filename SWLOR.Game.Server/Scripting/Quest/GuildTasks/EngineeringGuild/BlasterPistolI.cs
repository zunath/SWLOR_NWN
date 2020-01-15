using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.Quest;

namespace SWLOR.Game.Server.Scripting.Quest.GuildTasks.EngineeringGuild
{
    public class BlasterPistolI: AbstractQuest
    {
        public BlasterPistolI()
        {
            CreateQuest(375, "Engineering Guild Task: 1x Blaster Pistol I", "eng_tsk_375")
                .IsRepeatable()
				.IsGuildTask(GuildType.EngineeringGuild, 1)


                .AddObjectiveCollectItem(1, "blaster_1", 1, true)

                .AddRewardGold(85)
                .AddRewardGuildPoints(GuildType.EngineeringGuild, 19);
        }
    }
}
