using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.Quest;

namespace SWLOR.Game.Server.Scripting.Quest.GuildTasks.HuntersGuild
{
    public class AmphiHydrusBrain: AbstractQuest
    {
        public AmphiHydrusBrain()
        {
            CreateQuest(608, "Hunter's Guild Task: 6x Amphi-Hydrus Brain", "hun_tsk_608")
                .IsRepeatable()
				.IsGuildTask(GuildType.HuntersGuild, 2)


                .AddObjectiveCollectItem(1, "amphi_brain", 6, false)

                .AddRewardGold(212)
                .AddRewardGuildPoints(GuildType.HuntersGuild, 52);
        }
    }
}
