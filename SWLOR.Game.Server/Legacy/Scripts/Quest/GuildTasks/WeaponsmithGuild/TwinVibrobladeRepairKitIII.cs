using SWLOR.Game.Server.Legacy.Enumeration;
using SWLOR.Game.Server.Legacy.Quest;

namespace SWLOR.Game.Server.Legacy.Scripts.Quest.GuildTasks.WeaponsmithGuild
{
    public class TwinVibrobladeRepairKitIII: AbstractQuest
    {
        public TwinVibrobladeRepairKitIII()
        {
            CreateQuest(317, "Weaponsmith Guild Task: 1x Twin Vibroblade Repair Kit III", "wpn_tsk_317")
                .IsRepeatable()

                .AddObjectiveCollectItem(1, "tb_rep_3", 1, true)

                .AddRewardGold(355)
                .AddRewardGuildPoints(GuildType.WeaponsmithGuild, 77);
        }
    }
}
