using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.Quest;

namespace SWLOR.Game.Server.Scripting.Quest.GuildTasks.HuntersGuild
{
    public class KathHoundFur: AbstractQuest
    {
        public KathHoundFur()
        {
            CreateQuest(579, "Hunter's Guild Task: 6x Kath Hound Fur", "hun_tsk_579")
                .IsRepeatable()
				.IsGuildTask(GuildType.HuntersGuild, 0)


                .AddObjectiveCollectItem(1, "k_hound_fur", 6, false)

                .AddRewardGold(65)
                .AddRewardGuildPoints(GuildType.HuntersGuild, 12);
        }
    }
}
