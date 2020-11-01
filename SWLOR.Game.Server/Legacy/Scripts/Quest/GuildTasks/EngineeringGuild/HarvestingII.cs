using SWLOR.Game.Server.Legacy.Enumeration;
using SWLOR.Game.Server.Legacy.Quest;

namespace SWLOR.Game.Server.Legacy.Scripts.Quest.GuildTasks.EngineeringGuild
{
    public class HarvestingII: AbstractQuest
    {
        public HarvestingII()
        {
            CreateQuest(473, "Engineering Guild Task: 1x Harvesting II", "eng_tsk_473")
                .IsRepeatable()

                .AddObjectiveCollectItem(1, "rune_mining2", 1, true)

                .AddRewardGold(310)
                .AddRewardGuildPoints(GuildType.EngineeringGuild, 65);
        }
    }
}
