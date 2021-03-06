using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.Quest;

namespace SWLOR.Game.Server.Scripts.Quest.GuildTasks.WeaponsmithGuild
{
    public class VibrobladeRepairKitII: AbstractQuest
    {
        public VibrobladeRepairKitII()
        {
            CreateQuest(298, "Weaponsmith Guild Task: 1x Vibroblade Repair Kit II", "wpn_tsk_298")
                .IsRepeatable()

                .AddObjectiveCollectItem(1, "vb_rep_2", 1, true)

                .AddRewardGold(220)
                .AddRewardGuildPoints(GuildType.WeaponsmithGuild, 48);
        }
    }
}
