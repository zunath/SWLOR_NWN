using SWLOR.Game.Server.Legacy.Enumeration;
using SWLOR.Game.Server.Legacy.Quest;

namespace SWLOR.Game.Server.Legacy.Scripts.Quest.GuildTasks.ArmorsmithGuild
{
    public class TowerShieldI: AbstractQuest
    {
        public TowerShieldI()
        {
            CreateQuest(144, "Armorsmith Guild Task: 1x Tower Shield I", "arm_tsk_144")
                .IsRepeatable()

                .AddObjectiveCollectItem(1, "tower_shield_1", 1, true)

                .AddRewardGold(120)
                .AddRewardGuildPoints(GuildType.ArmorsmithGuild, 28);
        }
    }
}
