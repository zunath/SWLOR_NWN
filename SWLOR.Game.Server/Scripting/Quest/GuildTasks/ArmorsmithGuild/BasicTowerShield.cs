using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.Quest;

namespace SWLOR.Game.Server.Scripting.Quest.GuildTasks.ArmorsmithGuild
{
    public class BasicTowerShield: AbstractQuest
    {
        public BasicTowerShield()
        {
            CreateQuest(113, "Armorsmith Guild Task: 1x Basic Tower Shield", "arm_tsk_113")
                .IsRepeatable()

                .AddObjectiveCollectItem(1, "tower_shield_b", 1, true)

                .AddRewardGold(70)
                .AddRewardGuildPoints(GuildType.ArmorsmithGuild, 18);
        }
    }
}
