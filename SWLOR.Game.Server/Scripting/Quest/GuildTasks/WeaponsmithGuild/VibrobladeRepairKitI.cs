using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.Quest;

namespace SWLOR.Game.Server.Scripting.Quest.GuildTasks.WeaponsmithGuild
{
    public class VibrobladeRepairKitI: AbstractQuest
    {
        public VibrobladeRepairKitI()
        {
            CreateQuest(273, "Weaponsmith Guild Task: 1x Vibroblade Repair Kit I", "wpn_tsk_273")
                .IsRepeatable()

                .AddObjectiveCollectItem(1, "vb_rep_1", 1, true)

                .AddRewardGold(120)
                .AddRewardGuildPoints(GuildType.WeaponsmithGuild, 28);
        }
    }
}
