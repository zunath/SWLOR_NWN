using SWLOR.Game.Server.Legacy.Enumeration;
using SWLOR.Game.Server.Legacy.Quest;

namespace SWLOR.Game.Server.Legacy.Scripts.Quest.GuildTasks.WeaponsmithGuild
{
    public class MartialArtsWeaponRepairKitI: AbstractQuest
    {
        public MartialArtsWeaponRepairKitI()
        {
            CreateQuest(261, "Weaponsmith Guild Task: 1x Martial Arts Weapon Repair Kit I", "wpn_tsk_261")
                .IsRepeatable()

                .AddObjectiveCollectItem(1, "ma_rep_1", 1, true)

                .AddRewardGold(120)
                .AddRewardGuildPoints(GuildType.WeaponsmithGuild, 28);
        }
    }
}
