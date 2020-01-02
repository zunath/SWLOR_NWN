using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.Quest;

namespace SWLOR.Game.Server.Scripting.Quest.GuildTasks.EngineeringGuild
{
    public class FabricationII: AbstractQuest
    {
        public FabricationII()
        {
            CreateQuest(468, "Engineering Guild Task: 1x Fabrication II", "eng_tsk_468")
                .IsRepeatable()

                .AddObjectiveCollectItem(1, "rune_fab2", 1, true)

                .AddRewardGold(310)
                .AddRewardGuildPoints(GuildType.EngineeringGuild, 65);
        }
    }
}
