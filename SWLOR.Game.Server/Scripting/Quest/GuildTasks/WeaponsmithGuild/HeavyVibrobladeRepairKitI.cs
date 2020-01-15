using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.Quest;

namespace SWLOR.Game.Server.Scripting.Quest.GuildTasks.WeaponsmithGuild
{
    public class HeavyVibrobladeRepairKitI: AbstractQuest
    {
        public HeavyVibrobladeRepairKitI()
        {
            CreateQuest(260, "Weaponsmith Guild Task: 1x Heavy Vibroblade Repair Kit I", "wpn_tsk_260")
                .IsRepeatable()
				.IsGuildTask(GuildType.WeaponsmithGuild, 1)


                .AddObjectiveCollectItem(1, "hv_rep_1", 1, true)

                .AddRewardGold(155)
                .AddRewardGuildPoints(GuildType.WeaponsmithGuild, 37);
        }
    }
}
