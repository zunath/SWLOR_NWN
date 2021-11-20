using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.Quest;

namespace SWLOR.Game.Server.Scripts.Quest.GuildTasks.EngineeringGuild
{
    public class FPRegenIII: AbstractQuest
    {
        public FPRegenIII()
        {
            CreateQuest(526, "Engineering Guild Task: 1x FP Regen III", "eng_tsk_526")
                .IsRepeatable()

                .AddObjectiveCollectItem(1, "rune_manareg3", 1, true)

                .AddRewardGold(430)
                .AddRewardGuildPoints(GuildType.EngineeringGuild, 90);
        }
    }
}
