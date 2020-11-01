using SWLOR.Game.Server.Legacy.Enumeration;
using SWLOR.Game.Server.Legacy.Quest;

namespace SWLOR.Game.Server.Legacy.Scripts.Quest.GuildTasks.EngineeringGuild
{
    public class HarvestingIII: AbstractQuest
    {
        public HarvestingIII()
        {
            CreateQuest(528, "Engineering Guild Task: 1x Harvesting III", "eng_tsk_528")
                .IsRepeatable()

                .AddObjectiveCollectItem(1, "rune_mining3", 1, true)

                .AddRewardGold(430)
                .AddRewardGuildPoints(GuildType.EngineeringGuild, 90);
        }
    }
}
