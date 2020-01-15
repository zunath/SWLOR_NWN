using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.Quest;

namespace SWLOR.Game.Server.Scripting.Quest.GuildTasks.WeaponsmithGuild
{
    public class PolearmRepairKitII: AbstractQuest
    {
        public PolearmRepairKitII()
        {
            CreateQuest(288, "Weaponsmith Guild Task: 1x Polearm Repair Kit II", "wpn_tsk_288")
                .IsRepeatable()
				.IsGuildTask(GuildType.WeaponsmithGuild, 2)


                .AddObjectiveCollectItem(1, "po_rep_2", 1, true)

                .AddRewardGold(255)
                .AddRewardGuildPoints(GuildType.WeaponsmithGuild, 57);
        }
    }
}
