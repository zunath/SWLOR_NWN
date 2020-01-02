using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.Quest;

namespace SWLOR.Game.Server.Scripting.Quest.GuildTasks.WeaponsmithGuild
{
    public class TwinVibrobladeRepairKitII: AbstractQuest
    {
        public TwinVibrobladeRepairKitII()
        {
            CreateQuest(292, "Weaponsmith Guild Task: 1x Twin Vibroblade Repair Kit II", "wpn_tsk_292")
                .IsRepeatable()

                .AddObjectiveCollectItem(1, "tb_rep_2", 1, true)

                .AddRewardGold(255)
                .AddRewardGuildPoints(GuildType.WeaponsmithGuild, 57);
        }
    }
}
