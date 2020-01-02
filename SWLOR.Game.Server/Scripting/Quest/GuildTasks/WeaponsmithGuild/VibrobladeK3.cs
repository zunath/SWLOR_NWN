using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.Quest;

namespace SWLOR.Game.Server.Scripting.Quest.GuildTasks.WeaponsmithGuild
{
    public class VibrobladeK3: AbstractQuest
    {
        public VibrobladeK3()
        {
            CreateQuest(321, "Weaponsmith Guild Task: 1x Vibroblade K3", "wpn_tsk_321")
                .IsRepeatable()

                .AddObjectiveCollectItem(1, "katana_3", 1, true)

                .AddRewardGold(285)
                .AddRewardGuildPoints(GuildType.WeaponsmithGuild, 59);
        }
    }
}
