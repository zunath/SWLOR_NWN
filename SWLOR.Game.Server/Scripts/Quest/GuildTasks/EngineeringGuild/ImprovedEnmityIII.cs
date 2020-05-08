using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.Quest;

namespace SWLOR.Game.Server.Scripts.Quest.GuildTasks.EngineeringGuild
{
    public class ImprovedEnmityIII: AbstractQuest
    {
        public ImprovedEnmityIII()
        {
            CreateQuest(532, "Engineering Guild Task: 1x Improved Enmity III", "eng_tsk_532")
                .IsRepeatable()

                .AddObjectiveCollectItem(1, "rune_enmup3", 1, true)

                .AddRewardGold(430)
                .AddRewardGuildPoints(GuildType.EngineeringGuild, 90);
        }
    }
}
