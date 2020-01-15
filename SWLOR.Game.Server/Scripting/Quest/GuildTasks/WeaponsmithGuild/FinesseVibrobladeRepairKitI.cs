using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.Quest;

namespace SWLOR.Game.Server.Scripting.Quest.GuildTasks.WeaponsmithGuild
{
    public class FinesseVibrobladeRepairKitI: AbstractQuest
    {
        public FinesseVibrobladeRepairKitI()
        {
            CreateQuest(256, "Weaponsmith Guild Task: 1x Finesse Vibroblade Repair Kit I", "wpn_tsk_256")
                .IsRepeatable()
				.IsGuildTask(GuildType.WeaponsmithGuild, 1)


                .AddObjectiveCollectItem(1, "fv_rep_1", 1, true)

                .AddRewardGold(120)
                .AddRewardGuildPoints(GuildType.WeaponsmithGuild, 28);
        }
    }
}
