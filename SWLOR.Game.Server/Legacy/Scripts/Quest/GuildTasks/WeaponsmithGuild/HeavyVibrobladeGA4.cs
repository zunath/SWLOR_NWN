using SWLOR.Game.Server.Legacy.Enumeration;
using SWLOR.Game.Server.Legacy.Quest;

namespace SWLOR.Game.Server.Legacy.Scripts.Quest.GuildTasks.WeaponsmithGuild
{
    public class HeavyVibrobladeGA4: AbstractQuest
    {
        public HeavyVibrobladeGA4()
        {
            CreateQuest(333, "Weaponsmith Guild Task: 1x Heavy Vibroblade GA4", "wpn_tsk_333")
                .IsRepeatable()

                .AddObjectiveCollectItem(1, "greataxe_4", 1, true)

                .AddRewardGold(385)
                .AddRewardGuildPoints(GuildType.WeaponsmithGuild, 79);
        }
    }
}
