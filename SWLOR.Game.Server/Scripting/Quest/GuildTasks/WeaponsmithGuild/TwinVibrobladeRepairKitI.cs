using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.Quest;

namespace SWLOR.Game.Server.Scripting.Quest.GuildTasks.WeaponsmithGuild
{
    public class TwinVibrobladeRepairKitI: AbstractQuest
    {
        public TwinVibrobladeRepairKitI()
        {
            CreateQuest(267, "Weaponsmith Guild Task: 1x Twin Vibroblade Repair Kit I", "wpn_tsk_267")
                .IsRepeatable()

                .AddObjectiveCollectItem(1, "tb_rep_1", 1, true)

                .AddRewardGold(155)
                .AddRewardGuildPoints(GuildType.WeaponsmithGuild, 37);
        }
    }
}
