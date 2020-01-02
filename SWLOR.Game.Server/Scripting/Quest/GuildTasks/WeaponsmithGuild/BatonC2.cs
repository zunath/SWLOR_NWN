using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.Quest;

namespace SWLOR.Game.Server.Scripting.Quest.GuildTasks.WeaponsmithGuild
{
    public class BatonC2: AbstractQuest
    {
        public BatonC2()
        {
            CreateQuest(274, "Weaponsmith Guild Task: 1x Baton C2", "wpn_tsk_274")
                .IsRepeatable()

                .AddObjectiveCollectItem(1, "club_2", 1, true)

                .AddRewardGold(185)
                .AddRewardGuildPoints(GuildType.WeaponsmithGuild, 39);
        }
    }
}
