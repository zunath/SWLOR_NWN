using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.Quest;

namespace SWLOR.Game.Server.Scripting.Quest.GuildTasks.EngineeringGuild
{
    public class BlasterRifleRepairKitII: AbstractQuest
    {
        public BlasterRifleRepairKitII()
        {
            CreateQuest(417, "Engineering Guild Task: 1x Blaster Rifle Repair Kit II", "eng_tsk_417")
                .IsRepeatable()
				.IsGuildTask(GuildType.EngineeringGuild, 2)


                .AddObjectiveCollectItem(1, "br_rep_2", 1, true)

                .AddRewardGold(220)
                .AddRewardGuildPoints(GuildType.EngineeringGuild, 48);
        }
    }
}
