using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.Quest;

namespace SWLOR.Game.Server.Scripting.Quest.GuildTasks.ArmorsmithGuild
{
    public class ForceArmorRepairKitI: AbstractQuest
    {
        public ForceArmorRepairKitI()
        {
            CreateQuest(123, "Armorsmith Guild Task: 1x Force Armor Repair Kit I", "arm_tsk_123")
                .IsRepeatable()
				.IsGuildTask(GuildType.ArmorsmithGuild, 1)


                .AddObjectiveCollectItem(1, "fa_rep_1", 1, true)

                .AddRewardGold(120)
                .AddRewardGuildPoints(GuildType.ArmorsmithGuild, 28);
        }
    }
}
