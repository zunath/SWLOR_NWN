using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.Quest;

namespace SWLOR.Game.Server.Scripting.Quest.GuildTasks.EngineeringGuild
{
    public class BlasterPistolRepairKitIII: AbstractQuest
    {
        public BlasterPistolRepairKitIII()
        {
            CreateQuest(455, "Engineering Guild Task: 1x Blaster Pistol Repair Kit III", "eng_tsk_455")
                .IsRepeatable()
				.IsGuildTask(GuildType.EngineeringGuild, 3)


                .AddObjectiveCollectItem(1, "bp_rep_3", 1, true)

                .AddRewardGold(320)
                .AddRewardGuildPoints(GuildType.EngineeringGuild, 68);
        }
    }
}
