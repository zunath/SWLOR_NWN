using SWLOR.Game.Server.Legacy.Enumeration;
using SWLOR.Game.Server.Legacy.Quest;

namespace SWLOR.Game.Server.Legacy.Scripts.Quest.GuildTasks.WeaponsmithGuild
{
    public class PolearmH2: AbstractQuest
    {
        public PolearmH2()
        {
            CreateQuest(287, "Weaponsmith Guild Task: 1x Polearm H2", "wpn_tsk_287")
                .IsRepeatable()

                .AddObjectiveCollectItem(1, "halberd_2", 1, true)

                .AddRewardGold(185)
                .AddRewardGuildPoints(GuildType.WeaponsmithGuild, 39);
        }
    }
}
