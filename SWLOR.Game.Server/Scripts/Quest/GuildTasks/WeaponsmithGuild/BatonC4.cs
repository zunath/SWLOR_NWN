using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.Quest;

namespace SWLOR.Game.Server.Scripts.Quest.GuildTasks.WeaponsmithGuild
{
    public class BatonC4: AbstractQuest
    {
        public BatonC4()
        {
            CreateQuest(324, "Weaponsmith Guild Task: 1x Baton C4", "wpn_tsk_324")
                .IsRepeatable()

                .AddObjectiveCollectItem(1, "club_4", 1, true)

                .AddRewardGold(385)
                .AddRewardGuildPoints(GuildType.WeaponsmithGuild, 79);
        }
    }
}
