using SWLOR.Game.Server.Legacy.Enumeration;
using SWLOR.Game.Server.Legacy.Quest;

namespace SWLOR.Game.Server.Legacy.Scripts.Quest.GuildTasks.WeaponsmithGuild
{
    public class FinesseVibrobladeD4: AbstractQuest
    {
        public FinesseVibrobladeD4()
        {
            CreateQuest(328, "Weaponsmith Guild Task: 1x Finesse Vibroblade D4", "wpn_tsk_328")
                .IsRepeatable()

                .AddObjectiveCollectItem(1, "dagger_4", 1, true)

                .AddRewardGold(385)
                .AddRewardGuildPoints(GuildType.WeaponsmithGuild, 79);
        }
    }
}
