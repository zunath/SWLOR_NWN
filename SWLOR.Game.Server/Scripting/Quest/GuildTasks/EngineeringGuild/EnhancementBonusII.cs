using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.Quest;

namespace SWLOR.Game.Server.Scripting.Quest.GuildTasks.EngineeringGuild
{
    public class EnhancementBonusII: AbstractQuest
    {
        public EnhancementBonusII()
        {
            CreateQuest(467, "Engineering Guild Task: 1x Enhancement Bonus II", "eng_tsk_467")
                .IsRepeatable()

                .AddObjectiveCollectItem(1, "rune_eb2", 1, true)

                .AddRewardGold(310)
                .AddRewardGuildPoints(GuildType.EngineeringGuild, 65);
        }
    }
}
