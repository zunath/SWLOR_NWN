using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.Quest;

namespace SWLOR.Game.Server.Scripting.Quest.GuildTasks.WeaponsmithGuild
{
    public class FinesseVibrobladeRepairKitII: AbstractQuest
    {
        public FinesseVibrobladeRepairKitII()
        {
            CreateQuest(281, "Weaponsmith Guild Task: 1x Finesse Vibroblade Repair Kit II", "wpn_tsk_281")
                .IsRepeatable()

                .AddObjectiveCollectItem(1, "fv_rep_2", 1, true)

                .AddRewardGold(220)
                .AddRewardGuildPoints(GuildType.WeaponsmithGuild, 48);
        }
    }
}
