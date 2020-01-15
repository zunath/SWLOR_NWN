using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.Quest;

namespace SWLOR.Game.Server.Scripting.Quest.GuildTasks.EngineeringGuild
{
    public class BlasterPistolRepairKitIV: AbstractQuest
    {
        public BlasterPistolRepairKitIV()
        {
            CreateQuest(507, "Engineering Guild Task: 1x Blaster Pistol Repair Kit IV", "eng_tsk_507")
                .IsRepeatable()
				.IsGuildTask(GuildType.EngineeringGuild, 4)


                .AddObjectiveCollectItem(1, "bp_rep_4", 1, true)

                .AddRewardGold(420)
                .AddRewardGuildPoints(GuildType.EngineeringGuild, 88);
        }
    }
}
