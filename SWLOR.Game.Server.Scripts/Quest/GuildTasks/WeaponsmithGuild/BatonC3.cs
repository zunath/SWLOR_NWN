using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.Quest;

namespace SWLOR.Game.Server.Scripts.Quest.GuildTasks.WeaponsmithGuild
{
    public class BatonC3: AbstractQuest
    {
        public BatonC3()
        {
            CreateQuest(299, "Weaponsmith Guild Task: 1x Baton C3", "wpn_tsk_299")
                .IsRepeatable()

                .AddObjectiveCollectItem(1, "club_3", 1, true)

                .AddRewardGold(285)
                .AddRewardGuildPoints(GuildType.WeaponsmithGuild, 59);
        }
    }
}
