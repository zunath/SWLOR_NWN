using SWLOR.Game.Server.Legacy.Enumeration;
using SWLOR.Game.Server.Legacy.Quest;

namespace SWLOR.Game.Server.Legacy.Scripts.Quest.GuildTasks.WeaponsmithGuild
{
    public class PolearmRepairKitIV: AbstractQuest
    {
        public PolearmRepairKitIV()
        {
            CreateQuest(338, "Weaponsmith Guild Task: 1x Polearm Repair Kit IV", "wpn_tsk_338")
                .IsRepeatable()

                .AddObjectiveCollectItem(1, "po_rep_4", 1, true)

                .AddRewardGold(455)
                .AddRewardGuildPoints(GuildType.WeaponsmithGuild, 97);
        }
    }
}
