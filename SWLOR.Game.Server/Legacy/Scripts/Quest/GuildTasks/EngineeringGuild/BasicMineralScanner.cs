using SWLOR.Game.Server.Legacy.Enumeration;
using SWLOR.Game.Server.Legacy.Quest;

namespace SWLOR.Game.Server.Legacy.Scripts.Quest.GuildTasks.EngineeringGuild
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
