using SWLOR.Game.Server.Legacy.Enumeration;
using SWLOR.Game.Server.Legacy.Quest;

namespace SWLOR.Game.Server.Legacy.Scripts.Quest.GuildTasks.WeaponsmithGuild
{
    public class BatonC1: AbstractQuest
    {
        public BatonC1()
        {
            CreateQuest(249, "Weaponsmith Guild Task: 1x Baton C1", "wpn_tsk_249")
                .IsRepeatable()

                .AddObjectiveCollectItem(1, "club_1", 1, true)

                .AddRewardGold(85)
                .AddRewardGuildPoints(GuildType.WeaponsmithGuild, 19);
        }
    }
}
