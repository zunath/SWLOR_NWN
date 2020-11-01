using SWLOR.Game.Server.Legacy.Enumeration;
using SWLOR.Game.Server.Legacy.Quest;

namespace SWLOR.Game.Server.Legacy.Scripts.Quest.GuildTasks.WeaponsmithGuild
{
    public class MartialArtsWeaponRepairKitII: AbstractQuest
    {
        public MartialArtsWeaponRepairKitII()
        {
            CreateQuest(286, "Weaponsmith Guild Task: 1x Martial Arts Weapon Repair Kit II", "wpn_tsk_286")
                .IsRepeatable()

                .AddObjectiveCollectItem(1, "ma_rep_2", 1, true)

                .AddRewardGold(220)
                .AddRewardGuildPoints(GuildType.WeaponsmithGuild, 48);
        }
    }
}
