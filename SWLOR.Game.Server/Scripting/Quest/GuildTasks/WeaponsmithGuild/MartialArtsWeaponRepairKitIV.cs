using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.Quest;

namespace SWLOR.Game.Server.Scripting.Quest.GuildTasks.WeaponsmithGuild
{
    public class MartialArtsWeaponRepairKitIV: AbstractQuest
    {
        public MartialArtsWeaponRepairKitIV()
        {
            CreateQuest(336, "Weaponsmith Guild Task: 1x Martial Arts Weapon Repair Kit IV", "wpn_tsk_336")
                .IsRepeatable()

                .AddObjectiveCollectItem(1, "ma_rep_4", 1, true)

                .AddRewardGold(420)
                .AddRewardGuildPoints(GuildType.WeaponsmithGuild, 88);
        }
    }
}
