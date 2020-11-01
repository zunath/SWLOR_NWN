using SWLOR.Game.Server.Legacy.Enumeration;
using SWLOR.Game.Server.Legacy.Quest;

namespace SWLOR.Game.Server.Legacy.Scripts.Quest.GuildTasks.EngineeringGuild
{
    public class ImprovedEnmity: AbstractQuest
    {
        public ImprovedEnmity()
        {
            CreateQuest(396, "Engineering Guild Task: 1x Improved Enmity", "eng_tsk_396")
                .IsRepeatable()

                .AddObjectiveCollectItem(1, "rune_enmup1", 1, true)

                .AddRewardGold(70)
                .AddRewardGuildPoints(GuildType.EngineeringGuild, 15);
        }
    }
}
