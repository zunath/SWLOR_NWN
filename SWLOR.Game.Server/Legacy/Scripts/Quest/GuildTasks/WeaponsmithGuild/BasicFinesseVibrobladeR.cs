using SWLOR.Game.Server.Legacy.Enumeration;
using SWLOR.Game.Server.Legacy.Quest;

namespace SWLOR.Game.Server.Legacy.Scripts.Quest.GuildTasks.WeaponsmithGuild
{
    public class BasicFinesseVibrobladeR: AbstractQuest
    {
        public BasicFinesseVibrobladeR()
        {
            CreateQuest(227, "Weaponsmith Guild Task: 1x Basic Finesse Vibroblade R", "wpn_tsk_227")
                .IsRepeatable()

                .AddObjectiveCollectItem(1, "rapier_b", 1, true)

                .AddRewardGold(35)
                .AddRewardGuildPoints(GuildType.WeaponsmithGuild, 9);
        }
    }
}
