using SWLOR.Game.Server.Legacy.Enumeration;
using SWLOR.Game.Server.Legacy.Quest;

namespace SWLOR.Game.Server.Legacy.Scripts.Quest.GuildTasks.WeaponsmithGuild
{
    public class HeavyVibrobladeGA2: AbstractQuest
    {
        public HeavyVibrobladeGA2()
        {
            CreateQuest(283, "Weaponsmith Guild Task: 1x Heavy Vibroblade GA2", "wpn_tsk_283")
                .IsRepeatable()

                .AddObjectiveCollectItem(1, "greataxe_2", 1, true)

                .AddRewardGold(185)
                .AddRewardGuildPoints(GuildType.WeaponsmithGuild, 39);
        }
    }
}
