using SWLOR.Game.Server.Legacy.Enumeration;
using SWLOR.Game.Server.Legacy.Quest;

namespace SWLOR.Game.Server.Legacy.Scripts.Quest.GuildTasks.WeaponsmithGuild
{
    public class PolearmRepairKitIII: AbstractQuest
    {
        public PolearmRepairKitIII()
        {
            CreateQuest(313, "Weaponsmith Guild Task: 1x Polearm Repair Kit III", "wpn_tsk_313")
                .IsRepeatable()

                .AddObjectiveCollectItem(1, "po_rep_3", 1, true)

                .AddRewardGold(355)
                .AddRewardGuildPoints(GuildType.WeaponsmithGuild, 77);
        }
    }
}
