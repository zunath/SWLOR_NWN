using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.Quest;

namespace SWLOR.Game.Server.Scripts.Quest.GuildTasks.WeaponsmithGuild
{
    public class BasicFinesseVibrobladeD: AbstractQuest
    {
        public BasicFinesseVibrobladeD()
        {
            CreateQuest(225, "Weaponsmith Guild Task: 1x Basic Finesse Vibroblade D", "wpn_tsk_225")
                .IsRepeatable()

                .AddObjectiveCollectItem(1, "dagger_b", 1, true)

                .AddRewardGold(35)
                .AddRewardGuildPoints(GuildType.WeaponsmithGuild, 9);
        }
    }
}
