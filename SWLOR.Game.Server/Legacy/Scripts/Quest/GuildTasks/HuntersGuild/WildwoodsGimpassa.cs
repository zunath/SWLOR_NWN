using SWLOR.Game.Server.Legacy.Enumeration;
using SWLOR.Game.Server.Legacy.Quest;

namespace SWLOR.Game.Server.Legacy.Scripts.Quest.GuildTasks.HuntersGuild
{
    public class WildwoodsGimpassa: AbstractQuest
    {
        public WildwoodsGimpassa()
        {
            CreateQuest(573, "Hunter's Guild Task: 8x Wildwoods Gimpassa", "hun_tsk_573")
                .IsRepeatable()

                .AddObjectiveKillTarget(1, NPCGroupType.Viscara_WildwoodsGimpassas, 8)
                .AddObjectiveTalkToNPC(2)

                .AddRewardGold(70)
                .AddRewardGuildPoints(GuildType.HuntersGuild, 13);
        }
    }
}
