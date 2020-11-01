using SWLOR.Game.Server.Legacy.Enumeration;
using SWLOR.Game.Server.Legacy.Quest;

namespace SWLOR.Game.Server.Legacy.Scripts.Quest.GuildTasks.WeaponsmithGuild
{
    public class FinesseVibrobladeK2: AbstractQuest
    {
        public FinesseVibrobladeK2()
        {
            CreateQuest(279, "Weaponsmith Guild Task: 1x Finesse Vibroblade K2", "wpn_tsk_279")
                .IsRepeatable()

                .AddObjectiveCollectItem(1, "kukri_2", 1, true)

                .AddRewardGold(185)
                .AddRewardGuildPoints(GuildType.WeaponsmithGuild, 39);
        }
    }
}
