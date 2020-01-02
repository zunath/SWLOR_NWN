using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.Quest;

namespace SWLOR.Game.Server.Scripting.Quest.GuildTasks.EngineeringGuild
{
    public class ResourceScannerIV: AbstractQuest
    {
        public ResourceScannerIV()
        {
            CreateQuest(546, "Engineering Guild Task: 1x Resource Scanner IV", "eng_tsk_546")
                .IsRepeatable()

                .AddObjectiveCollectItem(1, "scanner_r_4", 1, true)

                .AddRewardGold(410)
                .AddRewardGuildPoints(GuildType.EngineeringGuild, 85);
        }
    }
}
