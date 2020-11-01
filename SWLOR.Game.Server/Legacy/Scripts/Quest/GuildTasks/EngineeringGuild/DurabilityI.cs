using SWLOR.Game.Server.Legacy.Enumeration;
using SWLOR.Game.Server.Legacy.Quest;

namespace SWLOR.Game.Server.Legacy.Scripts.Quest.GuildTasks.EngineeringGuild
{
    public class DurabilityI: AbstractQuest
    {
        public DurabilityI()
        {
            CreateQuest(421, "Engineering Guild Task: 1x Durability I", "eng_tsk_421")
                .IsRepeatable()

                .AddObjectiveCollectItem(1, "rune_dur1", 1, true)

                .AddRewardGold(190)
                .AddRewardGuildPoints(GuildType.EngineeringGuild, 40);
        }
    }
}
