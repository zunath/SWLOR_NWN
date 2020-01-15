using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.Quest;

namespace SWLOR.Game.Server.Scripting.Quest.GuildTasks.EngineeringGuild
{
    public class WisdomI: AbstractQuest
    {
        public WisdomI()
        {
            CreateQuest(410, "Engineering Guild Task: 1x Wisdom I", "eng_tsk_410")
                .IsRepeatable()
				.IsGuildTask(GuildType.EngineeringGuild, 1)


                .AddObjectiveCollectItem(1, "rune_wis1", 1, true)

                .AddRewardGold(70)
                .AddRewardGuildPoints(GuildType.EngineeringGuild, 15);
        }
    }
}
