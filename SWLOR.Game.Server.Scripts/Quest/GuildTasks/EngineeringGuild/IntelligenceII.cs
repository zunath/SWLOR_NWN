using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.Quest;

namespace SWLOR.Game.Server.Scripts.Quest.GuildTasks.EngineeringGuild
{
    public class IntelligenceII: AbstractQuest
    {
        public IntelligenceII()
        {
            CreateQuest(426, "Engineering Guild Task: 1x Intelligence II", "eng_tsk_426")
                .IsRepeatable()

                .AddObjectiveCollectItem(1, "rune_int2", 1, true)

                .AddRewardGold(190)
                .AddRewardGuildPoints(GuildType.EngineeringGuild, 40);
        }
    }
}
