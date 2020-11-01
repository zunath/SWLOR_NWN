using SWLOR.Game.Server.Legacy.Enumeration;
using SWLOR.Game.Server.Legacy.Quest;

namespace SWLOR.Game.Server.Legacy.Scripts.Quest.GuildTasks.EngineeringGuild
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
