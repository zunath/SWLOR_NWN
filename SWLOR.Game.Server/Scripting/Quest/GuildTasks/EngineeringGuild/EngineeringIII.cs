using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.Quest;

namespace SWLOR.Game.Server.Scripting.Quest.GuildTasks.EngineeringGuild
{
    public class EngineeringIII: AbstractQuest
    {
        public EngineeringIII()
        {
            CreateQuest(521, "Engineering Guild Task: 1x Engineering III", "eng_tsk_521")
                .IsRepeatable()

                .AddObjectiveCollectItem(1, "rune_engin3", 1, true)

                .AddRewardGold(430)
                .AddRewardGuildPoints(GuildType.EngineeringGuild, 90);
        }
    }
}
