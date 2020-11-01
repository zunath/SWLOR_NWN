using SWLOR.Game.Server.Legacy.Enumeration;
using SWLOR.Game.Server.Legacy.Quest;

namespace SWLOR.Game.Server.Legacy.Scripts.Quest.GuildTasks.EngineeringGuild
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
