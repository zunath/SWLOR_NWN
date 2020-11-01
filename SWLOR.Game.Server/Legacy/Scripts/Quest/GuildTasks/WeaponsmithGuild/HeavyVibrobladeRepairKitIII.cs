using SWLOR.Game.Server.Legacy.Enumeration;
using SWLOR.Game.Server.Legacy.Quest;

namespace SWLOR.Game.Server.Legacy.Scripts.Quest.GuildTasks.WeaponsmithGuild
{
    public class HeavyVibrobladeRepairKitIII: AbstractQuest
    {
        public HeavyVibrobladeRepairKitIII()
        {
            CreateQuest(310, "Weaponsmith Guild Task: 1x Heavy Vibroblade Repair Kit III", "wpn_tsk_310")
                .IsRepeatable()

                .AddObjectiveCollectItem(1, "hv_rep_3", 1, true)

                .AddRewardGold(355)
                .AddRewardGuildPoints(GuildType.WeaponsmithGuild, 77);
        }
    }
}
