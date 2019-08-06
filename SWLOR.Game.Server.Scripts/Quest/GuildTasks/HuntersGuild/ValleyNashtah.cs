using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.Quest;

namespace SWLOR.Game.Server.Scripts.Quest.GuildTasks.HuntersGuild
{
    public class ValleyNashtah: AbstractQuest
    {
        public ValleyNashtah()
        {
            CreateQuest(590, "Hunter's Guild Task: 10x Valley Nashtah", "hun_tsk_590")
                .IsRepeatable()

                .AddObjectiveKillTarget(1, NPCGroupType.Viscara_ValleyNashtah, 10)
                .AddObjectiveTalkToNPC(2)

                .AddRewardGold(84)
                .AddRewardGuildPoints(GuildType.HuntersGuild, 27);
        }
    }
}
