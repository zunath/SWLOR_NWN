using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.Quest;

namespace SWLOR.Game.Server.Scripting.Quest.GuildTasks.ArmorsmithGuild
{
    public class TowerShieldIV: AbstractQuest
    {
        public TowerShieldIV()
        {
            CreateQuest(221, "Armorsmith Guild Task: 1x Tower Shield IV", "arm_tsk_221")
                .IsRepeatable()

                .AddObjectiveCollectItem(1, "tower_shield_4", 1, true)

                .AddRewardGold(420)
                .AddRewardGuildPoints(GuildType.ArmorsmithGuild, 88);
        }
    }
}
