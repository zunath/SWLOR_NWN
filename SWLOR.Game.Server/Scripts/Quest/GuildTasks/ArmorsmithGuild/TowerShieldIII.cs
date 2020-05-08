using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.Quest;

namespace SWLOR.Game.Server.Scripts.Quest.GuildTasks.ArmorsmithGuild
{
    public class TowerShieldIII: AbstractQuest
    {
        public TowerShieldIII()
        {
            CreateQuest(192, "Armorsmith Guild Task: 1x Tower Shield III", "arm_tsk_192")
                .IsRepeatable()

                .AddObjectiveCollectItem(1, "tower_shield_3", 1, true)

                .AddRewardGold(320)
                .AddRewardGuildPoints(GuildType.ArmorsmithGuild, 68);
        }
    }
}
