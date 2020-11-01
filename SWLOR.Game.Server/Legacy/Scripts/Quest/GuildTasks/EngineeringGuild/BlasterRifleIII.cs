using SWLOR.Game.Server.Legacy.Enumeration;
using SWLOR.Game.Server.Legacy.Quest;

namespace SWLOR.Game.Server.Legacy.Scripts.Quest.GuildTasks.EngineeringGuild
{
    public class BlasterRifleIII: AbstractQuest
    {
        public BlasterRifleIII()
        {
            CreateQuest(456, "Engineering Guild Task: 1x Blaster Rifle III", "eng_tsk_456")
                .IsRepeatable()

                .AddObjectiveCollectItem(1, "rifle_3", 1, true)

                .AddRewardGold(285)
                .AddRewardGuildPoints(GuildType.EngineeringGuild, 59);
        }
    }
}
