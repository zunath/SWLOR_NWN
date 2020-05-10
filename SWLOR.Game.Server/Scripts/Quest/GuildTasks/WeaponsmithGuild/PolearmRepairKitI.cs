using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.Quest;

namespace SWLOR.Game.Server.Scripts.Quest.GuildTasks.WeaponsmithGuild
{
    public class PolearmRepairKitI: AbstractQuest
    {
        public PolearmRepairKitI()
        {
            CreateQuest(263, "Weaponsmith Guild Task: 1x Polearm Repair Kit I", "wpn_tsk_263")
                .IsRepeatable()

                .AddObjectiveCollectItem(1, "po_rep_1", 1, true)

                .AddRewardGold(155)
                .AddRewardGuildPoints(GuildType.WeaponsmithGuild, 37);
        }
    }
}
