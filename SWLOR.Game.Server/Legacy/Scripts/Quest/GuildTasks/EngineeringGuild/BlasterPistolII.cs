using SWLOR.Game.Server.Legacy.Enumeration;
using SWLOR.Game.Server.Legacy.Quest;

namespace SWLOR.Game.Server.Legacy.Scripts.Quest.GuildTasks.EngineeringGuild
{
    public class BlasterPistolII: AbstractQuest
    {
        public BlasterPistolII()
        {
            CreateQuest(414, "Engineering Guild Task: 1x Blaster Pistol II", "eng_tsk_414")
                .IsRepeatable()

                .AddObjectiveCollectItem(1, "blaster_2", 1, true)

                .AddRewardGold(185)
                .AddRewardGuildPoints(GuildType.EngineeringGuild, 39);
        }
    }
}
