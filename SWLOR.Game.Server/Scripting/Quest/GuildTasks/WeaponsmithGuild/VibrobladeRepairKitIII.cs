using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.Quest;

namespace SWLOR.Game.Server.Scripting.Quest.GuildTasks.WeaponsmithGuild
{
    public class VibrobladeRepairKitIII: AbstractQuest
    {
        public VibrobladeRepairKitIII()
        {
            CreateQuest(323, "Weaponsmith Guild Task: 1x Vibroblade Repair Kit III", "wpn_tsk_323")
                .IsRepeatable()

                .AddObjectiveCollectItem(1, "vb_rep_3", 1, true)

                .AddRewardGold(320)
                .AddRewardGuildPoints(GuildType.WeaponsmithGuild, 68);
        }
    }
}
