using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.Quest;

namespace SWLOR.Game.Server.Scripting.Quest.GuildTasks.ArmorsmithGuild
{
    public class HeavyArmorRepairKitII: AbstractQuest
    {
        public HeavyArmorRepairKitII()
        {
            CreateQuest(152, "Armorsmith Guild Task: 1x Heavy Armor Repair Kit II", "arm_tsk_152")
                .IsRepeatable()

                .AddObjectiveCollectItem(1, "ha_rep_2", 1, true)

                .AddRewardGold(255)
                .AddRewardGuildPoints(GuildType.ArmorsmithGuild, 57);
        }
    }
}
