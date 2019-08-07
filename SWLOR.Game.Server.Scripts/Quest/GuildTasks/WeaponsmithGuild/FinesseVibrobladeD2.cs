using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.Quest;

namespace SWLOR.Game.Server.Scripts.Quest.GuildTasks.WeaponsmithGuild
{
    public class FinesseVibrobladeD2: AbstractQuest
    {
        public FinesseVibrobladeD2()
        {
            CreateQuest(278, "Weaponsmith Guild Task: 1x Finesse Vibroblade D2", "wpn_tsk_278")
                .IsRepeatable()

                .AddObjectiveCollectItem(1, "dagger_2", 1, true)

                .AddRewardGold(185)
                .AddRewardGuildPoints(GuildType.WeaponsmithGuild, 39);
        }
    }
}
