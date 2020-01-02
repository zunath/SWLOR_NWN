using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.Quest;

namespace SWLOR.Game.Server.Scripting.Quest.GuildTasks.EngineeringGuild
{
    public class ReducedEnmityII: AbstractQuest
    {
        public ReducedEnmityII()
        {
            CreateQuest(486, "Engineering Guild Task: 1x Reduced Enmity II", "eng_tsk_486")
                .IsRepeatable()

                .AddObjectiveCollectItem(1, "rune_enmdown2", 1, true)

                .AddRewardGold(310)
                .AddRewardGuildPoints(GuildType.EngineeringGuild, 65);
        }
    }
}
