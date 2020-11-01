using SWLOR.Game.Server.Legacy.Enumeration;
using SWLOR.Game.Server.Legacy.Quest;

namespace SWLOR.Game.Server.Legacy.Scripts.Quest.GuildTasks.EngineeringGuild
{
    public class EnhancementBonusIII: AbstractQuest
    {
        public EnhancementBonusIII()
        {
            CreateQuest(522, "Engineering Guild Task: 1x Enhancement Bonus III", "eng_tsk_522")
                .IsRepeatable()

                .AddObjectiveCollectItem(1, "rune_eb3", 1, true)

                .AddRewardGold(430)
                .AddRewardGuildPoints(GuildType.EngineeringGuild, 90);
        }
    }
}
