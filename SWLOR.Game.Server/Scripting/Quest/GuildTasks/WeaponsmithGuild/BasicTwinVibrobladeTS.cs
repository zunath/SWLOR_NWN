using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.Quest;

namespace SWLOR.Game.Server.Scripting.Quest.GuildTasks.WeaponsmithGuild
{
    public class BasicTwinVibrobladeTS: AbstractQuest
    {
        public BasicTwinVibrobladeTS()
        {
            CreateQuest(235, "Weaponsmith Guild Task: 1x Basic Twin Vibroblade TS", "wpn_tsk_235")
                .IsRepeatable()
				.IsGuildTask(GuildType.WeaponsmithGuild, 0)


                .AddObjectiveCollectItem(1, "twinblade_b", 1, true)

                .AddRewardGold(55)
                .AddRewardGuildPoints(GuildType.WeaponsmithGuild, 14);
        }
    }
}
