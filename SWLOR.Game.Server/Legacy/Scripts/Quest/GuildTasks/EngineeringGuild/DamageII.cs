using SWLOR.Game.Server.Legacy.Enumeration;
using SWLOR.Game.Server.Legacy.Quest;

namespace SWLOR.Game.Server.Legacy.Scripts.Quest.GuildTasks.EngineeringGuild
{
    public class DamageII: AbstractQuest
    {
        public DamageII()
        {
            CreateQuest(460, "Engineering Guild Task: 1x Damage II", "eng_tsk_460")
                .IsRepeatable()

                .AddObjectiveCollectItem(1, "rune_dmg2", 1, true)

                .AddRewardGold(310)
                .AddRewardGuildPoints(GuildType.EngineeringGuild, 65);
        }
    }
}
