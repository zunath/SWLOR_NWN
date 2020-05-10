using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.Quest;

namespace SWLOR.Game.Server.Scripts.Quest.GuildTasks.WeaponsmithGuild
{
    public class FinesseVibrobladeK4: AbstractQuest
    {
        public FinesseVibrobladeK4()
        {
            CreateQuest(329, "Weaponsmith Guild Task: 1x Finesse Vibroblade K4", "wpn_tsk_329")
                .IsRepeatable()

                .AddObjectiveCollectItem(1, "kukri_4", 1, true)

                .AddRewardGold(385)
                .AddRewardGuildPoints(GuildType.WeaponsmithGuild, 79);
        }
    }
}
