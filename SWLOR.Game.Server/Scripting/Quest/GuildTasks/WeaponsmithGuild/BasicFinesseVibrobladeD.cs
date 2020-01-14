using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.Quest;

namespace SWLOR.Game.Server.Scripting.Quest.GuildTasks.WeaponsmithGuild
{
    public class BasicFinesseVibrobladeD: AbstractQuest
    {
        public BasicFinesseVibrobladeD()
        {
            CreateQuest(225, "Weaponsmith Guild Task: 1x Basic Finesse Vibroblade D", "wpn_tsk_225")
                .IsRepeatable()
				.IsGuildTask(GuildType.WeaponsmithGuild, 0)


                .AddObjectiveCollectItem(1, "dagger_b", 1, true)

                .AddRewardGold(35)
                .AddRewardGuildPoints(GuildType.WeaponsmithGuild, 9);
        }
    }
}
