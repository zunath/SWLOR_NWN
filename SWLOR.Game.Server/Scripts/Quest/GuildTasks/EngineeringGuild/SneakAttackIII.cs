using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.Quest;

namespace SWLOR.Game.Server.Scripts.Quest.GuildTasks.EngineeringGuild
{
    public class SneakAttackIII: AbstractQuest
    {
        public SneakAttackIII()
        {
            CreateQuest(553, "Engineering Guild Task: 1x Sneak Attack III", "eng_tsk_553")
                .IsRepeatable()

                .AddObjectiveCollectItem(1, "rune_snkatk3", 1, true)

                .AddRewardGold(430)
                .AddRewardGuildPoints(GuildType.EngineeringGuild, 90);
        }
    }
}
