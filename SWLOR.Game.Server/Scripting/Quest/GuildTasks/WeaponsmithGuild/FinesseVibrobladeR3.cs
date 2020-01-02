using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.Quest;

namespace SWLOR.Game.Server.Scripting.Quest.GuildTasks.WeaponsmithGuild
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
