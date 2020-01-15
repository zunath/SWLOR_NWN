using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.Quest;

namespace SWLOR.Game.Server.Scripting.Quest.GuildTasks.WeaponsmithGuild
{
    public class VibrobladeK1: AbstractQuest
    {
        public VibrobladeK1()
        {
            CreateQuest(271, "Weaponsmith Guild Task: 1x Vibroblade K1", "wpn_tsk_271")
                .IsRepeatable()
				.IsGuildTask(GuildType.WeaponsmithGuild, 1)


                .AddObjectiveCollectItem(1, "katana_1", 1, true)

                .AddRewardGold(85)
                .AddRewardGuildPoints(GuildType.WeaponsmithGuild, 19);
        }
    }
}
