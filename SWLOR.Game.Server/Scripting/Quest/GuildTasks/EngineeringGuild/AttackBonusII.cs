using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.Quest;

namespace SWLOR.Game.Server.Scripting.Quest.GuildTasks.EngineeringGuild
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
