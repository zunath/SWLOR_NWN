using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.Quest;

namespace SWLOR.Game.Server.Scripts.Quest.GuildTasks.WeaponsmithGuild
{
    public class VibrobladeBS1: AbstractQuest
    {
        public VibrobladeBS1()
        {
            CreateQuest(270, "Weaponsmith Guild Task: 1x Vibroblade BS1", "wpn_tsk_270")
                .IsRepeatable()

                .AddObjectiveCollectItem(1, "bst_sword_1", 1, true)

                .AddRewardGold(85)
                .AddRewardGuildPoints(GuildType.WeaponsmithGuild, 19);
        }
    }
}
