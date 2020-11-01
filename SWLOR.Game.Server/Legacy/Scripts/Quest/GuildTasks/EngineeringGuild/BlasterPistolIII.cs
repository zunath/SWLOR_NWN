using SWLOR.Game.Server.Legacy.Enumeration;
using SWLOR.Game.Server.Legacy.Quest;

namespace SWLOR.Game.Server.Legacy.Scripts.Quest.GuildTasks.EngineeringGuild
{
    public class BlasterPistolIII: AbstractQuest
    {
        public BlasterPistolIII()
        {
            CreateQuest(454, "Engineering Guild Task: 1x Blaster Pistol III", "eng_tsk_454")
                .IsRepeatable()

                .AddObjectiveCollectItem(1, "blaster_3", 1, true)

                .AddRewardGold(285)
                .AddRewardGuildPoints(GuildType.EngineeringGuild, 59);
        }
    }
}
