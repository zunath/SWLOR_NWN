using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.Quest;

namespace SWLOR.Game.Server.Scripts.Quest.GuildTasks.HuntersGuild
{
    public class ViscaraWarocas: AbstractQuest
    {
        public ViscaraWarocas()
        {
            CreateQuest(571, "Hunter's Guild Task: 10x Viscara Warocas", "hun_tsk_571")
                .IsRepeatable()

                .AddObjectiveKillTarget(1, NPCGroupType.Viscara_WildlandsWarocas, 10)
                .AddObjectiveTalkToNPC(2)

                .AddRewardGold(65)
                .AddRewardGuildPoints(GuildType.HuntersGuild, 12);
        }
    }
}
