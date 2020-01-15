using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.Quest;

namespace SWLOR.Game.Server.Scripting.Quest.GuildTasks.HuntersGuild
{
    public class CZ220ColicoidExperiment: AbstractQuest
    {
        public CZ220ColicoidExperiment()
        {
            CreateQuest(569, "Hunter's Guild Task: 1x CZ-220 Colicoid Experiment", "hun_tsk_569")
                .IsRepeatable()
				.IsGuildTask(GuildType.HuntersGuild, 0)


                .AddObjectiveKillTarget(1, NPCGroup.CZ220ColicoidExperiment, 1)
                .AddObjectiveTalkToNPC(2)

                .AddRewardGold(53)
                .AddRewardGuildPoints(GuildType.HuntersGuild, 15);
        }
    }
}
