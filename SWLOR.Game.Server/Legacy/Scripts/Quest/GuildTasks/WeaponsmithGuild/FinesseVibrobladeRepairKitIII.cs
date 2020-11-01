using SWLOR.Game.Server.Legacy.Enumeration;
using SWLOR.Game.Server.Legacy.Quest;

namespace SWLOR.Game.Server.Legacy.Scripts.Quest.GuildTasks.WeaponsmithGuild
{
    public class FinesseVibrobladeRepairKitIII: AbstractQuest
    {
        public FinesseVibrobladeRepairKitIII()
        {
            CreateQuest(306, "Weaponsmith Guild Task: 1x Finesse Vibroblade Repair Kit III", "wpn_tsk_306")
                .IsRepeatable()

                .AddObjectiveCollectItem(1, "fv_rep_3", 1, true)

                .AddRewardGold(320)
                .AddRewardGuildPoints(GuildType.WeaponsmithGuild, 68);
        }
    }
}
