using SWLOR.Game.Server.Legacy.Enumeration;
using SWLOR.Game.Server.Legacy.Quest;

namespace SWLOR.Game.Server.Legacy.Scripts.Quest.GuildTasks.EngineeringGuild
{
    public class ReducedEnmityI: AbstractQuest
    {
        public ReducedEnmityI()
        {
            CreateQuest(438, "Engineering Guild Task: 1x Reduced Enmity I", "eng_tsk_438")
                .IsRepeatable()

                .AddObjectiveCollectItem(1, "rune_enmdown1", 1, true)

                .AddRewardGold(190)
                .AddRewardGuildPoints(GuildType.EngineeringGuild, 40);
        }
    }
}
