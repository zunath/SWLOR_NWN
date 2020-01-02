using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.Quest;

namespace SWLOR.Game.Server.Scripting.Quest.GuildTasks.EngineeringGuild
{
    public class WisdomIII: AbstractQuest
    {
        public WisdomIII()
        {
            CreateQuest(557, "Engineering Guild Task: 1x Wisdom III", "eng_tsk_557")
                .IsRepeatable()

                .AddObjectiveCollectItem(1, "rune_wis3", 1, true)

                .AddRewardGold(430)
                .AddRewardGuildPoints(GuildType.EngineeringGuild, 90);
        }
    }
}
