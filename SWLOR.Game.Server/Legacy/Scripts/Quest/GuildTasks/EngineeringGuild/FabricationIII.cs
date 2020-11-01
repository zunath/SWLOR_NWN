using SWLOR.Game.Server.Legacy.Enumeration;
using SWLOR.Game.Server.Legacy.Quest;

namespace SWLOR.Game.Server.Legacy.Scripts.Quest.GuildTasks.EngineeringGuild
{
    public class FabricationIII: AbstractQuest
    {
        public FabricationIII()
        {
            CreateQuest(523, "Engineering Guild Task: 1x Fabrication III", "eng_tsk_523")
                .IsRepeatable()

                .AddObjectiveCollectItem(1, "rune_fab3", 1, true)

                .AddRewardGold(430)
                .AddRewardGuildPoints(GuildType.EngineeringGuild, 90);
        }
    }
}
