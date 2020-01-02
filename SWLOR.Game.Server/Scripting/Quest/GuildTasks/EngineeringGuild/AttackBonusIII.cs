using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.Quest;

namespace SWLOR.Game.Server.Scripting.Quest.GuildTasks.EngineeringGuild
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
