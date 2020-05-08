using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.Quest;

namespace SWLOR.Game.Server.Scripts.Quest.GuildTasks.ArmorsmithGuild
{
    public class ForceArmorRepairKitIV: AbstractQuest
    {
        public ForceArmorRepairKitIV()
        {
            CreateQuest(196, "Armorsmith Guild Task: 1x Force Armor Repair Kit IV", "arm_tsk_196")
                .IsRepeatable()

                .AddObjectiveCollectItem(1, "fa_rep_4", 1, true)

                .AddRewardGold(420)
                .AddRewardGuildPoints(GuildType.ArmorsmithGuild, 88);
        }
    }
}
