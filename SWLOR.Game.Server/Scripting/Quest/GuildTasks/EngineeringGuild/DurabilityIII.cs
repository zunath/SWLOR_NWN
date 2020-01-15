using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.Quest;

namespace SWLOR.Game.Server.Scripting.Quest.GuildTasks.EngineeringGuild
{
    public class DurabilityIII: AbstractQuest
    {
        public DurabilityIII()
        {
            CreateQuest(518, "Engineering Guild Task: 1x Durability III", "eng_tsk_518")
                .IsRepeatable()
				.IsGuildTask(GuildType.EngineeringGuild, 4)


                .AddObjectiveCollectItem(1, "rune_dur3", 1, true)

                .AddRewardGold(430)
                .AddRewardGuildPoints(GuildType.EngineeringGuild, 90);
        }
    }
}
