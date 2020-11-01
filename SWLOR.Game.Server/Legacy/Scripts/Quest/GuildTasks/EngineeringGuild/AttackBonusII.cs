using SWLOR.Game.Server.Legacy.Enumeration;
using SWLOR.Game.Server.Legacy.Quest;

namespace SWLOR.Game.Server.Legacy.Scripts.Quest.GuildTasks.EngineeringGuild
{
    public class AttackBonusII: AbstractQuest
    {
        public AttackBonusII()
        {
            CreateQuest(412, "Engineering Guild Task: 1x Attack Bonus II", "eng_tsk_412")
                .IsRepeatable()

                .AddObjectiveCollectItem(1, "rune_ab2", 1, true)

                .AddRewardGold(190)
                .AddRewardGuildPoints(GuildType.EngineeringGuild, 40);
        }
    }
}
