using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.Quest;

namespace SWLOR.Game.Server.Scripting.Quest.GuildTasks.EngineeringGuild
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
