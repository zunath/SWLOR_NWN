using SWLOR.Game.Server.Legacy.Enumeration;
using SWLOR.Game.Server.Legacy.Quest;

namespace SWLOR.Game.Server.Legacy.Scripts.Quest.GuildTasks.ArmorsmithGuild
{
    public class HeavyArmorRepairKitIV: AbstractQuest
    {
        public HeavyArmorRepairKitIV()
        {
            CreateQuest(201, "Armorsmith Guild Task: 1x Heavy Armor Repair Kit IV", "arm_tsk_201")
                .IsRepeatable()

                .AddObjectiveCollectItem(1, "ha_rep_4", 1, true)

                .AddRewardGold(455)
                .AddRewardGuildPoints(GuildType.ArmorsmithGuild, 97);
        }
    }
}
