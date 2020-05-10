using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.Quest;

namespace SWLOR.Game.Server.Scripts.Quest.GuildTasks.HuntersGuild
{
    public class MonCalaViper: AbstractQuest
    {
        public MonCalaViper()
        {
            CreateQuest(605, "Hunter's Guild Task: 10x Mon Cala Viper", "hun_tsk_605")
                .IsRepeatable()

                .AddObjectiveKillTarget(1, NPCGroupType.MonCala_Viper, 10)
                .AddObjectiveTalkToNPC(2)

                .AddRewardGold(212)
                .AddRewardGuildPoints(GuildType.HuntersGuild, 52);
        }
    }
}
