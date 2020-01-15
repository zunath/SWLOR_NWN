using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.Quest;

namespace SWLOR.Game.Server.Scripting.Quest.GuildTasks.WeaponsmithGuild
{
    public class FinesseVibrobladeK1: AbstractQuest
    {
        public FinesseVibrobladeK1()
        {
            CreateQuest(254, "Weaponsmith Guild Task: 1x Finesse Vibroblade K1", "wpn_tsk_254")
                .IsRepeatable()
				.IsGuildTask(GuildType.WeaponsmithGuild, 1)


                .AddObjectiveCollectItem(1, "kukri_1", 1, true)

                .AddRewardGold(85)
                .AddRewardGuildPoints(GuildType.WeaponsmithGuild, 19);
        }
    }
}
