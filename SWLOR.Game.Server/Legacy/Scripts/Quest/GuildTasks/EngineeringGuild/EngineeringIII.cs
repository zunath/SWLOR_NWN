using SWLOR.Game.Server.Legacy.Enumeration;
using SWLOR.Game.Server.Legacy.Quest;

namespace SWLOR.Game.Server.Legacy.Scripts.Quest.GuildTasks.EngineeringGuild
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
