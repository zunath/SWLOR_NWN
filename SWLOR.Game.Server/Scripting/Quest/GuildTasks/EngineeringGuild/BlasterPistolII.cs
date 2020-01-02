using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.Quest;

namespace SWLOR.Game.Server.Scripting.Quest.GuildTasks.EngineeringGuild
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
