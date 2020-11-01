using SWLOR.Game.Server.Legacy.Enumeration;
using SWLOR.Game.Server.Legacy.Quest;

namespace SWLOR.Game.Server.Legacy.Scripts.Quest.GuildTasks.EngineeringGuild
{
    public class BlasterRifleRepairKitIII: AbstractQuest
    {
        public BlasterRifleRepairKitIII()
        {
            CreateQuest(457, "Engineering Guild Task: 1x Blaster Rifle Repair Kit III", "eng_tsk_457")
                .IsRepeatable()

                .AddObjectiveCollectItem(1, "br_rep_3", 1, true)

                .AddRewardGold(320)
                .AddRewardGuildPoints(GuildType.EngineeringGuild, 68);
        }
    }
}
