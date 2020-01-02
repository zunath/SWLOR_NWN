using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.Quest;

namespace SWLOR.Game.Server.Scripting.Quest.GuildTasks.EngineeringGuild
{
    public class FPI: AbstractQuest
    {
        public FPI()
        {
            CreateQuest(392, "Engineering Guild Task: 1x FP I", "eng_tsk_392")
                .IsRepeatable()

                .AddObjectiveCollectItem(1, "rune_mana1", 1, true)

                .AddRewardGold(70)
                .AddRewardGuildPoints(GuildType.EngineeringGuild, 15);
        }
    }
}
