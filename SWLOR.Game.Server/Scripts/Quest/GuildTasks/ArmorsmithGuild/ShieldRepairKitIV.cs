using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.Quest;

namespace SWLOR.Game.Server.Scripts.Quest.GuildTasks.ArmorsmithGuild
{
    public class ShieldRepairKitIV: AbstractQuest
    {
        public ShieldRepairKitIV()
        {
            CreateQuest(219, "Armorsmith Guild Task: 1x Shield Repair Kit IV", "arm_tsk_219")
                .IsRepeatable()

                .AddObjectiveCollectItem(1, "sh_rep_4", 1, true)

                .AddRewardGold(420)
                .AddRewardGuildPoints(GuildType.ArmorsmithGuild, 88);
        }
    }
}
