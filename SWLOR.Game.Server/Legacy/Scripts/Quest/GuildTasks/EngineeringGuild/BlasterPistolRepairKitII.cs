using SWLOR.Game.Server.Legacy.Enumeration;
using SWLOR.Game.Server.Legacy.Quest;

namespace SWLOR.Game.Server.Legacy.Scripts.Quest.GuildTasks.EngineeringGuild
{
    public class BlasterPistolRepairKitII: AbstractQuest
    {
        public BlasterPistolRepairKitII()
        {
            CreateQuest(415, "Engineering Guild Task: 1x Blaster Pistol Repair Kit II", "eng_tsk_415")
                .IsRepeatable()

                .AddObjectiveCollectItem(1, "bp_rep_2", 1, true)

                .AddRewardGold(220)
                .AddRewardGuildPoints(GuildType.EngineeringGuild, 48);
        }
    }
}
