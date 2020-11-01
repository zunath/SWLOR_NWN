using SWLOR.Game.Server.Legacy.Enumeration;
using SWLOR.Game.Server.Legacy.Quest;

namespace SWLOR.Game.Server.Legacy.Scripts.Quest.GuildTasks.WeaponsmithGuild
{
    public class FinesseVibrobladeRepairKitI: AbstractQuest
    {
        public FinesseVibrobladeRepairKitI()
        {
            CreateQuest(256, "Weaponsmith Guild Task: 1x Finesse Vibroblade Repair Kit I", "wpn_tsk_256")
                .IsRepeatable()

                .AddObjectiveCollectItem(1, "fv_rep_1", 1, true)

                .AddRewardGold(120)
                .AddRewardGuildPoints(GuildType.WeaponsmithGuild, 28);
        }
    }
}
