using SWLOR.Game.Server.Legacy.Enumeration;
using SWLOR.Game.Server.Legacy.Quest;

namespace SWLOR.Game.Server.Legacy.Scripts.Quest.GuildTasks.HuntersGuild
{
    public class ViscaraKathHound: AbstractQuest
    {
        public ViscaraKathHound()
        {
            CreateQuest(570, "Hunter's Guild Task: 10x Viscara Kath Hound", "hun_tsk_570")
                .IsRepeatable()

                .AddObjectiveKillTarget(1, NPCGroupType.Viscara_WildlandKathHounds, 10)
                .AddObjectiveTalkToNPC(2)

                .AddRewardGold(65)
                .AddRewardGuildPoints(GuildType.HuntersGuild, 12);
        }
    }
}
