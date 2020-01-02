using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.Quest;

namespace SWLOR.Game.Server.Scripting.Quest.GuildTasks.WeaponsmithGuild
{
    public class FinesseVibrobladeR4: AbstractQuest
    {
        public FinesseVibrobladeR4()
        {
            CreateQuest(330, "Weaponsmith Guild Task: 1x Finesse Vibroblade R4", "wpn_tsk_330")
                .IsRepeatable()

                .AddObjectiveCollectItem(1, "rapier_4", 1, true)

                .AddRewardGold(385)
                .AddRewardGuildPoints(GuildType.WeaponsmithGuild, 79);
        }
    }
}
