using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.Quest;

namespace SWLOR.Game.Server.Scripts.Quest.GuildTasks.EngineeringGuild
{
    public class ResourceScannerIII: AbstractQuest
    {
        public ResourceScannerIII()
        {
            CreateQuest(488, "Engineering Guild Task: 1x Resource Scanner III", "eng_tsk_488")
                .IsRepeatable()

                .AddObjectiveCollectItem(1, "scanner_r_3", 1, true)

                .AddRewardGold(310)
                .AddRewardGuildPoints(GuildType.EngineeringGuild, 65);
        }
    }
}
