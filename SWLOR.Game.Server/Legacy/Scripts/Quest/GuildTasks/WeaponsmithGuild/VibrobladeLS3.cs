using SWLOR.Game.Server.Legacy.Enumeration;
using SWLOR.Game.Server.Legacy.Quest;

namespace SWLOR.Game.Server.Legacy.Scripts.Quest.GuildTasks.WeaponsmithGuild
{
    public class VibrobladeLS3: AbstractQuest
    {
        public VibrobladeLS3()
        {
            CreateQuest(322, "Weaponsmith Guild Task: 1x Vibroblade LS3", "wpn_tsk_322")
                .IsRepeatable()

                .AddObjectiveCollectItem(1, "longsword_3", 1, true)

                .AddRewardGold(285)
                .AddRewardGuildPoints(GuildType.WeaponsmithGuild, 59);
        }
    }
}
