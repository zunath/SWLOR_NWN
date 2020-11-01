using SWLOR.Game.Server.Legacy.Enumeration;
using SWLOR.Game.Server.Legacy.Quest;

namespace SWLOR.Game.Server.Legacy.Scripts.Quest.GuildTasks.EngineeringGuild
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
