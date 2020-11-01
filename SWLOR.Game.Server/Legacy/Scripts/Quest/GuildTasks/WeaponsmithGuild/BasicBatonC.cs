using SWLOR.Game.Server.Legacy.Enumeration;
using SWLOR.Game.Server.Legacy.Quest;

namespace SWLOR.Game.Server.Legacy.Scripts.Quest.GuildTasks.WeaponsmithGuild
{
    public class BasicBatonC: AbstractQuest
    {
        public BasicBatonC()
        {
            CreateQuest(222, "Weaponsmith Guild Task: 1x Basic Baton C", "wpn_tsk_222")
                .IsRepeatable()

                .AddObjectiveCollectItem(1, "club_b", 1, true)

                .AddRewardGold(35)
                .AddRewardGuildPoints(GuildType.WeaponsmithGuild, 9);
        }
    }
}
