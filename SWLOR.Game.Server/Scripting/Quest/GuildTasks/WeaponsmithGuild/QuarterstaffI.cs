using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.Quest;

namespace SWLOR.Game.Server.Scripting.Quest.GuildTasks.WeaponsmithGuild
{
    public class QuarterstaffI: AbstractQuest
    {
        public QuarterstaffI()
        {
            CreateQuest(265, "Weaponsmith Guild Task: 1x Quarterstaff I", "wpn_tsk_265")
                .IsRepeatable()

                .AddObjectiveCollectItem(1, "quarterstaff_1", 1, true)

                .AddRewardGold(85)
                .AddRewardGuildPoints(GuildType.WeaponsmithGuild, 19);
        }
    }
}
