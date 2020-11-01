using SWLOR.Game.Server.Legacy.Enumeration;
using SWLOR.Game.Server.Legacy.Quest;

namespace SWLOR.Game.Server.Legacy.Scripts.Quest.GuildTasks.ArmorsmithGuild
{
    public class LightArmorRepairKitIII: AbstractQuest
    {
        public LightArmorRepairKitIII()
        {
            CreateQuest(184, "Armorsmith Guild Task: 1x Light Armor Repair Kit III", "arm_tsk_184")
                .IsRepeatable()

                .AddObjectiveCollectItem(1, "la_rep_3", 1, true)

                .AddRewardGold(320)
                .AddRewardGuildPoints(GuildType.ArmorsmithGuild, 68);
        }
    }
}
