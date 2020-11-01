using SWLOR.Game.Server.Legacy.Enumeration;
using SWLOR.Game.Server.Legacy.Quest;

namespace SWLOR.Game.Server.Legacy.Scripts.Quest.GuildTasks.WeaponsmithGuild
{
    public class BatonRepairKitIII: AbstractQuest
    {
        public BatonRepairKitIII()
        {
            CreateQuest(302, "Weaponsmith Guild Task: 1x Baton Repair Kit III", "wpn_tsk_302")
                .IsRepeatable()

                .AddObjectiveCollectItem(1, "bt_rep_3", 1, true)

                .AddRewardGold(320)
                .AddRewardGuildPoints(GuildType.WeaponsmithGuild, 68);
        }
    }
}
