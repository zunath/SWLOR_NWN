using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.Quest;

namespace SWLOR.Game.Server.Scripts.Quest.GuildTasks.WeaponsmithGuild
{
    public class FinesseVibrobladeR2: AbstractQuest
    {
        public FinesseVibrobladeR2()
        {
            CreateQuest(280, "Weaponsmith Guild Task: 1x Finesse Vibroblade R2", "wpn_tsk_280")
                .IsRepeatable()

                .AddObjectiveCollectItem(1, "rapier_2", 1, true)

                .AddRewardGold(185)
                .AddRewardGuildPoints(GuildType.WeaponsmithGuild, 39);
        }
    }
}
