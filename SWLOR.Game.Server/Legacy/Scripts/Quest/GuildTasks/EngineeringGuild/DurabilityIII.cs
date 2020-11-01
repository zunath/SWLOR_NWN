using SWLOR.Game.Server.Legacy.Enumeration;
using SWLOR.Game.Server.Legacy.Quest;

namespace SWLOR.Game.Server.Legacy.Scripts.Quest.GuildTasks.EngineeringGuild
{
    public class DurabilityIII: AbstractQuest
    {
        public DurabilityIII()
        {
            CreateQuest(518, "Engineering Guild Task: 1x Durability III", "eng_tsk_518")
                .IsRepeatable()

                .AddObjectiveCollectItem(1, "rune_dur3", 1, true)

                .AddRewardGold(430)
                .AddRewardGuildPoints(GuildType.EngineeringGuild, 90);
        }
    }
}
