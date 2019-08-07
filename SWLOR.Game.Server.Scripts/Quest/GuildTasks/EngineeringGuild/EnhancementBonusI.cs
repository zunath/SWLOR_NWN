using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.Quest;

namespace SWLOR.Game.Server.Scripts.Quest.GuildTasks.EngineeringGuild
{
    public class EnhancementBonusI: AbstractQuest
    {
        public EnhancementBonusI()
        {
            CreateQuest(422, "Engineering Guild Task: 1x Enhancement Bonus I", "eng_tsk_422")
                .IsRepeatable()

                .AddObjectiveCollectItem(1, "rune_eb1", 1, true)

                .AddRewardGold(190)
                .AddRewardGuildPoints(GuildType.EngineeringGuild, 40);
        }
    }
}
