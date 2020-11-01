using SWLOR.Game.Server.Legacy.Enumeration;
using SWLOR.Game.Server.Legacy.Quest;

namespace SWLOR.Game.Server.Legacy.Scripts.Quest.GuildTasks.ArmorsmithGuild
{
    public class ShieldRepairKitI: AbstractQuest
    {
        public ShieldRepairKitI()
        {
            CreateQuest(142, "Armorsmith Guild Task: 1x Shield Repair Kit I", "arm_tsk_142")
                .IsRepeatable()

                .AddObjectiveCollectItem(1, "sh_rep_1", 1, true)

                .AddRewardGold(120)
                .AddRewardGuildPoints(GuildType.ArmorsmithGuild, 28);
        }
    }
}
