using SWLOR.Game.Server.Legacy.Enumeration;
using SWLOR.Game.Server.Legacy.Quest;

namespace SWLOR.Game.Server.Legacy.Scripts.Quest.GuildTasks.WeaponsmithGuild
{
    public class FinesseVibrobladeSS3: AbstractQuest
    {
        public FinesseVibrobladeSS3()
        {
            CreateQuest(307, "Weaponsmith Guild Task: 1x Finesse Vibroblade SS3", "wpn_tsk_307")
                .IsRepeatable()

                .AddObjectiveCollectItem(1, "shortsword_3", 1, true)

                .AddRewardGold(285)
                .AddRewardGuildPoints(GuildType.WeaponsmithGuild, 59);
        }
    }
}
