using SWLOR.Game.Server.Legacy.Enumeration;
using SWLOR.Game.Server.Legacy.Quest;

namespace SWLOR.Game.Server.Legacy.Scripts.Quest.GuildTasks.EngineeringGuild
{
    public class SneakAttackII: AbstractQuest
    {
        public SneakAttackII()
        {
            CreateQuest(495, "Engineering Guild Task: 1x Sneak Attack II", "eng_tsk_495")
                .IsRepeatable()

                .AddObjectiveCollectItem(1, "rune_snkatk2", 1, true)

                .AddRewardGold(310)
                .AddRewardGuildPoints(GuildType.EngineeringGuild, 65);
        }
    }
}
