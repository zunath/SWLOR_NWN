using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.Quest;

namespace SWLOR.Game.Server.Scripting.Quest.GuildTasks.WeaponsmithGuild
{
    public class QuarterstaffIII: AbstractQuest
    {
        public QuarterstaffIII()
        {
            CreateQuest(315, "Weaponsmith Guild Task: 1x Quarterstaff III", "wpn_tsk_315")
                .IsRepeatable()

                .AddObjectiveCollectItem(1, "quarterstaff_3", 1, true)

                .AddRewardGold(285)
                .AddRewardGuildPoints(GuildType.WeaponsmithGuild, 59);
        }
    }
}
