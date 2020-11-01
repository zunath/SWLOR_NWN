using SWLOR.Game.Server.Legacy.Enumeration;
using SWLOR.Game.Server.Legacy.Quest;

namespace SWLOR.Game.Server.Legacy.Scripts.Quest.GuildTasks.EngineeringGuild
{
    public class SneakAttackI: AbstractQuest
    {
        public SneakAttackI()
        {
            CreateQuest(407, "Engineering Guild Task: 1x Sneak Attack I", "eng_tsk_407")
                .IsRepeatable()

                .AddObjectiveCollectItem(1, "rune_snkatk1", 1, true)

                .AddRewardGold(70)
                .AddRewardGuildPoints(GuildType.EngineeringGuild, 15);
        }
    }
}
