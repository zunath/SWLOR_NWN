using SWLOR.Game.Server.Legacy.Enumeration;
using SWLOR.Game.Server.Legacy.Quest;

namespace SWLOR.Game.Server.Legacy.Scripts.Quest.GuildTasks.HuntersGuild
{
    public class CZ220ColicoidExperiment: AbstractQuest
    {
        public CZ220ColicoidExperiment()
        {
            CreateQuest(569, "Hunter's Guild Task: 1x CZ-220 Colicoid Experiment", "hun_tsk_569")
                .IsRepeatable()

                .AddObjectiveKillTarget(1, NPCGroupType.CZ220_ColicoidExperiment, 1)
                .AddObjectiveTalkToNPC(2)

                .AddRewardGold(53)
                .AddRewardGuildPoints(GuildType.HuntersGuild, 15);
        }
    }
}
