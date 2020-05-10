using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.Quest;

namespace SWLOR.Game.Server.Scripts.Quest.GuildTasks.EngineeringGuild
{
    public class BlasterRifleRepairKitI: AbstractQuest
    {
        public BlasterRifleRepairKitI()
        {
            CreateQuest(378, "Engineering Guild Task: 1x Blaster Rifle Repair Kit I", "eng_tsk_378")
                .IsRepeatable()

                .AddObjectiveCollectItem(1, "br_rep_1", 1, true)

                .AddRewardGold(120)
                .AddRewardGuildPoints(GuildType.EngineeringGuild, 28);
        }
    }
}
