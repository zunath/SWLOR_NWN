using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.Quest;

namespace SWLOR.Game.Server.Scripts.Quest.GuildTasks.ArmorsmithGuild
{
    public class ForceArmorRepairKitIII: AbstractQuest
    {
        public ForceArmorRepairKitIII()
        {
            CreateQuest(172, "Armorsmith Guild Task: 1x Force Armor Repair Kit III", "arm_tsk_172")
                .IsRepeatable()

                .AddObjectiveCollectItem(1, "fa_rep_3", 1, true)

                .AddRewardGold(320)
                .AddRewardGuildPoints(GuildType.ArmorsmithGuild, 68);
        }
    }
}
