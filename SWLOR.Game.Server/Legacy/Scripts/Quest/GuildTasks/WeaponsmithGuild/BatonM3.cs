using SWLOR.Game.Server.Legacy.Enumeration;
using SWLOR.Game.Server.Legacy.Quest;

namespace SWLOR.Game.Server.Legacy.Scripts.Quest.GuildTasks.WeaponsmithGuild
{
    public class BatonM3: AbstractQuest
    {
        public BatonM3()
        {
            CreateQuest(300, "Weaponsmith Guild Task: 1x Baton M3", "wpn_tsk_300")
                .IsRepeatable()

                .AddObjectiveCollectItem(1, "mace_3", 1, true)

                .AddRewardGold(285)
                .AddRewardGuildPoints(GuildType.WeaponsmithGuild, 59);
        }
    }
}
