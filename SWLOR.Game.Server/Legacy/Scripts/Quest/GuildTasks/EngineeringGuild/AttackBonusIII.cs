using SWLOR.Game.Server.Legacy.Enumeration;
using SWLOR.Game.Server.Legacy.Quest;

namespace SWLOR.Game.Server.Legacy.Scripts.Quest.GuildTasks.EngineeringGuild
{
    public class AttackBonusIII: AbstractQuest
    {
        public AttackBonusIII()
        {
            CreateQuest(501, "Engineering Guild Task: 1x Attack Bonus III", "eng_tsk_501")
                .IsRepeatable()

                .AddObjectiveCollectItem(1, "rune_ab3", 1, true)

                .AddRewardGold(430)
                .AddRewardGuildPoints(GuildType.EngineeringGuild, 90);
        }
    }
}
