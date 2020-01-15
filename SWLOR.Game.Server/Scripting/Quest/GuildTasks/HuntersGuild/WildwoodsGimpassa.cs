using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.Quest;

namespace SWLOR.Game.Server.Scripting.Quest.GuildTasks.HuntersGuild
{
    public class WildwoodsGimpassa: AbstractQuest
    {
        public WildwoodsGimpassa()
        {
            CreateQuest(573, "Hunter's Guild Task: 8x Wildwoods Gimpassa", "hun_tsk_573")
                .IsRepeatable()
				.IsGuildTask(GuildType.HuntersGuild, 0)


                .AddObjectiveKillTarget(1, NPCGroup.WildwoodsGimpassas, 8)
                .AddObjectiveTalkToNPC(2)

                .AddRewardGold(70)
                .AddRewardGuildPoints(GuildType.HuntersGuild, 13);
        }
    }
}
