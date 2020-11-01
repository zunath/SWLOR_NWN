using SWLOR.Game.Server.Legacy.Enumeration;
using SWLOR.Game.Server.Legacy.Quest;

namespace SWLOR.Game.Server.Legacy.Scripts.Quest.GuildTasks.WeaponsmithGuild
{
    public class FinesseVibrobladeSS2: AbstractQuest
    {
        public FinesseVibrobladeSS2()
        {
            CreateQuest(282, "Weaponsmith Guild Task: 1x Finesse Vibroblade SS2", "wpn_tsk_282")
                .IsRepeatable()

                .AddObjectiveCollectItem(1, "shortsword_2", 1, true)

                .AddRewardGold(185)
                .AddRewardGuildPoints(GuildType.WeaponsmithGuild, 39);
        }
    }
}
