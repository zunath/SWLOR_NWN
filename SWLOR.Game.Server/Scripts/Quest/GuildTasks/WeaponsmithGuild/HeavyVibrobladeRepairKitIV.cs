using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.Quest;

namespace SWLOR.Game.Server.Scripts.Quest.GuildTasks.WeaponsmithGuild
{
    public class HeavyVibrobladeRepairKitIV: AbstractQuest
    {
        public HeavyVibrobladeRepairKitIV()
        {
            CreateQuest(335, "Weaponsmith Guild Task: 1x Heavy Vibroblade Repair Kit IV", "wpn_tsk_335")
                .IsRepeatable()

                .AddObjectiveCollectItem(1, "hv_rep_4", 1, true)

                .AddRewardGold(455)
                .AddRewardGuildPoints(GuildType.WeaponsmithGuild, 97);
        }
    }
}
