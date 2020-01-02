using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.Quest;

namespace SWLOR.Game.Server.Scripting.Quest.GuildTasks.WeaponsmithGuild
{
    public class BasicFinesseVibrobladeK: AbstractQuest
    {
        public BasicFinesseVibrobladeK()
        {
            CreateQuest(226, "Weaponsmith Guild Task: 1x Basic Finesse Vibroblade K", "wpn_tsk_226")
                .IsRepeatable()

                .AddObjectiveCollectItem(1, "kukri_b", 1, true)

                .AddRewardGold(35)
                .AddRewardGuildPoints(GuildType.WeaponsmithGuild, 9);
        }
    }
}
