using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.Quest;

namespace SWLOR.Game.Server.Scripting.Quest.GuildTasks.HuntersGuild
{
    public class MandalorianPolearmParts: AbstractQuest
    {
        public MandalorianPolearmParts()
        {
            CreateQuest(597, "Hunter's Guild Task: 6x Mandalorian Polearm Parts", "hun_tsk_597")
                .IsRepeatable()
				.IsGuildTask(GuildType.HuntersGuild, 1)


                .AddObjectiveCollectItem(1, "m_polearm_parts", 6, false)

                .AddRewardGold(83)
                .AddRewardGuildPoints(GuildType.HuntersGuild, 25);
        }
    }
}
