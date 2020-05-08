using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.Quest;

namespace SWLOR.Game.Server.Scripts.Quest.GuildTasks.WeaponsmithGuild
{
    public class VibrobladeBS3: AbstractQuest
    {
        public VibrobladeBS3()
        {
            CreateQuest(320, "Weaponsmith Guild Task: 1x Vibroblade BS3", "wpn_tsk_320")
                .IsRepeatable()

                .AddObjectiveCollectItem(1, "bst_sword_3", 1, true)

                .AddRewardGold(285)
                .AddRewardGuildPoints(GuildType.WeaponsmithGuild, 59);
        }
    }
}
