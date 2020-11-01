using SWLOR.Game.Server.Legacy.Enumeration;
using SWLOR.Game.Server.Legacy.Quest;

namespace SWLOR.Game.Server.Legacy.Scripts.Quest.GuildTasks.WeaponsmithGuild
{
    public class FinesseVibrobladeRepairKitIV: AbstractQuest
    {
        public FinesseVibrobladeRepairKitIV()
        {
            CreateQuest(331, "Weaponsmith Guild Task: 1x Finesse Vibroblade Repair Kit IV", "wpn_tsk_331")
                .IsRepeatable()

                .AddObjectiveCollectItem(1, "fv_rep_4", 1, true)

                .AddRewardGold(420)
                .AddRewardGuildPoints(GuildType.WeaponsmithGuild, 88);
        }
    }
}
