using SWLOR.Game.Server.Legacy.Enumeration;
using SWLOR.Game.Server.Legacy.Quest;

namespace SWLOR.Game.Server.Legacy.Scripts.Quest.GuildTasks.HuntersGuild
{
    public class WildwoodsOutlaw: AbstractQuest
    {
        public WildwoodsOutlaw()
        {
            CreateQuest(572, "Hunter's Guild Task: 10x Wildwoods Outlaw", "hun_tsk_572")
                .IsRepeatable()

                .AddObjectiveKillTarget(1, NPCGroupType.Viscara_WildwoodsOutlaws, 10)
                .AddObjectiveTalkToNPC(2)

                .AddRewardGold(65)
                .AddRewardGuildPoints(GuildType.HuntersGuild, 12);
        }
    }
}
