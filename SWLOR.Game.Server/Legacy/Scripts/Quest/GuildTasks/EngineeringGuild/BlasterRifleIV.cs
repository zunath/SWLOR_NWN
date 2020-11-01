using SWLOR.Game.Server.Legacy.Enumeration;
using SWLOR.Game.Server.Legacy.Quest;

namespace SWLOR.Game.Server.Legacy.Scripts.Quest.GuildTasks.EngineeringGuild
{
    public class BlasterRifleIV: AbstractQuest
    {
        public BlasterRifleIV()
        {
            CreateQuest(508, "Engineering Guild Task: 1x Blaster Rifle IV", "eng_tsk_508")
                .IsRepeatable()

                .AddObjectiveCollectItem(1, "rifle_4", 1, true)

                .AddRewardGold(385)
                .AddRewardGuildPoints(GuildType.EngineeringGuild, 79);
        }
    }
}
