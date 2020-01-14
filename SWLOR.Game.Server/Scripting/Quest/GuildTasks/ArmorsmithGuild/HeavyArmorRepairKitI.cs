using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.Quest;

namespace SWLOR.Game.Server.Scripting.Quest.GuildTasks.ArmorsmithGuild
{
    public class HeavyArmorRepairKitI: AbstractQuest
    {
        public HeavyArmorRepairKitI()
        {
            CreateQuest(129, "Armorsmith Guild Task: 1x Heavy Armor Repair Kit I", "arm_tsk_129")
                .IsRepeatable()
				.IsGuildTask(GuildType.ArmorsmithGuild, 1)


                .AddObjectiveCollectItem(1, "ha_rep_1", 1, true)

                .AddRewardGold(155)
                .AddRewardGuildPoints(GuildType.ArmorsmithGuild, 37);
        }
    }
}
