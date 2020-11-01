using SWLOR.Game.Server.Legacy.Enumeration;
using SWLOR.Game.Server.Legacy.Quest;

namespace SWLOR.Game.Server.Legacy.Scripts.Quest.GuildTasks.EngineeringGuild
{
    public class BlasterPistolRepairKitIII: AbstractQuest
    {
        public BlasterPistolRepairKitIII()
        {
            CreateQuest(455, "Engineering Guild Task: 1x Blaster Pistol Repair Kit III", "eng_tsk_455")
                .IsRepeatable()

                .AddObjectiveCollectItem(1, "bp_rep_3", 1, true)

                .AddRewardGold(320)
                .AddRewardGuildPoints(GuildType.EngineeringGuild, 68);
        }
    }
}
