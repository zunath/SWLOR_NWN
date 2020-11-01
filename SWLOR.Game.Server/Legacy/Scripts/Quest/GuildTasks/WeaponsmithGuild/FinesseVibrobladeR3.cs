using SWLOR.Game.Server.Legacy.Enumeration;
using SWLOR.Game.Server.Legacy.Quest;

namespace SWLOR.Game.Server.Legacy.Scripts.Quest.GuildTasks.WeaponsmithGuild
{
    public class FinesseVibrobladeR3: AbstractQuest
    {
        public FinesseVibrobladeR3()
        {
            CreateQuest(305, "Weaponsmith Guild Task: 1x Finesse Vibroblade R3", "wpn_tsk_305")
                .IsRepeatable()

                .AddObjectiveCollectItem(1, "rapier_3", 1, true)

                .AddRewardGold(285)
                .AddRewardGuildPoints(GuildType.WeaponsmithGuild, 59);
        }
    }
}
