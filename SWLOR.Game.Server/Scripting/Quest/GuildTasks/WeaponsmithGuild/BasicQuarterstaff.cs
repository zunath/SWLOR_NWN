using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.Quest;

namespace SWLOR.Game.Server.Scripting.Quest.GuildTasks.WeaponsmithGuild
{
    public class BasicQuarterstaff: AbstractQuest
    {
        public BasicQuarterstaff()
        {
            CreateQuest(233, "Weaponsmith Guild Task: 1x Basic Quarterstaff", "wpn_tsk_233")
                .IsRepeatable()
				.IsGuildTask(GuildType.WeaponsmithGuild, 0)


                .AddObjectiveCollectItem(1, "quarterstaff_b", 1, true)

                .AddRewardGold(35)
                .AddRewardGuildPoints(GuildType.WeaponsmithGuild, 9);
        }
    }
}
