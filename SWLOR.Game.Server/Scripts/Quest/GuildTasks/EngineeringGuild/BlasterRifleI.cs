using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.Quest;

namespace SWLOR.Game.Server.Scripts.Quest.GuildTasks.EngineeringGuild
{
    public class BlasterRifleI: AbstractQuest
    {
        public BlasterRifleI()
        {
            CreateQuest(377, "Engineering Guild Task: 1x Blaster Rifle I", "eng_tsk_377")
                .IsRepeatable()

                .AddObjectiveCollectItem(1, "rifle_1", 1, true)

                .AddRewardGold(85)
                .AddRewardGuildPoints(GuildType.EngineeringGuild, 19);
        }
    }
}
