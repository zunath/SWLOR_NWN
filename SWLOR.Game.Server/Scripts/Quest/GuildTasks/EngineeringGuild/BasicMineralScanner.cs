using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.Quest;

namespace SWLOR.Game.Server.Scripts.Quest.GuildTasks.EngineeringGuild
{
    public class BasicMineralScanner: AbstractQuest
    {
        public BasicMineralScanner()
        {
            CreateQuest(351, "Engineering Guild Task: 1x Basic Mineral Scanner", "eng_tsk_351")
                .IsRepeatable()

                .AddObjectiveCollectItem(1, "scanner_m_b", 1, true)

                .AddRewardGold(60)
                .AddRewardGuildPoints(GuildType.EngineeringGuild, 15);
        }
    }
}
