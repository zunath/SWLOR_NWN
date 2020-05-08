using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.Quest;

namespace SWLOR.Game.Server.Scripts.Quest.GuildTasks.EngineeringGuild
{
    public class ResourceScannerII: AbstractQuest
    {
        public ResourceScannerII()
        {
            CreateQuest(440, "Engineering Guild Task: 1x Resource Scanner II", "eng_tsk_440")
                .IsRepeatable()

                .AddObjectiveCollectItem(1, "scanner_r_2", 1, true)

                .AddRewardGold(210)
                .AddRewardGuildPoints(GuildType.EngineeringGuild, 45);
        }
    }
}
