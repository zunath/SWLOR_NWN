using SWLOR.Game.Server.Legacy.Enumeration;
using SWLOR.Game.Server.Legacy.Quest;

namespace SWLOR.Game.Server.Legacy.Scripts.Quest.GuildTasks.EngineeringGuild
{
    public class FPRegenII: AbstractQuest
    {
        public FPRegenII()
        {
            CreateQuest(471, "Engineering Guild Task: 1x FP Regen II", "eng_tsk_471")
                .IsRepeatable()

                .AddObjectiveCollectItem(1, "rune_manareg2", 1, true)

                .AddRewardGold(310)
                .AddRewardGuildPoints(GuildType.EngineeringGuild, 65);
        }
    }
}
