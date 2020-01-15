using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.Quest;

namespace SWLOR.Game.Server.Scripting.Quest.GuildTasks.WeaponsmithGuild
{
    public class VibrobladeK4: AbstractQuest
    {
        public VibrobladeK4()
        {
            CreateQuest(346, "Weaponsmith Guild Task: 1x Vibroblade K4", "wpn_tsk_346")
                .IsRepeatable()
				.IsGuildTask(GuildType.WeaponsmithGuild, 4)


                .AddObjectiveCollectItem(1, "katana_4", 1, true)

                .AddRewardGold(385)
                .AddRewardGuildPoints(GuildType.WeaponsmithGuild, 79);
        }
    }
}
