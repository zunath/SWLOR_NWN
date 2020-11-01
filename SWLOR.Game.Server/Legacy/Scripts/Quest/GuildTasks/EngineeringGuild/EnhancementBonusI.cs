using SWLOR.Game.Server.Legacy.Enumeration;
using SWLOR.Game.Server.Legacy.Quest;

namespace SWLOR.Game.Server.Legacy.Scripts.Quest.GuildTasks.EngineeringGuild
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
