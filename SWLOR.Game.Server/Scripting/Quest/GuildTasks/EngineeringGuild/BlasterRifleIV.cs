using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.Quest;

namespace SWLOR.Game.Server.Scripting.Quest.GuildTasks.EngineeringGuild
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
