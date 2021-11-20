using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.Quest;

namespace SWLOR.Game.Server.Scripts.Quest.GuildTasks.WeaponsmithGuild
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
