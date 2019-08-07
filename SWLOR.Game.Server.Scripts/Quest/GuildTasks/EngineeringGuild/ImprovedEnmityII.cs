using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.Quest;

namespace SWLOR.Game.Server.Scripts.Quest.GuildTasks.EngineeringGuild
{
    public class ImprovedEnmityII: AbstractQuest
    {
        public ImprovedEnmityII()
        {
            CreateQuest(476, "Engineering Guild Task: 1x Improved Enmity II", "eng_tsk_476")
                .IsRepeatable()

                .AddObjectiveCollectItem(1, "rune_enmup2", 1, true)

                .AddRewardGold(310)
                .AddRewardGuildPoints(GuildType.EngineeringGuild, 65);
        }
    }
}
