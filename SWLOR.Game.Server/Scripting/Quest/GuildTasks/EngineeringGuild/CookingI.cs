using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.Quest;

namespace SWLOR.Game.Server.Scripting.Quest.GuildTasks.EngineeringGuild
{
    public class CookingI: AbstractQuest
    {
        public CookingI()
        {
            CreateQuest(381, "Engineering Guild Task: 1x Cooking I", "eng_tsk_381")
                .IsRepeatable()

                .AddObjectiveCollectItem(1, "rune_cooking1", 1, true)

                .AddRewardGold(70)
                .AddRewardGuildPoints(GuildType.EngineeringGuild, 15);
        }
    }
}
