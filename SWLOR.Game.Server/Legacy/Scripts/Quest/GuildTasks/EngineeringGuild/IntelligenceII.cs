using SWLOR.Game.Server.Legacy.Enumeration;
using SWLOR.Game.Server.Legacy.Quest;

namespace SWLOR.Game.Server.Legacy.Scripts.Quest.GuildTasks.EngineeringGuild
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
