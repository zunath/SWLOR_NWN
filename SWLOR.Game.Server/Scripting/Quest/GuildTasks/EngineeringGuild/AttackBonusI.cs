using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.Quest;

namespace SWLOR.Game.Server.Scripting.Quest.GuildTasks.EngineeringGuild
{
    public class AttackBonusI: AbstractQuest
    {
        public AttackBonusI()
        {
            CreateQuest(366, "Engineering Guild Task: 1x Attack Bonus I", "eng_tsk_366")
                .IsRepeatable()

                .AddObjectiveCollectItem(1, "rune_ab1", 1, true)

                .AddRewardGold(70)
                .AddRewardGuildPoints(GuildType.EngineeringGuild, 15);
        }
    }
}
