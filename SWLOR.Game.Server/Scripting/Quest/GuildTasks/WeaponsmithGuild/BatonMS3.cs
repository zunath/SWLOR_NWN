using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.Quest;

namespace SWLOR.Game.Server.Scripting.Quest.GuildTasks.WeaponsmithGuild
{
    public class BatonMS3: AbstractQuest
    {
        public BatonMS3()
        {
            CreateQuest(301, "Weaponsmith Guild Task: 1x Baton MS3", "wpn_tsk_301")
                .IsRepeatable()
				.IsGuildTask(GuildType.WeaponsmithGuild, 3)


                .AddObjectiveCollectItem(1, "morningstar_3", 1, true)

                .AddRewardGold(285)
                .AddRewardGuildPoints(GuildType.WeaponsmithGuild, 59);
        }
    }
}
