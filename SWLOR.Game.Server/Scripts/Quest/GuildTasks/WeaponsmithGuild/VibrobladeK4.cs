using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.Quest;

namespace SWLOR.Game.Server.Scripts.Quest.GuildTasks.WeaponsmithGuild
{
    public class VibrobladeK4: AbstractQuest
    {
        public VibrobladeK4()
        {
            CreateQuest(346, "Weaponsmith Guild Task: 1x Vibroblade K4", "wpn_tsk_346")
                .IsRepeatable()

                .AddObjectiveCollectItem(1, "katana_4", 1, true)

                .AddRewardGold(385)
                .AddRewardGuildPoints(GuildType.WeaponsmithGuild, 79);
        }
    }
}
