using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.Quest;

namespace SWLOR.Game.Server.Scripts.Quest.GuildTasks.EngineeringGuild
{
    public class CookingIII: AbstractQuest
    {
        public CookingIII()
        {
            CreateQuest(513, "Engineering Guild Task: 1x Cooking III", "eng_tsk_513")
                .IsRepeatable()

                .AddObjectiveCollectItem(1, "rune_cooking3", 1, true)

                .AddRewardGold(430)
                .AddRewardGuildPoints(GuildType.EngineeringGuild, 90);
        }
    }
}
