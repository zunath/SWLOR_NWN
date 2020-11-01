using SWLOR.Game.Server.Legacy.Enumeration;
using SWLOR.Game.Server.Legacy.Quest;

namespace SWLOR.Game.Server.Legacy.Scripts.Quest.GuildTasks.EngineeringGuild
{
    public class CookingII: AbstractQuest
    {
        public CookingII()
        {
            CreateQuest(459, "Engineering Guild Task: 1x Cooking II", "eng_tsk_459")
                .IsRepeatable()

                .AddObjectiveCollectItem(1, "rune_cooking2", 1, true)

                .AddRewardGold(310)
                .AddRewardGuildPoints(GuildType.EngineeringGuild, 65);
        }
    }
}
