using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.Quest;

namespace SWLOR.Game.Server.Scripting.Quest.GuildTasks.HuntersGuild
{
    public class ValleyCairnmog: AbstractQuest
    {
        public ValleyCairnmog()
        {
            CreateQuest(588, "Hunter's Guild Task: 10x Valley Cairnmog", "hun_tsk_588")
                .IsRepeatable()

                .AddObjectiveKillTarget(1, NPCGroup.ValleyCairnmogs, 10)
                .AddObjectiveTalkToNPC(2)

                .AddRewardGold(84)
                .AddRewardGuildPoints(GuildType.HuntersGuild, 27);
        }
    }
}
