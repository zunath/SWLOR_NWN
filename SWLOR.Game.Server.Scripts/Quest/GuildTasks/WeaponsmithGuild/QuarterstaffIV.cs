using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.Quest;

namespace SWLOR.Game.Server.Scripts.Quest.GuildTasks.WeaponsmithGuild
{
    public class QuarterstaffIV: AbstractQuest
    {
        public QuarterstaffIV()
        {
            CreateQuest(340, "Weaponsmith Guild Task: 1x Quarterstaff IV", "wpn_tsk_340")
                .IsRepeatable()

                .AddObjectiveCollectItem(1, "quarterstaff_4", 1, true)

                .AddRewardGold(385)
                .AddRewardGuildPoints(GuildType.WeaponsmithGuild, 79);
        }
    }
}
