using SWLOR.Game.Server.Legacy.Enumeration;
using SWLOR.Game.Server.Legacy.Quest;

namespace SWLOR.Game.Server.Legacy.Scripts.Quest.GuildTasks.EngineeringGuild
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
