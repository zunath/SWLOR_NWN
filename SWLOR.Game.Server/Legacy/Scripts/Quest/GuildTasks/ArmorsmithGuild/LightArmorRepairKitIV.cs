using SWLOR.Game.Server.Legacy.Enumeration;
using SWLOR.Game.Server.Legacy.Quest;

namespace SWLOR.Game.Server.Legacy.Scripts.Quest.GuildTasks.ArmorsmithGuild
{
    public class LightArmorRepairKitIV: AbstractQuest
    {
        public LightArmorRepairKitIV()
        {
            CreateQuest(208, "Armorsmith Guild Task: 1x Light Armor Repair Kit IV", "arm_tsk_208")
                .IsRepeatable()

                .AddObjectiveCollectItem(1, "la_rep_4", 1, true)

                .AddRewardGold(420)
                .AddRewardGuildPoints(GuildType.ArmorsmithGuild, 88);
        }
    }
}
