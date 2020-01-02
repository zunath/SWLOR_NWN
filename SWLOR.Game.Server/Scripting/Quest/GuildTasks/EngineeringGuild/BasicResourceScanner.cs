using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.Quest;

namespace SWLOR.Game.Server.Scripting.Quest.GuildTasks.EngineeringGuild
{
    public class BasicResourceScanner: AbstractQuest
    {
        public BasicResourceScanner()
        {
            CreateQuest(353, "Engineering Guild Task: 1x Basic Resource Scanner", "eng_tsk_353")
                .IsRepeatable()

                .AddObjectiveCollectItem(1, "scanner_r_b", 1, true)

                .AddRewardGold(60)
                .AddRewardGuildPoints(GuildType.EngineeringGuild, 15);
        }
    }
}
