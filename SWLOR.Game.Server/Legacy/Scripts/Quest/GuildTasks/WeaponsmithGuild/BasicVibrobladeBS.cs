using SWLOR.Game.Server.Legacy.Enumeration;
using SWLOR.Game.Server.Legacy.Quest;

namespace SWLOR.Game.Server.Legacy.Scripts.Quest.GuildTasks.WeaponsmithGuild
{
    public class BasicVibrobladeBS: AbstractQuest
    {
        public BasicVibrobladeBS()
        {
            CreateQuest(237, "Weaponsmith Guild Task: 1x Basic Vibroblade BS", "wpn_tsk_237")
                .IsRepeatable()

                .AddObjectiveCollectItem(1, "bst_sword_b", 1, true)

                .AddRewardGold(35)
                .AddRewardGuildPoints(GuildType.WeaponsmithGuild, 9);
        }
    }
}
