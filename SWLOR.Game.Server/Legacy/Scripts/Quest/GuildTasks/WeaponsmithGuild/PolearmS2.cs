using SWLOR.Game.Server.Legacy.Enumeration;
using SWLOR.Game.Server.Legacy.Quest;

namespace SWLOR.Game.Server.Legacy.Scripts.Quest.GuildTasks.WeaponsmithGuild
{
    public class PolearmS2: AbstractQuest
    {
        public PolearmS2()
        {
            CreateQuest(289, "Weaponsmith Guild Task: 1x Polearm S2", "wpn_tsk_289")
                .IsRepeatable()

                .AddObjectiveCollectItem(1, "spear_2", 1, true)

                .AddRewardGold(185)
                .AddRewardGuildPoints(GuildType.WeaponsmithGuild, 39);
        }
    }
}
