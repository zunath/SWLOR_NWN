using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.Quest;

namespace SWLOR.Game.Server.Scripting.Quest.GuildTasks.EngineeringGuild
{
    public class BlasterPistolIV: AbstractQuest
    {
        public BlasterPistolIV()
        {
            CreateQuest(506, "Engineering Guild Task: 1x Blaster Pistol IV", "eng_tsk_506")
                .IsRepeatable()

                .AddObjectiveCollectItem(1, "blaster_4", 1, true)

                .AddRewardGold(385)
                .AddRewardGuildPoints(GuildType.EngineeringGuild, 79);
        }
    }
}
