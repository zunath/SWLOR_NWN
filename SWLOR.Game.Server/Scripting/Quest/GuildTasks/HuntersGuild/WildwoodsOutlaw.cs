using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.Quest;

namespace SWLOR.Game.Server.Scripting.Quest.GuildTasks.HuntersGuild
{
    public class WildwoodsOutlaw: AbstractQuest
    {
        public WildwoodsOutlaw()
        {
            CreateQuest(572, "Hunter's Guild Task: 10x Wildwoods Outlaw", "hun_tsk_572")
                .IsRepeatable()
				.IsGuildTask(GuildType.HuntersGuild, 0)


                .AddObjectiveKillTarget(1, NPCGroup.WildwoodsOutlaws, 10)
                .AddObjectiveTalkToNPC(2)

                .AddRewardGold(65)
                .AddRewardGuildPoints(GuildType.HuntersGuild, 12);
        }
    }
}
