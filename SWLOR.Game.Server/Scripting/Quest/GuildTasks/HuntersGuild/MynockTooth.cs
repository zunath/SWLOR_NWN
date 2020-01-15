using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.Quest;

namespace SWLOR.Game.Server.Scripting.Quest.GuildTasks.HuntersGuild
{
    public class MynockTooth: AbstractQuest
    {
        public MynockTooth()
        {
            CreateQuest(576, "Hunter's Guild Task: 6x Mynock Tooth", "hun_tsk_576")
                .IsRepeatable()
				.IsGuildTask(GuildType.HuntersGuild, 0)


                .AddObjectiveCollectItem(1, "mynock_tooth", 6, false)

                .AddRewardGold(23)
                .AddRewardGuildPoints(GuildType.HuntersGuild, 7);
        }
    }
}
