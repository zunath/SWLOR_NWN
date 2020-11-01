using SWLOR.Game.Server.Legacy.Enumeration;
using SWLOR.Game.Server.Legacy.Quest;

namespace SWLOR.Game.Server.Legacy.Scripts.Quest.GuildTasks.EngineeringGuild
{
    public class FPIII: AbstractQuest
    {
        public FPIII()
        {
            CreateQuest(525, "Engineering Guild Task: 1x FP III", "eng_tsk_525")
                .IsRepeatable()

                .AddObjectiveCollectItem(1, "rune_mana3", 1, true)

                .AddRewardGold(430)
                .AddRewardGuildPoints(GuildType.EngineeringGuild, 90);
        }
    }
}
