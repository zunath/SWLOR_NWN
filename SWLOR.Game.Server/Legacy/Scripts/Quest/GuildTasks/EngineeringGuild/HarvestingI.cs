using SWLOR.Game.Server.Legacy.Enumeration;
using SWLOR.Game.Server.Legacy.Quest;

namespace SWLOR.Game.Server.Legacy.Scripts.Quest.GuildTasks.EngineeringGuild
{
    public class HarvestingI: AbstractQuest
    {
        public HarvestingI()
        {
            CreateQuest(394, "Engineering Guild Task: 1x Harvesting I", "eng_tsk_394")
                .IsRepeatable()

                .AddObjectiveCollectItem(1, "rune_mining1", 1, true)

                .AddRewardGold(70)
                .AddRewardGuildPoints(GuildType.EngineeringGuild, 15);
        }
    }
}
