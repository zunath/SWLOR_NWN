using SWLOR.Game.Server.Legacy.Enumeration;
using SWLOR.Game.Server.Legacy.Quest;

namespace SWLOR.Game.Server.Legacy.Scripts.Quest.GuildTasks.HuntersGuild
{
    public class MandalorianWarrior: AbstractQuest
    {
        public MandalorianWarrior()
        {
            CreateQuest(585, "Hunter's Guild Task: 10x Mandalorian Warrior", "hun_tsk_585")
                .IsRepeatable()

                .AddObjectiveKillTarget(1, NPCGroupType.Viscara_MandalorianWarriors, 10)
                .AddObjectiveTalkToNPC(2)

                .AddRewardGold(76)
                .AddRewardGuildPoints(GuildType.HuntersGuild, 19);
        }
    }
}
