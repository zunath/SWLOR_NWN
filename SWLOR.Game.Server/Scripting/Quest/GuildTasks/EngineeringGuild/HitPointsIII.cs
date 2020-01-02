using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.Quest;

namespace SWLOR.Game.Server.Scripting.Quest.GuildTasks.EngineeringGuild
{
    public class HitPointsIII: AbstractQuest
    {
        public HitPointsIII()
        {
            CreateQuest(529, "Engineering Guild Task: 1x Hit Points III", "eng_tsk_529")
                .IsRepeatable()

                .AddObjectiveCollectItem(1, "rune_hp3", 1, true)

                .AddRewardGold(430)
                .AddRewardGuildPoints(GuildType.EngineeringGuild, 90);
        }
    }
}
