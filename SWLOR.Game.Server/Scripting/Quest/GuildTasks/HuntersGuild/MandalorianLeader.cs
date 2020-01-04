using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.Quest;

namespace SWLOR.Game.Server.Scripting.Quest.GuildTasks.HuntersGuild
{
    public class MandalorianLeader: AbstractQuest
    {
        public MandalorianLeader()
        {
            CreateQuest(587, "Hunter's Guild Task: 1x Mandalorian Leader", "hun_tsk_587")
                .IsRepeatable()

                .AddObjectiveKillTarget(1, NPCGroup.MandalorianLeader, 1)
                .AddObjectiveTalkToNPC(2)

                .AddRewardGold(82)
                .AddRewardGuildPoints(GuildType.HuntersGuild, 24);
        }
    }
}
