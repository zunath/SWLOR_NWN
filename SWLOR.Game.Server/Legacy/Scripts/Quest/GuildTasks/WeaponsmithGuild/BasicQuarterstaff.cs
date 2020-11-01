using SWLOR.Game.Server.Legacy.Enumeration;
using SWLOR.Game.Server.Legacy.Quest;

namespace SWLOR.Game.Server.Legacy.Scripts.Quest.GuildTasks.WeaponsmithGuild
{
    public class BasicQuarterstaff: AbstractQuest
    {
        public BasicQuarterstaff()
        {
            CreateQuest(233, "Weaponsmith Guild Task: 1x Basic Quarterstaff", "wpn_tsk_233")
                .IsRepeatable()

                .AddObjectiveCollectItem(1, "quarterstaff_b", 1, true)

                .AddRewardGold(35)
                .AddRewardGuildPoints(GuildType.WeaponsmithGuild, 9);
        }
    }
}
