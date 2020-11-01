using SWLOR.Game.Server.Legacy.Enumeration;
using SWLOR.Game.Server.Legacy.Quest;

namespace SWLOR.Game.Server.Legacy.Scripts.Quest.GuildTasks.EngineeringGuild
{
    public class BlasterPistolRepairKitI: AbstractQuest
    {
        public BlasterPistolRepairKitI()
        {
            CreateQuest(376, "Engineering Guild Task: 1x Blaster Pistol Repair Kit I", "eng_tsk_376")
                .IsRepeatable()

                .AddObjectiveCollectItem(1, "bp_rep_1", 1, true)

                .AddRewardGold(120)
                .AddRewardGuildPoints(GuildType.EngineeringGuild, 28);
        }
    }
}
