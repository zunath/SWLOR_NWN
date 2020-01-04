using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.Quest;

namespace SWLOR.Game.Server.Scripting.Quest.GuildTasks.HuntersGuild
{
    public class MandalorianWarrior: AbstractQuest
    {
        public MandalorianWarrior()
        {
            CreateQuest(585, "Hunter's Guild Task: 10x Mandalorian Warrior", "hun_tsk_585")
                .IsRepeatable()

                .AddObjectiveKillTarget(1, NPCGroup.MandalorianWarriors, 10)
                .AddObjectiveTalkToNPC(2)

                .AddRewardGold(76)
                .AddRewardGuildPoints(GuildType.HuntersGuild, 19);
        }
    }
}
