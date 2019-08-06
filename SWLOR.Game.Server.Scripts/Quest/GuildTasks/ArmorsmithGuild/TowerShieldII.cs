using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.Quest;

namespace SWLOR.Game.Server.Scripts.Quest.GuildTasks.ArmorsmithGuild
{
    public class TowerShieldII: AbstractQuest
    {
        public TowerShieldII()
        {
            CreateQuest(168, "Armorsmith Guild Task: 1x Tower Shield II", "arm_tsk_168")
                .IsRepeatable()

                .AddObjectiveCollectItem(1, "tower_shield_2", 1, true)

                .AddRewardGold(220)
                .AddRewardGuildPoints(GuildType.ArmorsmithGuild, 48);
        }
    }
}
