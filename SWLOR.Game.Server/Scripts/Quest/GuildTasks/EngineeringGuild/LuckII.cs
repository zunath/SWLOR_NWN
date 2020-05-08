using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.Quest;

namespace SWLOR.Game.Server.Scripts.Quest.GuildTasks.EngineeringGuild
{
    public class LuckII: AbstractQuest
    {
        public LuckII()
        {
            CreateQuest(435, "Engineering Guild Task: 1x Luck II", "eng_tsk_435")
                .IsRepeatable()

                .AddObjectiveCollectItem(1, "rune_luck2", 1, true)

                .AddRewardGold(230)
                .AddRewardGuildPoints(GuildType.EngineeringGuild, 50);
        }
    }
}
