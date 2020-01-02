using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.Quest;

namespace SWLOR.Game.Server.Scripting.Quest.GuildTasks.EngineeringGuild
{
    public class HitPointsII: AbstractQuest
    {
        public HitPointsII()
        {
            CreateQuest(474, "Engineering Guild Task: 1x Hit Points II", "eng_tsk_474")
                .IsRepeatable()

                .AddObjectiveCollectItem(1, "rune_hp2", 1, true)

                .AddRewardGold(310)
                .AddRewardGuildPoints(GuildType.EngineeringGuild, 65);
        }
    }
}
