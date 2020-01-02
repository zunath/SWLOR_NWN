using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.Quest;

namespace SWLOR.Game.Server.Scripting.Quest.GuildTasks.WeaponsmithGuild
{
    public class BatonRepairKitII: AbstractQuest
    {
        public BatonRepairKitII()
        {
            CreateQuest(277, "Weaponsmith Guild Task: 1x Baton Repair Kit II", "wpn_tsk_277")
                .IsRepeatable()

                .AddObjectiveCollectItem(1, "bt_rep_2", 1, true)

                .AddRewardGold(220)
                .AddRewardGuildPoints(GuildType.WeaponsmithGuild, 48);
        }
    }
}
