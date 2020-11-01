using SWLOR.Game.Server.Legacy.Enumeration;
using SWLOR.Game.Server.Legacy.Quest;

namespace SWLOR.Game.Server.Legacy.Scripts.Quest.GuildTasks.WeaponsmithGuild
{
    public class HeavyVibrobladeGS3: AbstractQuest
    {
        public HeavyVibrobladeGS3()
        {
            CreateQuest(309, "Weaponsmith Guild Task: 1x Heavy Vibroblade GS3", "wpn_tsk_309")
                .IsRepeatable()

                .AddObjectiveCollectItem(1, "greatsword_3", 1, true)

                .AddRewardGold(285)
                .AddRewardGuildPoints(GuildType.WeaponsmithGuild, 59);
        }
    }
}
