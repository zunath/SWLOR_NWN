using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.Quest;

namespace SWLOR.Game.Server.Scripting.Quest.GuildTasks.WeaponsmithGuild
{
    public class MartialArtsWeaponRepairKitIII: AbstractQuest
    {
        public MartialArtsWeaponRepairKitIII()
        {
            CreateQuest(311, "Weaponsmith Guild Task: 1x Martial Arts Weapon Repair Kit III", "wpn_tsk_311")
                .IsRepeatable()
				.IsGuildTask(GuildType.WeaponsmithGuild, 3)


                .AddObjectiveCollectItem(1, "ma_rep_3", 1, true)

                .AddRewardGold(320)
                .AddRewardGuildPoints(GuildType.WeaponsmithGuild, 68);
        }
    }
}
