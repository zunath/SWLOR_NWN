using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.Quest;

namespace SWLOR.Game.Server.Scripting.Quest.GuildTasks.EngineeringGuild
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
