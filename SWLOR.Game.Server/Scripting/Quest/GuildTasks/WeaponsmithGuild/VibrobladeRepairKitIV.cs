using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.Quest;

namespace SWLOR.Game.Server.Scripting.Quest.GuildTasks.WeaponsmithGuild
{
    public class VibrobladeRepairKitIV: AbstractQuest
    {
        public VibrobladeRepairKitIV()
        {
            CreateQuest(348, "Weaponsmith Guild Task: 1x Vibroblade Repair Kit IV", "wpn_tsk_348")
                .IsRepeatable()
				.IsGuildTask(GuildType.WeaponsmithGuild, 4)


                .AddObjectiveCollectItem(1, "vb_rep_4", 1, true)

                .AddRewardGold(420)
                .AddRewardGuildPoints(GuildType.WeaponsmithGuild, 88);
        }
    }
}
