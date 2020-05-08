using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.Quest;

namespace SWLOR.Game.Server.Scripts.Quest.GuildTasks.ArmorsmithGuild
{
    public class HeavyArmorRepairKitIII: AbstractQuest
    {
        public HeavyArmorRepairKitIII()
        {
            CreateQuest(178, "Armorsmith Guild Task: 1x Heavy Armor Repair Kit III", "arm_tsk_178")
                .IsRepeatable()

                .AddObjectiveCollectItem(1, "ha_rep_3", 1, true)

                .AddRewardGold(355)
                .AddRewardGuildPoints(GuildType.ArmorsmithGuild, 77);
        }
    }
}
