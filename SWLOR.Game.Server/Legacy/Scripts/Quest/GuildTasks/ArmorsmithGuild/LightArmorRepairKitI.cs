using SWLOR.Game.Server.Legacy.Enumeration;
using SWLOR.Game.Server.Legacy.Quest;

namespace SWLOR.Game.Server.Legacy.Scripts.Quest.GuildTasks.ArmorsmithGuild
{
    public class LightArmorRepairKitI: AbstractQuest
    {
        public LightArmorRepairKitI()
        {
            CreateQuest(136, "Armorsmith Guild Task: 1x Light Armor Repair Kit I", "arm_tsk_136")
                .IsRepeatable()

                .AddObjectiveCollectItem(1, "la_rep_1", 1, true)

                .AddRewardGold(120)
                .AddRewardGuildPoints(GuildType.ArmorsmithGuild, 28);
        }
    }
}
