using SWLOR.Game.Server.Legacy.Enumeration;
using SWLOR.Game.Server.Legacy.Quest;

namespace SWLOR.Game.Server.Legacy.Scripts.Quest.GuildTasks.WeaponsmithGuild
{
    public class FinesseVibrobladeD1: AbstractQuest
    {
        public FinesseVibrobladeD1()
        {
            CreateQuest(253, "Weaponsmith Guild Task: 1x Finesse Vibroblade D1", "wpn_tsk_253")
                .IsRepeatable()

                .AddObjectiveCollectItem(1, "dagger_1", 1, true)

                .AddRewardGold(85)
                .AddRewardGuildPoints(GuildType.WeaponsmithGuild, 19);
        }
    }
}
