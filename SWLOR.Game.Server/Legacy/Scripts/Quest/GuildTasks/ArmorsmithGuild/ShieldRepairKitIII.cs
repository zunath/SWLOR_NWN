using SWLOR.Game.Server.Legacy.Enumeration;
using SWLOR.Game.Server.Legacy.Quest;

namespace SWLOR.Game.Server.Legacy.Scripts.Quest.GuildTasks.ArmorsmithGuild
{
    public class ShieldRepairKitIII: AbstractQuest
    {
        public ShieldRepairKitIII()
        {
            CreateQuest(190, "Armorsmith Guild Task: 1x Shield Repair Kit III", "arm_tsk_190")
                .IsRepeatable()

                .AddObjectiveCollectItem(1, "sh_rep_3", 1, true)

                .AddRewardGold(320)
                .AddRewardGuildPoints(GuildType.ArmorsmithGuild, 68);
        }
    }
}
