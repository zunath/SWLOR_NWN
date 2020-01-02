using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.Quest;

namespace SWLOR.Game.Server.Scripting.Quest.GuildTasks.EngineeringGuild
{
    public class ReducedEnmityIII: AbstractQuest
    {
        public ReducedEnmityIII()
        {
            CreateQuest(544, "Engineering Guild Task: 1x Reduced Enmity III", "eng_tsk_544")
                .IsRepeatable()

                .AddObjectiveCollectItem(1, "rune_enmdown3", 1, true)

                .AddRewardGold(430)
                .AddRewardGuildPoints(GuildType.EngineeringGuild, 90);
        }
    }
}
