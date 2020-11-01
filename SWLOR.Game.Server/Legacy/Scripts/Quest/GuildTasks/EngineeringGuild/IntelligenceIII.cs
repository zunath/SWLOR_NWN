using SWLOR.Game.Server.Legacy.Enumeration;
using SWLOR.Game.Server.Legacy.Quest;

namespace SWLOR.Game.Server.Legacy.Scripts.Quest.GuildTasks.EngineeringGuild
{
    public class IntelligenceIII: AbstractQuest
    {
        public IntelligenceIII()
        {
            CreateQuest(533, "Engineering Guild Task: 1x Intelligence III", "eng_tsk_533")
                .IsRepeatable()

                .AddObjectiveCollectItem(1, "rune_int3", 1, true)

                .AddRewardGold(430)
                .AddRewardGuildPoints(GuildType.EngineeringGuild, 90);
        }
    }
}
