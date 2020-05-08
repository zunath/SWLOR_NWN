using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.Quest;

namespace SWLOR.Game.Server.Scripts.Quest.GuildTasks.WeaponsmithGuild
{
    public class FinesseVibrobladeD3: AbstractQuest
    {
        public FinesseVibrobladeD3()
        {
            CreateQuest(303, "Weaponsmith Guild Task: 1x Finesse Vibroblade D3", "wpn_tsk_303")
                .IsRepeatable()

                .AddObjectiveCollectItem(1, "dagger_3", 1, true)

                .AddRewardGold(285)
                .AddRewardGuildPoints(GuildType.WeaponsmithGuild, 59);
        }
    }
}
