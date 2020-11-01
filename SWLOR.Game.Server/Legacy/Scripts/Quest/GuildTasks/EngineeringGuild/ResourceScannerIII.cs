using SWLOR.Game.Server.Legacy.Enumeration;
using SWLOR.Game.Server.Legacy.Quest;

namespace SWLOR.Game.Server.Legacy.Scripts.Quest.GuildTasks.EngineeringGuild
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
