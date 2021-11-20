using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.Quest;

namespace SWLOR.Game.Server.Scripts.Quest.GuildTasks.EngineeringGuild
{
    public class IntelligenceI: AbstractQuest
    {
        public IntelligenceI()
        {
            CreateQuest(397, "Engineering Guild Task: 1x Intelligence I", "eng_tsk_397")
                .IsRepeatable()

                .AddObjectiveCollectItem(1, "rune_int1", 1, true)

                .AddRewardGold(70)
                .AddRewardGuildPoints(GuildType.EngineeringGuild, 15);
        }
    }
}
