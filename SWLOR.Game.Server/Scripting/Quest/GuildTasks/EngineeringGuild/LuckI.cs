using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.Quest;

namespace SWLOR.Game.Server.Scripting.Quest.GuildTasks.EngineeringGuild
{
    public class LuckI: AbstractQuest
    {
        public LuckI()
        {
            CreateQuest(434, "Engineering Guild Task: 1x Luck I", "eng_tsk_434")
                .IsRepeatable()

                .AddObjectiveCollectItem(1, "rune_luck1", 1, true)

                .AddRewardGold(190)
                .AddRewardGuildPoints(GuildType.EngineeringGuild, 40);
        }
    }
}
