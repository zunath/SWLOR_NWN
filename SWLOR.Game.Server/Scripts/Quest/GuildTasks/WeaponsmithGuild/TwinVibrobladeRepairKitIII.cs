using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.Quest;

namespace SWLOR.Game.Server.Scripts.Quest.GuildTasks.WeaponsmithGuild
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
