using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.Quest;

namespace SWLOR.Game.Server.Scripts.Quest.GuildTasks.HuntersGuild
{
    public class MonCalaAradile: AbstractQuest
    {
        public MonCalaAradile()
        {
            CreateQuest(604, "Hunter's Guild Task: 10x Mon Cala Aradile", "hun_tsk_604")
                .IsRepeatable()

                .AddObjectiveKillTarget(1, NPCGroupType.MonCala_Aradile, 10)
                .AddObjectiveTalkToNPC(2)

                .AddRewardGold(212)
                .AddRewardGuildPoints(GuildType.HuntersGuild, 52);
        }
    }
}
