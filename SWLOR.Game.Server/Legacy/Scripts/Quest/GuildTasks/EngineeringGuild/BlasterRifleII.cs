using SWLOR.Game.Server.Legacy.Enumeration;
using SWLOR.Game.Server.Legacy.Quest;

namespace SWLOR.Game.Server.Legacy.Scripts.Quest.GuildTasks.EngineeringGuild
{
    public class BlasterRifleII: AbstractQuest
    {
        public BlasterRifleII()
        {
            CreateQuest(416, "Engineering Guild Task: 1x Blaster Rifle II", "eng_tsk_416")
                .IsRepeatable()

                .AddObjectiveCollectItem(1, "rifle_2", 1, true)

                .AddRewardGold(185)
                .AddRewardGuildPoints(GuildType.EngineeringGuild, 39);
        }
    }
}
