using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.Quest;

namespace SWLOR.Game.Server.Scripting.Quest.GuildTasks.EngineeringGuild
{
    public class BlasterRifleRepairKitIV: AbstractQuest
    {
        public BlasterRifleRepairKitIV()
        {
            CreateQuest(509, "Engineering Guild Task: 1x Blaster Rifle Repair Kit IV", "eng_tsk_509")
                .IsRepeatable()

                .AddObjectiveCollectItem(1, "br_rep_4", 1, true)

                .AddRewardGold(420)
                .AddRewardGuildPoints(GuildType.EngineeringGuild, 88);
        }
    }
}
