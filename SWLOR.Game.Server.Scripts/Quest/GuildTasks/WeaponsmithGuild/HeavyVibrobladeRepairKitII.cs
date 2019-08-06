using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.Quest;

namespace SWLOR.Game.Server.Scripts.Quest.GuildTasks.WeaponsmithGuild
{
    public class HeavyVibrobladeRepairKitII: AbstractQuest
    {
        public HeavyVibrobladeRepairKitII()
        {
            CreateQuest(285, "Weaponsmith Guild Task: 1x Heavy Vibroblade Repair Kit II", "wpn_tsk_285")
                .IsRepeatable()

                .AddObjectiveCollectItem(1, "hv_rep_2", 1, true)

                .AddRewardGold(255)
                .AddRewardGuildPoints(GuildType.WeaponsmithGuild, 57);
        }
    }
}
