using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.Quest;

namespace SWLOR.Game.Server.Scripts.Quest.GuildTasks.EngineeringGuild
{
    public class WisdomII: AbstractQuest
    {
        public WisdomII()
        {
            CreateQuest(447, "Engineering Guild Task: 1x Wisdom II", "eng_tsk_447")
                .IsRepeatable()

                .AddObjectiveCollectItem(1, "rune_wis2", 1, true)

                .AddRewardGold(190)
                .AddRewardGuildPoints(GuildType.EngineeringGuild, 40);
        }
    }
}
