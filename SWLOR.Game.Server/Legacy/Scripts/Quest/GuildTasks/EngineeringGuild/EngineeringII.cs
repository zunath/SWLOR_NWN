using SWLOR.Game.Server.Legacy.Enumeration;
using SWLOR.Game.Server.Legacy.Quest;

namespace SWLOR.Game.Server.Legacy.Scripts.Quest.GuildTasks.EngineeringGuild
{
    public class EngineeringII: AbstractQuest
    {
        public EngineeringII()
        {
            CreateQuest(466, "Engineering Guild Task: 1x Engineering II", "eng_tsk_466")
                .IsRepeatable()

                .AddObjectiveCollectItem(1, "rune_engin2", 1, true)

                .AddRewardGold(310)
                .AddRewardGuildPoints(GuildType.EngineeringGuild, 65);
        }
    }
}
