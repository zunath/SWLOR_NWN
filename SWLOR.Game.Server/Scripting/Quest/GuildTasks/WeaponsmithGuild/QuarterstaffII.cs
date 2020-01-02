using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.Quest;

namespace SWLOR.Game.Server.Scripting.Quest.GuildTasks.WeaponsmithGuild
{
    public class QuarterstaffII: AbstractQuest
    {
        public QuarterstaffII()
        {
            CreateQuest(290, "Weaponsmith Guild Task: 1x Quarterstaff II", "wpn_tsk_290")
                .IsRepeatable()

                .AddObjectiveCollectItem(1, "quarterstaff_2", 1, true)

                .AddRewardGold(185)
                .AddRewardGuildPoints(GuildType.WeaponsmithGuild, 39);
        }
    }
}
