using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.Quest;

namespace SWLOR.Game.Server.Scripts.Quest.GuildTasks.WeaponsmithGuild
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
