using SWLOR.Game.Server.Legacy.Enumeration;
using SWLOR.Game.Server.Legacy.Quest;

namespace SWLOR.Game.Server.Legacy.Scripts.Quest.GuildTasks.EngineeringGuild
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
