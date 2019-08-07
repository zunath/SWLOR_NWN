using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.Quest;

namespace SWLOR.Game.Server.Scripts.Quest.GuildTasks.EngineeringGuild
{
    public class BaseAttackBonusI: AbstractQuest
    {
        public BaseAttackBonusI()
        {
            CreateQuest(413, "Engineering Guild Task: 1x Base Attack Bonus I", "eng_tsk_413")
                .IsRepeatable()

                .AddObjectiveCollectItem(1, "rune_bab1", 1, true)

                .AddRewardGold(190)
                .AddRewardGuildPoints(GuildType.EngineeringGuild, 40);
        }
    }
}
