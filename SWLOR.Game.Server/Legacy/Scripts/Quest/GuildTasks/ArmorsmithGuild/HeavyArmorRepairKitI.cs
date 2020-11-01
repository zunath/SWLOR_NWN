using SWLOR.Game.Server.Legacy.Enumeration;
using SWLOR.Game.Server.Legacy.Quest;

namespace SWLOR.Game.Server.Legacy.Scripts.Quest.GuildTasks.ArmorsmithGuild
{
    public class HeavyArmorRepairKitI: AbstractQuest
    {
        public HeavyArmorRepairKitI()
        {
            CreateQuest(129, "Armorsmith Guild Task: 1x Heavy Armor Repair Kit I", "arm_tsk_129")
                .IsRepeatable()

                .AddObjectiveCollectItem(1, "ha_rep_1", 1, true)

                .AddRewardGold(155)
                .AddRewardGuildPoints(GuildType.ArmorsmithGuild, 37);
        }
    }
}
