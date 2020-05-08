using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.Quest;

namespace SWLOR.Game.Server.Scripts.Quest.GuildTasks.HuntersGuild
{
    public class CZ220Mynock: AbstractQuest
    {
        public CZ220Mynock()
        {
            CreateQuest(567, "Hunter's Guild Task: 10x CZ-220 Mynock", "hun_tsk_567")
                .IsRepeatable()

                .AddObjectiveKillTarget(1, NPCGroupType.CZ220_Mynocks, 10)
                .AddObjectiveTalkToNPC(2)

                .AddRewardGold(20)
                .AddRewardGuildPoints(GuildType.HuntersGuild, 7);
        }
    }
}
