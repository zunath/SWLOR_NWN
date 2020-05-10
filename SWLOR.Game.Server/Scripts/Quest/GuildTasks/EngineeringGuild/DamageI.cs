using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.Quest;

namespace SWLOR.Game.Server.Scripts.Quest.GuildTasks.EngineeringGuild
{
    public class DamageI: AbstractQuest
    {
        public DamageI()
        {
            CreateQuest(382, "Engineering Guild Task: 1x Damage I", "eng_tsk_382")
                .IsRepeatable()

                .AddObjectiveCollectItem(1, "rune_dmg1", 1, true)

                .AddRewardGold(70)
                .AddRewardGuildPoints(GuildType.EngineeringGuild, 15);
        }
    }
}
