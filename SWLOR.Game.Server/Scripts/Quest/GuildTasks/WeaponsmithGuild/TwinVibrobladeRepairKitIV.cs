using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.Quest;

namespace SWLOR.Game.Server.Scripts.Quest.GuildTasks.WeaponsmithGuild
{
    public class TwinVibrobladeRepairKitIV: AbstractQuest
    {
        public TwinVibrobladeRepairKitIV()
        {
            CreateQuest(342, "Weaponsmith Guild Task: 1x Twin Vibroblade Repair Kit IV", "wpn_tsk_342")
                .IsRepeatable()

                .AddObjectiveCollectItem(1, "tb_rep_4", 1, true)

                .AddRewardGold(455)
                .AddRewardGuildPoints(GuildType.WeaponsmithGuild, 97);
        }
    }
}
