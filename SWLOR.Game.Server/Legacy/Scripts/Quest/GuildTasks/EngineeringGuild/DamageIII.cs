using SWLOR.Game.Server.Legacy.Enumeration;
using SWLOR.Game.Server.Legacy.Quest;

namespace SWLOR.Game.Server.Legacy.Scripts.Quest.GuildTasks.EngineeringGuild
{
    public class DamageIII: AbstractQuest
    {
        public DamageIII()
        {
            CreateQuest(514, "Engineering Guild Task: 1x Damage III", "eng_tsk_514")
                .IsRepeatable()

                .AddObjectiveCollectItem(1, "rune_dmg3", 1, true)

                .AddRewardGold(430)
                .AddRewardGuildPoints(GuildType.EngineeringGuild, 90);
        }
    }
}
