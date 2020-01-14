using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.Quest;

namespace SWLOR.Game.Server.Scripting.Quest.GuildTasks.EngineeringGuild
{
    public class BasicMineralScanner: AbstractQuest
    {
        public BasicMineralScanner()
        {
            CreateQuest(351, "Engineering Guild Task: 1x Basic Mineral Scanner", "eng_tsk_351")
                .IsRepeatable()
				.IsGuildTask(GuildType.EngineeringGuild, 0)


                .AddObjectiveCollectItem(1, "scanner_m_b", 1, true)

                .AddRewardGold(60)
                .AddRewardGuildPoints(GuildType.EngineeringGuild, 15);
        }
    }
}
